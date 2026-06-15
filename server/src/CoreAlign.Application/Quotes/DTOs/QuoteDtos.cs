using CoreAlign.Application.Orders.DTOs;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Quotes.DTOs;

public class QuoteLineDto
{
    public Guid Id { get; set; }
    public int LineNumber { get; set; }
    public Guid ProductId { get; set; }
    public string ProductSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? ProductDescription { get; set; }
    public Guid? UomId { get; set; }
    public string? UomCode { get; set; }
    public decimal UomConversionFactor { get; set; } = 1m;
    public decimal Quantity { get; set; }
    public decimal ListPriceSnapshot { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineDiscountPercent { get; set; }
    public decimal LineDiscountAmount { get; set; }
    public bool IsManualPriceOverride { get; set; }
    public Guid? TaxRateId { get; set; }
    public decimal TaxRatePercent { get; set; }
    public decimal TaxAmount { get; set; }
    public bool IsTaxInclusive { get; set; }
    public decimal WithholdingRatePercent { get; set; }
    public decimal WithholdingAmount { get; set; }
    public decimal LineSubtotal { get; set; }
    public decimal LineNetAmount { get; set; }
    public decimal LineTotal { get; set; }
    public string? LineNotes { get; set; }
}

public class QuoteDto
{
    public Guid Id { get; set; }
    public string QuoteNumber { get; set; } = string.Empty;
    public QuoteStatus Status { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public Guid? BillingAddressId { get; set; }
    public Guid? ShippingAddressId { get; set; }
    public CustomerSnapshotDto? CustomerSnapshot { get; set; }
    public AddressSnapshotDto? BillingAddressSnapshot { get; set; }
    public AddressSnapshotDto? ShippingAddressSnapshot { get; set; }
    public DateTime QuoteDate { get; set; }
    public DateTime ValidUntilUtc { get; set; }
    public DateTime? SentAtUtc { get; set; }
    public DateTime? AcceptedAtUtc { get; set; }
    public DateTime? RejectedAtUtc { get; set; }
    public DateTime? ExpiredAtUtc { get; set; }
    public DateTime? ConvertedAtUtc { get; set; }
    public string Currency { get; set; } = string.Empty;
    public decimal ExchangeRate { get; set; }
    public Guid? PaymentTermsId { get; set; }
    public int? PaymentTermsNetDaysSnapshot { get; set; }
    public Guid? PriceListId { get; set; }
    public Guid? SalesRepUserId { get; set; }
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
    public Guid? ConvertedOrderId { get; set; }
    public string? RejectionReason { get; set; }
    public string? InternalNotes { get; set; }
    public string? CustomerNotes { get; set; }
    public string? PublicNotes { get; set; }
    public string? TermsAndConditions { get; set; }
    public string? Notes { get; set; }
    public List<QuoteLineDto> Lines { get; set; } = new();
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public class QuoteSummaryDto
{
    public Guid Id { get; set; }
    public string QuoteNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public DateTime QuoteDate { get; set; }
    public DateTime ValidUntilUtc { get; set; }
    public QuoteStatus Status { get; set; }
    public string Currency { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public Guid? ConvertedOrderId { get; set; }
}
