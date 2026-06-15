using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using CoreAlign.Application.Billing.Payments;
using Iyzipay.Model;

namespace CoreAlign.Infrastructure.Payments;

/// <summary>
/// Pure helpers used by <see cref="IyzicoPaymentGateway"/>. Kept static so
/// every branch can be unit-tested without the SDK or HTTP.
/// </summary>
public static class IyzicoHelpers
{
    /// <summary>
    /// Iyzico expects price strings invariant-culture, ALWAYS with two decimals
    /// (banker-style rounding would over/under-bill — use MidpointRounding.AwayFromZero
    /// to match merchant-visible totals).
    /// </summary>
    public static string FormatAmount(decimal amount)
    {
        var rounded = Math.Round(amount, 2, MidpointRounding.AwayFromZero);
        return rounded.ToString("F2", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Translates our ISO currency code onto the Iyzipay <see cref="Currency"/>
    /// enum. Throws for anything outside the supported set.
    /// </summary>
    public static Currency MapCurrency(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency)) throw new PaymentGatewayException("Currency is required.");
        return currency.Trim().ToUpperInvariant() switch
        {
            "TRY" => Currency.TRY,
            "USD" => Currency.USD,
            "EUR" => Currency.EUR,
            "GBP" => Currency.GBP,
            _ => throw new PaymentGatewayException($"Currency '{currency}' is not supported by Iyzico.")
        };
    }

    /// <summary>
    /// Maps an Iyzico PaymentStatus string ("SUCCESS", "FAILURE", "INIT_THREEDS", ...)
    /// to our internal lifecycle status.
    /// </summary>
    public static PaymentIntentStatus MapPaymentStatus(string? iyzicoPaymentStatus, string? overallStatus)
    {
        if (string.Equals(overallStatus, "failure", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(iyzicoPaymentStatus))
        {
            return PaymentIntentStatus.Failed;
        }
        if (string.IsNullOrWhiteSpace(iyzicoPaymentStatus)) return PaymentIntentStatus.Pending;
        return iyzicoPaymentStatus.Trim().ToUpperInvariant() switch
        {
            "SUCCESS" => PaymentIntentStatus.Succeeded,
            "FAILURE" => PaymentIntentStatus.Failed,
            "INIT_THREEDS" => PaymentIntentStatus.Pending,
            "CALLBACK_THREEDS" => PaymentIntentStatus.Pending,
            "BKM_POS_SELECTED" => PaymentIntentStatus.Pending,
            "CALLBACK_PECCO" => PaymentIntentStatus.Pending,
            _ => PaymentIntentStatus.Pending,
        };
    }

    /// <summary>
    /// Constant-time verification of Iyzico's HMAC-SHA1 push-notification
    /// signature: base64( HMAC-SHA1(SecretKey, ApiKey + payload + SecretKey) )
    /// per Iyzico's docs. Returns false on mismatch / missing inputs — never throws.
    /// </summary>
    public static bool VerifyPushSignature(string? apiKey, string? secretKey, string? payload, string? providedSignatureBase64)
    {
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(secretKey) || payload is null || string.IsNullOrWhiteSpace(providedSignatureBase64))
        {
            return false;
        }

        var signingMaterial = Encoding.UTF8.GetBytes(apiKey + payload + secretKey);
        using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(secretKey));
        var computed = hmac.ComputeHash(signingMaterial);
        byte[] provided;
        try
        {
            provided = Convert.FromBase64String(providedSignatureBase64.Trim());
        }
        catch (FormatException)
        {
            return false;
        }
        if (computed.Length != provided.Length) return false;
        return CryptographicOperations.FixedTimeEquals(computed, provided);
    }

    /// <summary>
    /// Parses a form-urlencoded body (Iyzico's checkout callback POSTs
    /// <c>token=&lt;value&gt;</c>) into a case-insensitive dictionary.
    /// </summary>
    public static IReadOnlyDictionary<string, string> ParseFormUrlEncoded(string payload)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(payload)) return result;
        foreach (var pair in payload.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var eq = pair.IndexOf('=');
            if (eq <= 0) continue;
            var key = Uri.UnescapeDataString(pair[..eq]);
            var value = Uri.UnescapeDataString(pair[(eq + 1)..]);
            result[key] = value;
        }
        return result;
    }

    /// <summary>
    /// Determines whether a webhook content-type header indicates Iyzico's
    /// JSON push notification (refund / chargeback) versus the form-urlencoded
    /// checkout callback.
    /// </summary>
    public static bool IsJsonPush(IReadOnlyDictionary<string, string> headers)
    {
        if (headers is null || headers.Count == 0) return false;
        if (!headers.TryGetValue("Content-Type", out var ct) || string.IsNullOrWhiteSpace(ct)) return false;
        return ct.StartsWith("application/json", StringComparison.OrdinalIgnoreCase);
    }
}
