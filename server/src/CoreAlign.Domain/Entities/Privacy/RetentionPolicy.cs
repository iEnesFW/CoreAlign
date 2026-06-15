using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities.Privacy;

public enum RetentionActionOnExpiry
{
    Anonymize = 0,
    Archive = 1,
    Delete = 2,
}

public class RetentionPolicy : TenantEntity, IHasConcurrencyToken, ISoftDeletable
{
    public string EntityType { get; private set; } = string.Empty;
    public int RetentionDays { get; private set; }
    public RetentionActionOnExpiry ActionOnExpiry { get; private set; } = RetentionActionOnExpiry.Anonymize;
    public DateTime? LastRunAtUtc { get; private set; }
    public int LastRunAffectedCount { get; private set; }
    public bool IsEnabled { get; private set; } = true;
    public bool KeepFinancialTrail { get; private set; } = true;

    public long ConcurrencyToken { get; private set; }
    void IHasConcurrencyToken.BumpConcurrencyToken() => ConcurrencyToken++;

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public Guid? DeletedByUserId { get; set; }
    public string? DeletedReason { get; set; }

    public void MarkDeleted(Guid? userId, string? reason, DateTime utcNow)
    {
        ((ISoftDeletable)this).MarkDeletedInternal(userId, reason, utcNow);
        UpdatedAtUtc = utcNow;
    }

    public void Restore()
    {
        ((ISoftDeletable)this).RestoreInternal();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    protected RetentionPolicy() { }

    public static RetentionPolicy Create(
        Guid tenantId,
        string entityType,
        int retentionDays,
        RetentionActionOnExpiry actionOnExpiry,
        bool keepFinancialTrail = true)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(entityType)) throw new ArgumentException("EntityType is required.", nameof(entityType));
        if (retentionDays <= 0) throw new ArgumentOutOfRangeException(nameof(retentionDays), "RetentionDays must be positive.");

        return new RetentionPolicy
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EntityType = entityType.Trim(),
            RetentionDays = retentionDays,
            ActionOnExpiry = actionOnExpiry,
            KeepFinancialTrail = keepFinancialTrail,
            IsEnabled = true,
        };
    }

    public void Update(int retentionDays, RetentionActionOnExpiry actionOnExpiry, bool keepFinancialTrail, bool isEnabled, DateTime utcNow)
    {
        if (retentionDays <= 0) throw new ArgumentOutOfRangeException(nameof(retentionDays), "RetentionDays must be positive.");
        RetentionDays = retentionDays;
        ActionOnExpiry = actionOnExpiry;
        KeepFinancialTrail = keepFinancialTrail;
        IsEnabled = isEnabled;
        UpdatedAtUtc = utcNow;
    }

    public void RecordRun(DateTime utcNow, int affected)
    {
        LastRunAtUtc = utcNow;
        LastRunAffectedCount = affected;
        UpdatedAtUtc = utcNow;
    }
}
