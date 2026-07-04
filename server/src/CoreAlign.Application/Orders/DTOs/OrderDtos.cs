using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Orders.DTOs;

public class AddressSnapshotDto
{
    public string? Label { get; set; }
    public string? RecipientName { get; set; }
    public string? Phone { get; set; }
    public string Line1 { get; set; } = string.Empty;
    public string? Line2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
}

public class CustomerSnapshotDto
{
    public string? Code { get; set; }
    public string LegalName { get; set; } = string.Empty;
    public string? TradeName { get; set; }
    public string? TaxNumber { get; set; }
    public string? TaxOffice { get; set; }
    public string? NationalId { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
}

public class OrderLineDto
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
    public decimal QuantityAllocated { get; set; }
    public decimal QuantityShipped { get; set; }
    public decimal QuantityInvoiced { get; set; }
    public decimal QuantityReturned { get; set; }
    public decimal QuantityCancelled { get; set; }
    public decimal QuantityRemainingToShip { get; set; }
    public decimal QuantityRemainingToInvoice { get; set; }
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
    public Guid? WithholdingTaxCodeId { get; set; }
    public string? WithholdingCode { get; set; }
    public int? WithholdingNumerator { get; set; }
    public int? WithholdingDenominator { get; set; }
    public decimal LineSubtotal { get; set; }
    public decimal LineNetAmount { get; set; }
    public decimal LineTotal { get; set; }
    public decimal UnitCostSnapshot { get; set; }
    public Guid? WarehouseId { get; set; }
    public OrderLineStatus Status { get; set; }
    public string? LineNotes { get; set; }
}

public class OrderDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public OrderType Type { get; set; }
    public OrderStatus Status { get; set; }
    public OrderSource Source { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public Guid? BillingAddressId { get; set; }
    public Guid? ShippingAddressId { get; set; }
    public CustomerSnapshotDto? CustomerSnapshot { get; set; }
    public AddressSnapshotDto? BillingAddressSnapshot { get; set; }
    public AddressSnapshotDto? ShippingAddressSnapshot { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime? RequestedDeliveryDate { get; set; }
    public DateTime? PromisedDeliveryDate { get; set; }
    public DateTime? ActualDeliveryDate { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public string Currency { get; set; } = string.Empty;
    public decimal ExchangeRate { get; set; }
    public Guid? PaymentTermsId { get; set; }
    public int? PaymentTermsNetDaysSnapshot { get; set; }
    public DateTime? DueDate { get; set; }
    public Guid? PriceListId { get; set; }
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
    public Guid? SalesRepUserId { get; set; }
    public string? Channel { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public Guid? OriginOrderId { get; set; }
    public string? CancelReason { get; set; }
    public string? InternalNotes { get; set; }
    public string? CustomerNotes { get; set; }
    public string? Notes { get; set; }
    public List<OrderLineDto> Lines { get; set; } = new();
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public string? OriginDealerName { get; set; }
    public string? DealerApprovedByName { get; set; }
    public string? OriginPersona { get; set; }
    public string? DealerApprovalStatus { get; set; }
    public Guid? OriginDealerAccountId { get; set; }
    public Guid? OriginDealerUserId { get; set; }
    public Guid? OriginCustomerUserId { get; set; }
}

public class OrderSummaryDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public OrderStatus Status { get; set; }
    public string Currency { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public Guid? InvoiceId { get; set; }
    public string? InvoiceNumber { get; set; }
    public Guid? ShipmentId { get; set; }
    public string? ShipmentNumber { get; set; }
}
