using System.Security.Cryptography;
using System.Text;

namespace CoreAlign.Infrastructure.Providers.EFatura.Foriba;

public static class ForibaWebhookVerifier
{
    public const string SignatureHeader = "X-Foriba-Signature";

    public static bool Verify(string body, string? signature, string secret)
    {
        if (string.IsNullOrWhiteSpace(signature) || string.IsNullOrWhiteSpace(secret))
        {
            return false;
        }

        var payloadBytes = Encoding.UTF8.GetBytes(body ?? string.Empty);
        var secretBytes = Encoding.UTF8.GetBytes(secret);

        using var hmac = new HMACSHA1(secretBytes);
        var computed = hmac.ComputeHash(payloadBytes);
        var computedHex = Convert.ToHexString(computed);

        var normalized = signature.StartsWith("sha1=", StringComparison.OrdinalIgnoreCase)
            ? signature["sha1=".Length..]
            : signature;

        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(computedHex),
            Encoding.ASCII.GetBytes(normalized.ToUpperInvariant()));
    }
}
