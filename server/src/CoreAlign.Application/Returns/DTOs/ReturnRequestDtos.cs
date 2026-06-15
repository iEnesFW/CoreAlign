using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Returns.DTOs;

public class ReturnRequestLineDto
{
    public Guid Id { get; set; }
    public int LineNumber { get; set; }
    public Guid OrderLineId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public Guid? UomId { get; set; }
    public string? UomCode { get; set; }
    public decimal QuantityReturned { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TaxRatePercent { get; set; }
    public Guid? TaxRateId { get; set; }
    public bool IsTaxInclusive { get; set; }
    public decimal LineSubtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
    public bool Restockable { get; set; }
    public string? LineNotes { get; set; }
}

public class ReturnRequestDto
{
    public Guid Id { get; set; }
    public string ReturnNumber { get; set; } = string.Empty;
    public ReturnRequestStatus Status { get; set; }
    public ReturnReasonCode Reason { get; set; }
    public string? ReasonText { get; set; }
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public Guid? SourceInvoiceId { get; set; }
    public string? SourceInvoiceNumber { get; set; }
    public Guid? CreditNoteId { get; set; }
    public string? CreditNoteNumber { get; set; }
    public Guid? RefundPaymentId { get; set; }
    public DateTime RequestedAtUtc { get; set; }
    public Guid? RequestedByUserId { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public DateTime? RejectedAtUtc { get; set; }
    public Guid? RejectedByUserId { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime? ReceivedAtUtc { get; set; }
    public Guid? ReceivedByUserId { get; set; }
    public Guid? ReceivedAtWarehouseId { get; set; }
    public DateTime? CreditNoteIssuedAtUtc { get; set; }
    public DateTime? RefundedAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public string? InternalNotes { get; set; }
    public string? CustomerNotes { get; set; }
    public decimal LineSubtotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal Total { get; set; }
    public List<ReturnRequestLineDto> Lines { get; set; } = new();
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public class ReturnRequestSummaryDto
{
    public Guid Id { get; set; }
    public string ReturnNumber { get; set; } = string.Empty;
    public ReturnRequestStatus Status { get; set; }
    public ReturnReasonCode Reason { get; set; }
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public DateTime RequestedAtUtc { get; set; }
    public DateTime? ReceivedAtUtc { get; set; }
    public Guid? CreditNoteId { get; set; }
}
