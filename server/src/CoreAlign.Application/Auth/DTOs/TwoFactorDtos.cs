namespace CoreAlign.Application.Auth.DTOs;

public class TwoFactorEnrollmentDto
{
    public string QrCodeUri { get; set; } = string.Empty;
    public string ManualKey { get; set; } = string.Empty;
}

public class TwoFactorBackupCodesDto
{
    public List<string> BackupCodes { get; set; } = new();
}

public class StepUpResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime MfaVerifiedAtUtc { get; set; }
}
