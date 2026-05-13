using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Accounting.DTOs;

public class AccountingPeriodDto
{
    public Guid Id { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public string Code { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public AccountingPeriodStatus Status { get; set; }
    public DateTime? ClosedAtUtc { get; set; }
    public Guid? ClosedByUserId { get; set; }
    public DateTime? ReopenedAtUtc { get; set; }
    public string? Notes { get; set; }
}

public class CustomerProductPriceDto
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public Guid ProductId { get; set; }
    public string ProductSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Currency { get; set; } = "TRY";
    public decimal Price { get; set; }
    public decimal? DiscountPercent { get; set; }
    public decimal? MinQuantity { get; set; }
    public decimal? MaxQuantity { get; set; }
    public DateTime? ValidFromUtc { get; set; }
    public DateTime? ValidUntilUtc { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
}

public class ResolvedPriceDto
{
    public decimal UnitPrice { get; set; }
    public string Currency { get; set; } = "TRY";
    public decimal DiscountPercent { get; set; }
    public PriceSource Source { get; set; }
    public string SourceLabel { get; set; } = string.Empty;
    public decimal? ReferenceListPrice { get; set; }
    public decimal TaxRatePercent { get; set; }
    public bool IsTaxInclusive { get; set; }
    public Guid? TaxRateId { get; set; }
    public Guid? AppliedRecordId { get; set; }
}
