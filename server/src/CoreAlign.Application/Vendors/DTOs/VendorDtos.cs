using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Vendors.DTOs;

public class VendorDto
{
    public Guid Id { get; set; }
    public string? Code { get; set; }
    public VendorType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? LegalName { get; set; }
    public string? TradeName { get; set; }
    public string? NationalId { get; set; }
    public string? TaxNumber { get; set; }
    public string? TaxOffice { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Website { get; set; }
    public string DefaultCurrency { get; set; } = "TRY";
    public Guid? PaymentTermsId { get; set; }
    public string? PaymentTermsName { get; set; }
    public Guid? BuyerUserId { get; set; }
    public string? Classification { get; set; }
    public string? Territory { get; set; }
    public string? LanguageCode { get; set; }
    public Guid? ParentVendorId { get; set; }
    public VendorStatus Status { get; set; }
    public string? BlockReason { get; set; }
    public string? Notes { get; set; }
    public int? Rating { get; set; }
    public int DefaultLeadTimeDays { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal OverdueAmount { get; set; }
    public decimal TotalPayable { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public class VendorSummaryDto
{
    public Guid Id { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? LegalName { get; set; }
    public string? TaxNumber { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public VendorType Type { get; set; }
    public VendorStatus Status { get; set; }
    public string DefaultCurrency { get; set; } = "TRY";
    public decimal CurrentBalance { get; set; }
    public decimal OverdueAmount { get; set; }
}

public class VendorAddressDto
{
    public Guid Id { get; set; }
    public Guid VendorId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Line1 { get; set; } = string.Empty;
    public string? Line2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public bool IsPrimary { get; set; }
}

public class VendorContactDto
{
    public Guid Id { get; set; }
    public Guid VendorId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Role { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Notes { get; set; }
    public bool IsPrimary { get; set; }
}

public class VendorBankAccountDto
{
    public Guid Id { get; set; }
    public Guid VendorId { get; set; }
    public string BankName { get; set; } = string.Empty;
    public string? BranchName { get; set; }
    public string AccountHolder { get; set; } = string.Empty;
    public string Iban { get; set; } = string.Empty;
    public string? Swift { get; set; }
    public string Currency { get; set; } = "TRY";
    public string? AccountNumber { get; set; }
    public bool IsPrimary { get; set; }
    public string? Notes { get; set; }
}

public class VendorLedgerEntryDto
{
    public Guid Id { get; set; }
    public Guid VendorId { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public DateTime PostingDate { get; set; }
    public LedgerEntryType EntryType { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
    public decimal ExchangeRate { get; set; }
    public decimal AmountInBase { get; set; }
    public LedgerSourceType SourceType { get; set; }
    public Guid? SourceDocumentId { get; set; }
    public string? SourceDocumentNumber { get; set; }
    public decimal RunningBalanceAfter { get; set; }
    public string? Description { get; set; }
}
