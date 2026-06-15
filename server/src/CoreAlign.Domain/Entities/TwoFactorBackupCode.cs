using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities;

public class TwoFactorBackupCode : TenantEntity
{
    public Guid UserId { get; set; }
    public string CodeHash { get; set; } = string.Empty;
    public DateTime? UsedAtUtc { get; set; }

    public User? User { get; set; }

    protected TwoFactorBackupCode() { }

    public TwoFactorBackupCode(Guid tenantId, Guid userId, string codeHash)
    {
        TenantId = tenantId;
        UserId = userId;
        CodeHash = codeHash;
    }

    public bool IsUsed => UsedAtUtc.HasValue;

    public void MarkUsed(DateTime utcNow)
    {
        UsedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }
}
