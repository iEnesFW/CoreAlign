using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Entities;

public class DealerCustomerLink : TenantEntity
{
    public Guid DealerAccountId { get; private set; }
    public Guid CustomerId { get; private set; }
    public DealerCustomerLinkStatus Status { get; private set; } = DealerCustomerLinkStatus.Active;
    public Guid? AssignedByUserId { get; private set; }
    public DateTime AssignedAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime? RevokedAtUtc { get; private set; }
    public Guid? RevokedByUserId { get; private set; }
    public string? RevokeReason { get; private set; }
    public string? Notes { get; private set; }
    public decimal? CommissionPercentOverride { get; private set; }

    public DealerAccount DealerAccount { get; set; } = null!;
    public Customer Customer { get; set; } = null!;

    protected DealerCustomerLink() { }

    public DealerCustomerLink(
        Guid dealerAccountId,
        Guid customerId,
        Guid? assignedByUserId,
        string? notes = null,
        decimal? commissionPercentOverride = null)
    {
        DealerAccountId = dealerAccountId;
        CustomerId = customerId;
        AssignedByUserId = assignedByUserId;
        Notes = notes;
        Status = DealerCustomerLinkStatus.Active;
        AssignedAtUtc = DateTime.UtcNow;
        CommissionPercentOverride = NormalizeOverride(commissionPercentOverride);
    }

    public void SetCommissionPercentOverride(decimal? value)
    {
        CommissionPercentOverride = NormalizeOverride(value);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private static decimal? NormalizeOverride(decimal? value)
    {
        if (value is null) return null;
        if (value < 0m) throw new ArgumentOutOfRangeException(nameof(value), "Commission percent cannot be negative.");
        if (value > 100m) throw new ArgumentOutOfRangeException(nameof(value), "Commission percent cannot exceed 100.");
        return Math.Round(value.Value, 4);
    }

    public void Suspend()
    {
        Status = DealerCustomerLinkStatus.Suspended;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Activate()
    {
        Status = DealerCustomerLinkStatus.Active;
        RevokedAtUtc = null;
        RevokedByUserId = null;
        RevokeReason = null;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Revoke(Guid? byUserId, string? reason)
    {
        Status = DealerCustomerLinkStatus.Archived;
        RevokedAtUtc = DateTime.UtcNow;
        RevokedByUserId = byUserId;
        RevokeReason = reason;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
