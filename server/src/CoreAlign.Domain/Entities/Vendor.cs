using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Entities;

/// <summary>
/// Tedarikçi (Supplier / Vendor) master. Mirrors the shape of <see cref="Customer"/>
/// but lives in the AP side of the ledger: balance increases on goods receipt /
/// supplier invoice and decreases on payment. Vendor approval is a separate
/// gate — POs can only reference vendors with status <see cref="VendorStatus.Active"/>.
/// </summary>
public class Vendor : TenantEntity
{
    public string? Code { get; private set; }
    public VendorType Type { get; private set; } = VendorType.Business;
    public string Name { get; private set; } = string.Empty;
    public string? LegalName { get; private set; }
    public string? TradeName { get; private set; }
    public string? NationalId { get; private set; }
    public string? TaxNumber { get; private set; }
    public string? TaxOffice { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string? Website { get; private set; }

    public string DefaultCurrency { get; private set; } = "TRY";
    public Guid? PaymentTermsId { get; private set; }
    public Guid? BuyerUserId { get; private set; }

    /// <summary>Negative when we owe the vendor money — kept negative for cari report parity with Customer.</summary>
    public decimal CurrentBalance { get; private set; }
    public decimal OverdueAmount { get; private set; }
    public decimal TotalPayable { get; private set; }
    public string? Classification { get; private set; }
    public string? Territory { get; private set; }
    public string? LanguageCode { get; private set; }
    public Guid? ParentVendorId { get; private set; }

    public VendorStatus Status { get; private set; } = VendorStatus.PendingApproval;
    public string? BlockReason { get; private set; }
    public string? Notes { get; private set; }

    /// <summary>Supplier-specific procurement lead time in days; overrides the product default in MRP when this vendor is a product's preferred supplier. 0 = no override (use product lead time).</summary>
    public int DefaultLeadTimeDays { get; private set; }

    /// <summary>Vendor performance rating 1-5 (set externally from GRN/quality data).</summary>
    public int? Rating { get; private set; }
    public DateTime? ApprovedAtUtc { get; private set; }
    public Guid? ApprovedByUserId { get; private set; }

    public bool IsActive => Status == VendorStatus.Active;
    public bool CanReceivePO => Status == VendorStatus.Active;

    public PaymentTerm? PaymentTerms { get; set; }
    public Vendor? ParentVendor { get; set; }

    protected Vendor() { }

    public Vendor(
        string name,
        VendorType type = VendorType.Business,
        string? code = null,
        string? legalName = null,
        string? tradeName = null,
        string? email = null,
        string? phone = null,
        string? taxNumber = null,
        string? taxOffice = null,
        string? notes = null,
        string defaultCurrency = "TRY")
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Vendor name is required.", nameof(name));
        Name = name.Trim();
        Type = type;
        Code = code?.Trim();
        LegalName = legalName?.Trim();
        TradeName = tradeName?.Trim();
        Email = email?.Trim();
        Phone = phone?.Trim();
        TaxNumber = taxNumber?.Trim();
        TaxOffice = taxOffice?.Trim();
        Notes = notes?.Trim();
        DefaultCurrency = defaultCurrency.Trim().ToUpperInvariant();
    }

    public void AssignCode(string code)
    {
        if (!string.IsNullOrWhiteSpace(Code)) return;
        Code = code.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Update(
        VendorType type,
        string name,
        string? legalName,
        string? tradeName,
        string? nationalId,
        string? taxNumber,
        string? taxOffice,
        string? email,
        string? phone,
        string? website,
        string defaultCurrency,
        Guid? paymentTermsId,
        Guid? buyerUserId,
        string? classification,
        string? territory,
        string? languageCode,
        Guid? parentVendorId,
        string? notes)
    {
        if (parentVendorId == Id)
        {
            throw new ArgumentException("A vendor cannot reference itself as parent.", nameof(parentVendorId));
        }
        Type = type;
        Name = name.Trim();
        LegalName = legalName?.Trim();
        TradeName = tradeName?.Trim();
        NationalId = nationalId?.Trim();
        TaxNumber = taxNumber?.Trim();
        TaxOffice = taxOffice?.Trim();
        Email = email?.Trim();
        Phone = phone?.Trim();
        Website = website?.Trim();
        DefaultCurrency = defaultCurrency.Trim().ToUpperInvariant();
        PaymentTermsId = paymentTermsId;
        BuyerUserId = buyerUserId;
        Classification = classification?.Trim();
        Territory = territory?.Trim();
        LanguageCode = languageCode?.Trim();
        ParentVendorId = parentVendorId;
        Notes = notes?.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Approve(Guid approvedByUserId)
    {
        if (Status == VendorStatus.Archived)
        {
            throw new InvalidOperationException("Archived vendors cannot be approved; re-activate first.");
        }
        Status = VendorStatus.Active;
        ApprovedAtUtc = DateTime.UtcNow;
        ApprovedByUserId = approvedByUserId;
        BlockReason = null;
        UpdatedAtUtc = ApprovedAtUtc.Value;
    }

    public void Block(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Block reason is required.", nameof(reason));
        Status = VendorStatus.Blocked;
        BlockReason = reason.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Archive()
    {
        Status = VendorStatus.Archived;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetRating(int rating)
    {
        if (rating < 1 || rating > 5) throw new ArgumentOutOfRangeException(nameof(rating), "Rating must be 1-5.");
        Rating = rating;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetDefaultLeadTime(int days)
    {
        if (days < 0) throw new ArgumentOutOfRangeException(nameof(days), "Lead time cannot be negative.");
        DefaultLeadTimeDays = days;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void RecalculateBalance(decimal currentBalance, decimal overdueAmount, decimal totalPayable)
    {
        CurrentBalance = currentBalance;
        OverdueAmount = overdueAmount;
        TotalPayable = totalPayable;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
