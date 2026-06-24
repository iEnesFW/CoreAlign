using CoreAlign.Application.Common.Audit;

namespace CoreAlign.Application.Tests.Common.Audit;

public class DefaultAuditFieldRedactorTests
{
    private readonly DefaultAuditFieldRedactor _sut = new();

    [Fact]
    public void Password_field_is_masked()
    {
        var redacted = _sut.Redact("Password", "hunter2");

        redacted.Should().Be("***");
    }

    [Fact]
    public void Non_sensitive_field_returns_original_value()
    {
        var redacted = _sut.Redact("Email", "user@example.com");

        redacted.Should().Be("user@example.com");
    }

    [Theory]
    [InlineData("Token")]
    [InlineData("TOKEN")]
    [InlineData("token")]
    [InlineData("RefreshToken")]
    public void Sensitive_token_match_is_case_insensitive(string fieldName)
    {
        var redacted = _sut.Redact(fieldName, "abc.def.ghi");

        redacted.Should().Be("***");
    }

    [Fact]
    public void Null_value_returns_null_regardless_of_sensitivity()
    {
        _sut.Redact("Password", null).Should().BeNull();
        _sut.Redact("Email", null).Should().BeNull();
    }

    [Theory]
    [InlineData("Iban")]
    [InlineData("VendorIban")]
    [InlineData("NationalId")]
    [InlineData("TaxNumber")]
    [InlineData("AccountNumber")]
    [InlineData("TwoFactorSecretKey")]
    [InlineData("PhoneNumber")]
    public void Newly_added_sensitive_tokens_are_masked(string fieldName)
    {
        _sut.Redact(fieldName, "sensitive").Should().Be("***");
    }

    [Fact]
    public void RedactJson_masks_sensitive_properties_and_preserves_others()
    {
        var json = """{"name":"Acme","nationalId":"12345678901","iban":"TR000001","note":"ok"}""";

        var redacted = _sut.RedactJson(json);

        redacted.Should().Contain("\"name\":\"Acme\"");
        redacted.Should().Contain("\"note\":\"ok\"");
        redacted.Should().Contain("\"nationalId\":\"***\"");
        redacted.Should().Contain("\"iban\":\"***\"");
        redacted.Should().NotContain("12345678901");
        redacted.Should().NotContain("TR000001");
    }

    [Fact]
    public void RedactJson_recurses_into_nested_objects()
    {
        var json = """{"outer":{"password":"secret-value","keep":"visible"}}""";

        var redacted = _sut.RedactJson(json);

        redacted.Should().NotContain("secret-value");
        redacted.Should().Contain("\"keep\":\"visible\"");
    }

    [Fact]
    public void RedactJson_returns_input_unchanged_for_null_invalid_or_non_object()
    {
        _sut.RedactJson(null).Should().BeNull();
        _sut.RedactJson("not json").Should().Be("not json");
        _sut.RedactJson("[1,2,3]").Should().Be("[1,2,3]");
    }
}
