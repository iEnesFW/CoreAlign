namespace CoreAlign.Application.Auth.Services;

public interface ITwoFactorService
{
    string GenerateSecret();
    string BuildOtpAuthUri(string secret, string accountName, string issuer);
    bool Verify(string secret, string code, int allowedWindowsBack = 1);
    IReadOnlyList<string> GenerateBackupCodes(int count = 10);
    string HashBackupCode(string plaintext);
}
