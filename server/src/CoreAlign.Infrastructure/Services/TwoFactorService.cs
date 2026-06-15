using System.Security.Cryptography;
using System.Text;
using System.Web;
using CoreAlign.Application.Auth.Services;
using OtpNet;

namespace CoreAlign.Infrastructure.Services;

public sealed class TwoFactorService : ITwoFactorService
{
    private const int SecretByteLength = 20;
    private const int TotpDigits = 6;
    private const int TotpPeriodSeconds = 30;
    private const string BackupCodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public string GenerateSecret()
    {
        var bytes = new byte[SecretByteLength];
        RandomNumberGenerator.Fill(bytes);
        return Base32Encoding.ToString(bytes).TrimEnd('=');
    }

    public string BuildOtpAuthUri(string secret, string accountName, string issuer)
    {
        if (string.IsNullOrWhiteSpace(secret)) throw new ArgumentException("Secret is required.", nameof(secret));
        if (string.IsNullOrWhiteSpace(accountName)) throw new ArgumentException("AccountName is required.", nameof(accountName));
        if (string.IsNullOrWhiteSpace(issuer)) throw new ArgumentException("Issuer is required.", nameof(issuer));

        var encodedIssuer = HttpUtility.UrlEncode(issuer);
        var encodedAccount = HttpUtility.UrlEncode(accountName);
        var label = $"{encodedIssuer}:{encodedAccount}";
        return $"otpauth://totp/{label}?secret={secret}&issuer={encodedIssuer}&algorithm=SHA1&digits={TotpDigits}&period={TotpPeriodSeconds}";
    }

    public bool Verify(string secret, string code, int allowedWindowsBack = 1)
    {
        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var normalizedCode = code.Trim().Replace(" ", string.Empty);
        if (normalizedCode.Length != TotpDigits)
        {
            return false;
        }

        try
        {
            var secretBytes = Base32Encoding.ToBytes(secret);
            var totp = new Totp(secretBytes, step: TotpPeriodSeconds, mode: OtpHashMode.Sha1, totpSize: TotpDigits);
            var window = new VerificationWindow(previous: Math.Max(0, allowedWindowsBack), future: 1);
            return totp.VerifyTotp(normalizedCode, out _, window);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public IReadOnlyList<string> GenerateBackupCodes(int count = 10)
    {
        if (count <= 0)
        {
            return Array.Empty<string>();
        }

        var results = new List<string>(count);
        var buffer = new byte[8];
        for (var i = 0; i < count; i++)
        {
            RandomNumberGenerator.Fill(buffer);
            var sb = new StringBuilder(8);
            for (var j = 0; j < 8; j++)
            {
                sb.Append(BackupCodeAlphabet[buffer[j] % BackupCodeAlphabet.Length]);
            }
            results.Add(sb.ToString());
        }
        return results;
    }

    public string HashBackupCode(string plaintext)
    {
        if (string.IsNullOrWhiteSpace(plaintext)) throw new ArgumentException("Plaintext is required.", nameof(plaintext));
        var normalized = plaintext.Trim().ToUpperInvariant();
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes);
    }
}
