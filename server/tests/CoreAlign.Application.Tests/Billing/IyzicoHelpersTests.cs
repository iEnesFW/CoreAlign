using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using CoreAlign.Application.Billing.Payments;
using CoreAlign.Infrastructure.Payments;
using Iyzipay.Model;

namespace CoreAlign.Application.Tests.Billing;

public class IyzicoAmountFormattingTests
{
    [Theory]
    [InlineData("99", "99.00")]
    [InlineData("99.5", "99.50")]
    [InlineData("99.5500", "99.55")]
    [InlineData("1234.567", "1234.57")]
    [InlineData("0", "0.00")]
    [InlineData("0.005", "0.01")]
    public void Formats_amounts_invariant_culture_with_two_decimals(string input, string expected)
    {
        var value = decimal.Parse(input, CultureInfo.InvariantCulture);
        IyzicoHelpers.FormatAmount(value).Should().Be(expected);
    }

    [Fact]
    public void Formatting_uses_invariant_culture_not_turkish()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
            IyzicoHelpers.FormatAmount(99.5m).Should().Be("99.50");
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }
}

public class IyzicoCurrencyMapperTests
{
    [Theory]
    [InlineData("TRY")]
    [InlineData("try")]
    [InlineData("USD")]
    [InlineData("EUR")]
    [InlineData("GBP")]
    public void Supported_currencies_map_to_iyzipay_enum(string code)
    {
        var act = () => IyzicoHelpers.MapCurrency(code);
        act.Should().NotThrow();
    }

    [Fact]
    public void Try_maps_to_TRY()
    {
        IyzicoHelpers.MapCurrency("TRY").Should().Be(Currency.TRY);
    }

    [Fact]
    public void Usd_maps_to_USD()
    {
        IyzicoHelpers.MapCurrency("usd").Should().Be(Currency.USD);
    }

    [Theory]
    [InlineData("JPY")]
    [InlineData("AUD")]
    [InlineData("XYZ")]
    [InlineData("")]
    public void Unsupported_currencies_throw_PaymentGatewayException(string code)
    {
        var act = () => IyzicoHelpers.MapCurrency(code);
        act.Should().Throw<PaymentGatewayException>();
    }
}

public class IyzicoResponseMappingTests
{
    [Theory]
    [InlineData("SUCCESS", PaymentIntentStatus.Succeeded)]
    [InlineData("success", PaymentIntentStatus.Succeeded)]
    public void Success_maps_to_succeeded(string status, PaymentIntentStatus expected)
    {
        IyzicoHelpers.MapPaymentStatus(status, "success").Should().Be(expected);
    }

    [Fact]
    public void Failure_maps_to_failed()
    {
        IyzicoHelpers.MapPaymentStatus("FAILURE", "success").Should().Be(PaymentIntentStatus.Failed);
    }

    [Theory]
    [InlineData("INIT_THREEDS")]
    [InlineData("CALLBACK_THREEDS")]
    [InlineData("BKM_POS_SELECTED")]
    public void Intermediate_statuses_map_to_pending(string status)
    {
        IyzicoHelpers.MapPaymentStatus(status, "success").Should().Be(PaymentIntentStatus.Pending);
    }

    [Fact]
    public void Null_payment_status_with_failure_overall_is_failed()
    {
        IyzicoHelpers.MapPaymentStatus(null, "failure").Should().Be(PaymentIntentStatus.Failed);
    }

    [Fact]
    public void Null_payment_status_with_no_failure_is_pending()
    {
        IyzicoHelpers.MapPaymentStatus(null, "success").Should().Be(PaymentIntentStatus.Pending);
    }
}

public class IyzicoSignatureVerifierTests
{
    private const string ApiKey = "test-api-key";
    private const string SecretKey = "test-secret-key";

    [Fact]
    public void Correct_signature_passes()
    {
        var payload = "{\"eventType\":\"REFUND\",\"paymentId\":\"123\"}";
        var sig = ComputeSignature(payload);

        IyzicoHelpers.VerifyPushSignature(ApiKey, SecretKey, payload, sig).Should().BeTrue();
    }

    [Fact]
    public void Tampered_payload_fails()
    {
        var payload = "{\"eventType\":\"REFUND\",\"paymentId\":\"123\"}";
        var sig = ComputeSignature(payload);
        var tampered = payload + " ";

        IyzicoHelpers.VerifyPushSignature(ApiKey, SecretKey, tampered, sig).Should().BeFalse();
    }

    [Fact]
    public void Wrong_secret_fails()
    {
        var payload = "{\"eventType\":\"REFUND\"}";
        var sig = ComputeSignature(payload);

        IyzicoHelpers.VerifyPushSignature(ApiKey, "different-secret", payload, sig).Should().BeFalse();
    }

    [Fact]
    public void Missing_signature_returns_false()
    {
        IyzicoHelpers.VerifyPushSignature(ApiKey, SecretKey, "{}", null).Should().BeFalse();
        IyzicoHelpers.VerifyPushSignature(ApiKey, SecretKey, "{}", "").Should().BeFalse();
    }

    [Fact]
    public void Invalid_base64_returns_false()
    {
        IyzicoHelpers.VerifyPushSignature(ApiKey, SecretKey, "{}", "not-base64!!").Should().BeFalse();
    }

    [Fact]
    public void Missing_credentials_returns_false()
    {
        var sig = ComputeSignature("{}");
        IyzicoHelpers.VerifyPushSignature(null, SecretKey, "{}", sig).Should().BeFalse();
        IyzicoHelpers.VerifyPushSignature(ApiKey, null, "{}", sig).Should().BeFalse();
    }

    private static string ComputeSignature(string payload)
    {
        var material = Encoding.UTF8.GetBytes(ApiKey + payload + SecretKey);
        using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(SecretKey));
        return Convert.ToBase64String(hmac.ComputeHash(material));
    }
}

public class IyzicoFormParserTests
{
    [Fact]
    public void Parses_token_field_from_callback_body()
    {
        var body = "token=abc123";
        var result = IyzicoHelpers.ParseFormUrlEncoded(body);
        result.Should().ContainKey("token").WhoseValue.Should().Be("abc123");
    }

    [Fact]
    public void Handles_multiple_url_encoded_fields()
    {
        var body = "token=abc%20123&other=foo%26bar";
        var result = IyzicoHelpers.ParseFormUrlEncoded(body);
        result["token"].Should().Be("abc 123");
        result["other"].Should().Be("foo&bar");
    }

    [Fact]
    public void Empty_input_returns_empty_dictionary()
    {
        IyzicoHelpers.ParseFormUrlEncoded("").Should().BeEmpty();
        IyzicoHelpers.ParseFormUrlEncoded(null!).Should().BeEmpty();
    }

    [Fact]
    public void Is_json_push_detects_content_type()
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Content-Type"] = "application/json; charset=utf-8",
        };
        IyzicoHelpers.IsJsonPush(headers).Should().BeTrue();
    }

    [Fact]
    public void Is_json_push_returns_false_for_form_url_encoded()
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Content-Type"] = "application/x-www-form-urlencoded",
        };
        IyzicoHelpers.IsJsonPush(headers).Should().BeFalse();
    }
}
