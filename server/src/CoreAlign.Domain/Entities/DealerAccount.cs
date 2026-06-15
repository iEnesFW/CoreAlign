using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Entities;

public class DealerAccount : TenantEntity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? LegalName { get; private set; }
    public string? TaxNumber { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string? Address { get; private set; }
    public DealerAccountStatus Status { get; private set; } = DealerAccountStatus.Active;
    public Guid? CreatedByUserId { get; private set; }
    public string? Notes { get; private set; }
    public string? SuspensionReason { get; private set; }
    public decimal CommissionPercent { get; private set; }

    protected DealerAccount() { }

    public DealerAccount(
        string code,
        string name,
        Guid? createdByUserId,
        string? legalName = null,
        string? taxNumber = null,
        string? email = null,
        string? phone = null,
        string? address = null,
        string? notes = null,
        decimal commissionPercent = 0m)
    {
        Code = code;
        Name = name;
        LegalName = legalName;
        TaxNumber = taxNumber;
        Email = email;
        Phone = phone;
        Address = address;
        Notes = notes;
        CreatedByUserId = createdByUserId;
        Status = DealerAccountStatus.Active;
        CommissionPercent = NormalizePercent(commissionPercent);
    }

    public void SetCommissionPercent(decimal commissionPercent)
    {
        CommissionPercent = NormalizePercent(commissionPercent);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private static decimal NormalizePercent(decimal value)
    {
        if (value < 0m) throw new ArgumentOutOfRangeException(nameof(value), "Commission percent cannot be negative.");
        if (value > 100m) throw new ArgumentOutOfRangeException(nameof(value), "Commission percent cannot exceed 100.");
        return Math.Round(value, 4);
    }

    public void Update(
        string name,
        string? legalName,
        string? taxNumber,
        string? email,
        string? phone,
        string? address,
        string? notes)
    {
        Name = name;
        LegalName = legalName;
        TaxNumber = taxNumber;
        Email = email;
        Phone = phone;
        Address = address;
        Notes = notes;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Activate()
    {
        Status = DealerAccountStatus.Active;
        SuspensionReason = null;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Suspend(string? reason)
    {
        Status = DealerAccountStatus.Suspended;
        SuspensionReason = reason;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Archive()
    {
        Status = DealerAccountStatus.Archived;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
