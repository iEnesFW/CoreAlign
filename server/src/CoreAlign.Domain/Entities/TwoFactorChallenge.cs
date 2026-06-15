using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities;

public class TwoFactorChallenge : TenantEntity
{
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? ConsumedAtUtc { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    public User? User { get; set; }

    protected TwoFactorChallenge() { }

    public TwoFactorChallenge(
        Guid tenantId,
        Guid userId,
        string tokenHash,
        DateTime expiresAtUtc,
        string? ipAddress = null,
        string? userAgent = null)
    {
        TenantId = tenantId;
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
        IpAddress = ipAddress;
        UserAgent = userAgent;
    }

    public bool IsExpired(DateTime utcNow) => ExpiresAtUtc <= utcNow;
    public bool IsConsumed => ConsumedAtUtc.HasValue;

    public void Consume(DateTime utcNow)
    {
        ConsumedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }
}
