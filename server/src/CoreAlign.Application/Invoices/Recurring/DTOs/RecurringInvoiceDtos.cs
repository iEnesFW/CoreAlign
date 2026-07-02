using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Invoices.Recurring.DTOs;

public class RecurringInvoiceTemplateDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string Currency { get; set; } = "TRY";
    public RecurrenceFrequency Frequency { get; set; }
    public int IntervalCount { get; set; }
    public int? AnchorDayOfMonth { get; set; }
    public DayOfWeek? AnchorDayOfWeek { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public int? MaxOccurrences { get; set; }
    public DateOnly NextRunDate { get; set; }
    public DateOnly? LastRunDate { get; set; }
    public int OccurrencesGenerated { get; set; }
    public int DueDays { get; set; }
    public Guid? PaymentTermsId { get; set; }
    public decimal? HeaderDiscountPercent { get; set; }
    public decimal? HeaderDiscountAmount { get; set; }
    public decimal? ShippingCost { get; set; }
    public decimal? RoundingAdjustment { get; set; }
    public RecurringInvoiceStatus Status { get; set; }
    public bool AutoConfirm { get; set; }
    public string? PublicNotes { get; set; }
    public string? InternalNotes { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public IReadOnlyList<RecurringInvoiceTemplateLineDto> Lines { get; set; } = Array.Empty<RecurringInvoiceTemplateLineDto>();
}

public class RecurringInvoiceTemplateLineDto
{
    public Guid Id { get; set; }
    public int LineNumber { get; set; }
    public Guid? ProductId { get; set; }
    public string ProductSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TaxRatePercent { get; set; }
    public Guid? TaxRateId { get; set; }
    public decimal? LineDiscountPercent { get; set; }
    public decimal? LineDiscountAmount { get; set; }
    public decimal? WithholdingRatePercent { get; set; }
    public bool IsTaxInclusive { get; set; }
    public Guid? UomId { get; set; }
    public string? UomCode { get; set; }
}

public class RecurringInvoiceTemplateSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Currency { get; set; } = "TRY";
    public RecurrenceFrequency Frequency { get; set; }
    public int IntervalCount { get; set; }
    public DateOnly NextRunDate { get; set; }
    public int OccurrencesGenerated { get; set; }
    public RecurringInvoiceStatus Status { get; set; }
    public int LineCount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public record RecurringInvoiceLineInput(
    Guid? ProductId,
    string? ProductName,
    string? Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal TaxRatePercent = 0m,
    Guid? TaxRateId = null,
    decimal? LineDiscountPercent = null,
    decimal? LineDiscountAmount = null,
    decimal? WithholdingRatePercent = null,
    bool IsTaxInclusive = false,
    Guid? UomId = null,
    string? UomCode = null);
