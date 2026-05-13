using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Customers.DTOs;

public class CustomerDto
{
    public Guid Id { get; set; }
    public string? Code { get; set; }
    public CustomerType Type { get; set; }
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
    public Guid? PriceListId { get; set; }
    public Guid? CustomerGroupId { get; set; }
    public Guid? SalesRepUserId { get; set; }
    public decimal CreditLimit { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal OverdueAmount { get; set; }
    public decimal DefaultDiscountPercent { get; set; }
    public string? Classification { get; set; }
    public string? Channel { get; set; }
    public string? Territory { get; set; }
    public string? LanguageCode { get; set; }
    public Guid? ParentCustomerId { get; set; }
    public CustomerStatus Status { get; set; }
    public string? BlockReason { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
