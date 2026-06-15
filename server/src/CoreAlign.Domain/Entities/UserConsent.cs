using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities;

public class UserConsent : TenantEntity
{
    public Guid? UserId { get; private set; }
    public string? AnonymousFingerprint { get; private set; }
    public string Purpose { get; private set; } = string.Empty;
    public string Version { get; private set; } = string.Empty;
    public DateTime CapturedAtUtc { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public DateTime? WithdrawnAtUtc { get; private set; }

    protected UserConsent() { }

    public UserConsent(
        Guid? userId,
        string? anonymousFingerprint,
        string purpose,
        string version,
        DateTime capturedAtUtc,
        string? ipAddress,
        string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(purpose))
            throw new ArgumentException("Purpose is required.", nameof(purpose));
        if (string.IsNullOrWhiteSpace(version))
            throw new ArgumentException("Version is required.", nameof(version));
        if (!userId.HasValue && string.IsNullOrWhiteSpace(anonymousFingerprint))
            throw new ArgumentException("Either UserId or AnonymousFingerprint must be supplied.");

        UserId = userId;
        AnonymousFingerprint = string.IsNullOrWhiteSpace(anonymousFingerprint) ? null : anonymousFingerprint;
        Purpose = purpose.Trim().ToLowerInvariant();
        Version = version.Trim();
        CapturedAtUtc = capturedAtUtc;
        IpAddress = ipAddress;
        UserAgent = userAgent;
    }

    public void Withdraw(DateTime withdrawnAtUtc)
    {
        if (WithdrawnAtUtc.HasValue) return;
        WithdrawnAtUtc = withdrawnAtUtc;
    }
}
