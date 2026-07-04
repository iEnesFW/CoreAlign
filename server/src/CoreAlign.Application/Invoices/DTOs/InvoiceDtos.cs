using CoreAlign.Application.Orders.DTOs;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Invoices.DTOs;

public class InvoiceLineDto
{
    public Guid Id { get; set; }
    public int LineNumber { get; set; }
    public Guid? ProductId { get; set; }
    public string ProductSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? UomId { get; set; }
    public string? UomCode { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineDiscountPercent { get; set; }
    public decimal LineDiscountAmount { get; set; }
    public Guid? TaxRateId { get; set; }
    public decimal TaxRatePercent { get; set; }
    public decimal TaxAmount { get; set; }
    public bool IsTaxInclusive { get; set; }
    public decimal WithholdingRatePercent { get; set; }
    public decimal WithholdingAmount { get; set; }
    public decimal LineSubtotal { get; set; }
    public decimal LineNetAmount { get; set; }
    public decimal LineTotal { get; set; }
    public string? RevenueAccountCode { get; set; }
    public Guid? OriginOrderLineId { get; set; }
}

public class TaxBreakdownItem
{
    public decimal Rate { get; set; }
    public decimal Base { get; set; }
    public decimal Amount { get; set; }
}

public class InvoiceDto
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public InvoiceType Type { get; set; }
    public InvoiceStatus Status { get; set; }
    public Guid? OrderId { get; set; }
    public Guid? OriginInvoiceId { get; set; }
    public Guid? CreditNoteId { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public CustomerSnapshotDto? CustomerSnapshot { get; set; }
    public AddressSnapshotDto? BillingAddressSnapshot { get; set; }
    public AddressSnapshotDto? ShippingAddressSnapshot { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime PostingDate { get; set; }
    public DateTime? IssuedAtUtc { get; set; }
    public DateTime? SentAtUtc { get; set; }
    public DateTime? PaidAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public DateTime? VoidedAtUtc { get; set; }
    public string Currency { get; set; } = string.Empty;
    public decimal ExchangeRate { get; set; }
    public Guid? PaymentTermsId { get; set; }
    public int? PaymentTermsNetDaysSnapshot { get; set; }
    public decimal Subtotal { get; set; }
    public decimal LineDiscountTotal { get; set; }
    public decimal HeaderDiscountAmount { get; set; }
    public decimal HeaderDiscountPercent { get; set; }
    public decimal TaxableTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal WithholdingTotal { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal RoundingAdjustment { get; set; }
    public decimal Total { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal AmountDue { get; set; }
    public List<TaxBreakdownItem> TaxBreakdown { get; set; } = new();
    public string? CancelReason { get; set; }
    public string? VoidReason { get; set; }
    public string? InternalNotes { get; set; }
    public string? PublicNotes { get; set; }
    public string? TermsAndConditions { get; set; }
    public string? Notes { get; set; }
    public string? EInvoiceUuid { get; set; }
    public string? EInvoiceStatus { get; set; }
    public bool IsPostedToLedger { get; set; }
    public bool IsOverdue { get; set; }
    public List<InvoiceLineDto> Lines { get; set; } = new();
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public class CreditedLineQuantityDto
{
    public Guid InvoiceLineId { get; set; }
    public decimal CreditedQuantity { get; set; }
}

public class InvoiceSummaryDto
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public InvoiceType Type { get; set; }
    public Guid? OrderId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public DateTime DueDate { get; set; }
    public InvoiceStatus Status { get; set; }
    public string Currency { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal AmountDue { get; set; }
    public bool IsOverdue { get; set; }
    public string? OrderNumber { get; set; }
}
