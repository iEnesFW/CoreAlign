using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace CoreAlign.Infrastructure.Providers.Payment.PayTR;

/// <summary>
/// Builds and verifies the PayTR HMAC-SHA256 hash signatures. PayTR's API authenticates
/// every request with a per-request hash derived from the merchant credentials plus the
/// request payload; the merchant salt is concatenated into the hash payload and the
/// merchant key is the HMAC key. The output is base64. The same scheme verifies
/// inbound callback notifications.
/// </summary>
public static class PayTRHashBuilder
{
    public static string BuildChargeHash(
        string merchantId,
        string userIp,
        string merchantOid,
        string email,
        decimal paymentAmount,
        string currency,
        bool isSandbox,
        string merchantKey,
        string merchantSalt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(merchantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(merchantOid);
        ArgumentException.ThrowIfNullOrWhiteSpace(merchantKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(merchantSalt);

        var amountInCents = ToCents(paymentAmount);
        var testMode = isSandbox ? "1" : "0";
        var payload =
            merchantId
            + (userIp ?? string.Empty)
            + merchantOid
            + (email ?? string.Empty)
            + amountInCents.ToString(CultureInfo.InvariantCulture)
            + currency
            + testMode
            + merchantSalt;

        return ComputeBase64(merchantKey, payload);
    }

    public static string BuildRefundHash(
        string merchantId,
        string merchantOid,
        decimal returnAmount,
        string merchantKey,
        string merchantSalt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(merchantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(merchantOid);

        var amountInCents = ToCents(returnAmount);
        var payload =
            merchantId
            + merchantOid
            + amountInCents.ToString(CultureInfo.InvariantCulture)
            + merchantSalt;

        return ComputeBase64(merchantKey, payload);
    }

    public static string BuildStatusHash(
        string merchantId,
        string merchantOid,
        string merchantKey,
        string merchantSalt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(merchantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(merchantOid);

        var payload = merchantId + merchantOid + merchantSalt;
        return ComputeBase64(merchantKey, payload);
    }

    public static string BuildTokenizeHash(
        string merchantId,
        string merchantOid,
        string email,
        string merchantKey,
        string merchantSalt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(merchantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(merchantOid);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        var payload = merchantId + merchantOid + email + merchantSalt;
        return ComputeBase64(merchantKey, payload);
    }

    /// <summary>
    /// Verifies a PayTR callback signature. PayTR builds the callback hash as
    /// HMAC-SHA256(merchantKey, merchantOid + merchantSalt + status + totalAmount).
    /// Comparison is constant-time.
    /// </summary>
    public static bool VerifyCallback(
        string merchantOid,
        string status,
        decimal totalAmount,
        string receivedHashBase64,
        string merchantKey,
        string merchantSalt)
    {
        if (string.IsNullOrWhiteSpace(receivedHashBase64) ||
            string.IsNullOrWhiteSpace(merchantKey) ||
            string.IsNullOrWhiteSpace(merchantSalt) ||
            string.IsNullOrWhiteSpace(merchantOid) ||
            string.IsNullOrWhiteSpace(status))
        {
            return false;
        }

        var amountInCents = ToCents(totalAmount);
        var payload = merchantOid + merchantSalt + status + amountInCents.ToString(CultureInfo.InvariantCulture);
        var expected = ComputeBase64(merchantKey, payload);

        byte[] expectedBytes;
        byte[] receivedBytes;
        try
        {
            expectedBytes = Convert.FromBase64String(expected);
            receivedBytes = Convert.FromBase64String(receivedHashBase64.Trim());
        }
        catch (FormatException)
        {
            return false;
        }

        if (expectedBytes.Length != receivedBytes.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(expectedBytes, receivedBytes);
    }

    private static long ToCents(decimal amount)
    {
        return (long)decimal.Round(amount * 100m, 0, MidpointRounding.AwayFromZero);
    }

    private static string ComputeBase64(string key, string payload)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(payloadBytes);
        return Convert.ToBase64String(hash);
    }
}
