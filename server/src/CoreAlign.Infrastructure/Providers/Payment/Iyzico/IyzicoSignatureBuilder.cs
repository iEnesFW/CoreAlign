using System.Security.Cryptography;
using System.Text;

namespace CoreAlign.Infrastructure.Providers.Payment.Iyzico;

public static class IyzicoSignatureBuilder
{
    public const string AuthorizationScheme = "IYZWS";
    public const string RandomHeader = "x-iyzi-rnd";
    public const string AuthorizationHeader = "Authorization";

    public static string BuildAuthorizationHeader(string apiKey, string secretKey, string randomString, string requestBody)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(secretKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(randomString);
        ArgumentNullException.ThrowIfNull(requestBody);

        var pkiString = BuildPkiSignature(apiKey, secretKey, randomString, requestBody);
        return $"{AuthorizationScheme} {apiKey}:{pkiString}";
    }

    public static string BuildPkiSignature(string apiKey, string secretKey, string randomString, string requestBody)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(secretKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(randomString);
        ArgumentNullException.ThrowIfNull(requestBody);

        var raw = apiKey + randomString + secretKey + requestBody;
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToBase64String(bytes);
    }

    public static string GenerateRandomString()
    {
        var ticks = DateTime.UtcNow.Ticks;
        var entropy = RandomNumberGenerator.GetHexString(16, lowercase: true);
        return ticks.ToString(System.Globalization.CultureInfo.InvariantCulture) + entropy;
    }
}
