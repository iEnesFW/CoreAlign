using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Entities;

public class Customer : TenantEntity
{
    public string? Code { get; private set; }
    public CustomerType Type { get; private set; } = CustomerType.Business;
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
    public Guid? PriceListId { get; private set; }
    public Guid? CustomerGroupId { get; private set; }
    public Guid? SalesRepUserId { get; private set; }

    public decimal CreditLimit { get; private set; }
    public decimal CurrentBalance { get; private set; }
    public decimal OverdueAmount { get; private set; }
    public decimal DefaultDiscountPercent { get; private set; }
    public string? Classification { get; private set; }
    public string? Channel { get; private set; }
    public string? Territory { get; private set; }
    public string? LanguageCode { get; private set; }
    public Guid? ParentCustomerId { get; private set; }

    public CustomerStatus Status { get; private set; } = CustomerStatus.Active;
    public string? BlockReason { get; private set; }
    public string? Notes { get; private set; }
    public bool IsActive => Status == CustomerStatus.Active;

    public bool IsAnonymized { get; private set; }
    public DateTime? AnonymizedAtUtc { get; private set; }

    public void Anonymize(string redactedDisplayName)
    {
        if (IsAnonymized) return;
        Name = string.IsNullOrWhiteSpace(redactedDisplayName) ? "[REDACTED]" : redactedDisplayName;
        LegalName = null;
        TradeName = null;
        NationalId = null;
        TaxNumber = null;
        TaxOffice = null;
        Email = null;
        Phone = null;
        Website = null;
        Notes = null;
        IsAnonymized = true;
        AnonymizedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = AnonymizedAtUtc.Value;
    }

    public PaymentTerm? PaymentTerms { get; set; }
    public PriceList? PriceList { get; set; }
    public CustomerGroup? CustomerGroup { get; set; }
    public Customer? ParentCustomer { get; set; }

    protected Customer() { }

    public Customer(
        string name,
        CustomerType type = CustomerType.Business,
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
        Name = name;
        Type = type;
        Code = code;
        LegalName = legalName;
        TradeName = tradeName;
        Email = email;
        Phone = phone;
        TaxNumber = taxNumber;
        TaxOffice = taxOffice;
        Notes = notes;
        DefaultCurrency = defaultCurrency;
    }

    public void AssignCode(string code)
    {
        if (!string.IsNullOrWhiteSpace(Code))
        {
            return;
        }
        Code = code;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Update(
        CustomerType type,
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
        Guid? priceListId,
        Guid? customerGroupId,
        Guid? salesRepUserId,
        decimal creditLimit,
        decimal defaultDiscountPercent,
        string? classification,
        string? channel,
        string? territory,
        string? languageCode,
        Guid? parentCustomerId,
        string? notes)
    {
        if (parentCustomerId == Id)
        {
            throw new ArgumentException("A customer cannot reference itself as parent.", nameof(parentCustomerId));
        }
        Type = type;
        Name = name;
        LegalName = legalName;
        TradeName = tradeName;
        NationalId = nationalId;
        TaxNumber = taxNumber;
        TaxOffice = taxOffice;
        Email = email;
        Phone = phone;
        Website = website;
        DefaultCurrency = defaultCurrency;
        PaymentTermsId = paymentTermsId;
        PriceListId = priceListId;
        CustomerGroupId = customerGroupId;
        SalesRepUserId = salesRepUserId;
        CreditLimit = creditLimit;
        DefaultDiscountPercent = defaultDiscountPercent;
        Classification = classification;
        Channel = channel;
        Territory = territory;
        LanguageCode = languageCode;
        ParentCustomerId = parentCustomerId;
        Notes = notes;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void ApplyContactInfo(string? email, string? phone, string? taxNumber)
    {
        Email = email;
        Phone = phone;
        TaxNumber = taxNumber;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Block(string reason)
    {
        Status = CustomerStatus.Blocked;
        BlockReason = reason;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Archive()
    {
        Status = CustomerStatus.Archived;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Activate()
    {
        Status = CustomerStatus.Active;
        BlockReason = null;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void RecalculateBalance(decimal currentBalance, decimal overdueAmount)
    {
        CurrentBalance = currentBalance;
        OverdueAmount = overdueAmount;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
