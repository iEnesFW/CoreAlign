using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Events;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Domain.Entities;

public class ReturnRequest : TenantEntity
{
    public string ReturnNumber { get; private set; } = string.Empty;
    public ReturnRequestStatus Status { get; private set; } = ReturnRequestStatus.Requested;
    public ReturnReasonCode Reason { get; private set; } = ReturnReasonCode.Other;
    public string? ReasonText { get; private set; }

    public Guid OrderId { get; private set; }
    public Guid CustomerId { get; private set; }
    public string CustomerNameSnapshot { get; private set; } = string.Empty;
    public string Currency { get; private set; } = "TRY";

    public Guid? SourceInvoiceId { get; private set; }
    public Guid? CreditNoteId { get; private set; }
    public Guid? RefundPaymentId { get; private set; }

    public DateTime RequestedAtUtc { get; private set; } = DateTime.UtcNow;
    public Guid? RequestedByUserId { get; private set; }

    public DateTime? ApprovedAtUtc { get; private set; }
    public Guid? ApprovedByUserId { get; private set; }
    public DateTime? RejectedAtUtc { get; private set; }
    public Guid? RejectedByUserId { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTime? ReceivedAtUtc { get; private set; }
    public Guid? ReceivedByUserId { get; private set; }
    public Guid? ReceivedAtWarehouseId { get; private set; }
    public DateTime? CreditNoteIssuedAtUtc { get; private set; }
    public DateTime? RefundedAtUtc { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }

    public string? InternalNotes { get; private set; }
    public string? CustomerNotes { get; private set; }

    public Order Order { get; set; } = null!;
    public Customer Customer { get; set; } = null!;
    public ICollection<ReturnRequestLine> Lines { get; private set; } = new List<ReturnRequestLine>();

    public decimal LineSubtotal => Math.Round(Lines.Sum(l => l.LineSubtotal), 4);
    public decimal TaxTotal => Math.Round(Lines.Sum(l => l.TaxAmount), 4);
    public decimal Total => Math.Round(LineSubtotal + TaxTotal, 4);

    public bool IsTerminal => Status is ReturnRequestStatus.Rejected
        or ReturnRequestStatus.Refunded
        or ReturnRequestStatus.Cancelled;

    protected ReturnRequest() { }

    public ReturnRequest(
        string returnNumber,
        Order order,
        ReturnReasonCode reason,
        string? reasonText,
        Guid? requestedByUserId,
        Guid? sourceInvoiceId,
        string? customerNotes)
    {
        ArgumentNullException.ThrowIfNull(order);
        if (string.IsNullOrWhiteSpace(returnNumber))
        {
            throw new ArgumentException("Return number is required.", nameof(returnNumber));
        }
        if (order.Status is not OrderStatus.Shipped
            and not OrderStatus.PartiallyShipped
            and not OrderStatus.Delivered
            and not OrderStatus.Closed
            and not OrderStatus.Returned)
        {
            throw new InvalidReturnRequestStateException(
                $"Return cannot be created for an order in status '{order.Status}'.");
        }

        ReturnNumber = returnNumber;
        TenantId = order.TenantId;
        OrderId = order.Id;
        CustomerId = order.CustomerId;
        CustomerNameSnapshot = order.Customer?.Name
            ?? order.CustomerSnapshot?.LegalName
            ?? string.Empty;
        Currency = order.Currency;
        Reason = reason;
        ReasonText = reasonText;
        RequestedByUserId = requestedByUserId;
        SourceInvoiceId = sourceInvoiceId;
        CustomerNotes = customerNotes;
        RequestedAtUtc = DateTime.UtcNow;
    }

    public void AddLine(ReturnRequestLine line)
    {
        ArgumentNullException.ThrowIfNull(line);
        EnsureRequested();
        line.TenantId = TenantId;
        Lines.Add(line);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void ReplaceLines(IEnumerable<ReturnRequestLine> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        EnsureRequested();
        Lines.Clear();
        var lineNumber = 1;
        foreach (var l in lines)
        {
            l.TenantId = TenantId;
            l.SetLineNumber(lineNumber++);
            Lines.Add(l);
        }
        if (Lines.Count == 0)
        {
            throw new InvalidReturnRequestStateException("A return request must have at least one line.");
        }
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Approve(Guid approvedByUserId)
    {
        if (Status != ReturnRequestStatus.Requested)
        {
            throw new InvalidReturnRequestStateException(
                $"Return cannot be approved from status '{Status}'.");
        }
        if (Lines.Count == 0)
        {
            throw new InvalidReturnRequestStateException("Cannot approve a return with no lines.");
        }
        var now = DateTime.UtcNow;
        Status = ReturnRequestStatus.Approved;
        ApprovedByUserId = approvedByUserId;
        ApprovedAtUtc = now;
        UpdatedAtUtc = now;
        AddDomainEvent(new ReturnRequestApprovedEvent(TenantId, Id, ReturnNumber, OrderId, CustomerId, now));
    }

    public void Reject(Guid rejectedByUserId, string? reason)
    {
        if (Status != ReturnRequestStatus.Requested)
        {
            throw new InvalidReturnRequestStateException(
                $"Return cannot be rejected from status '{Status}'.");
        }
        var now = DateTime.UtcNow;
        Status = ReturnRequestStatus.Rejected;
        RejectedByUserId = rejectedByUserId;
        RejectedAtUtc = now;
        RejectionReason = reason;
        UpdatedAtUtc = now;
        AddDomainEvent(new ReturnRequestRejectedEvent(TenantId, Id, ReturnNumber, OrderId, CustomerId, reason, now));
    }

    public void Cancel()
    {
        if (Status is ReturnRequestStatus.Rejected
            or ReturnRequestStatus.Refunded
            or ReturnRequestStatus.Cancelled
            or ReturnRequestStatus.CreditNoted
            or ReturnRequestStatus.Received)
        {
            throw new InvalidReturnRequestStateException(
                $"Return cannot be cancelled from status '{Status}'.");
        }
        var now = DateTime.UtcNow;
        Status = ReturnRequestStatus.Cancelled;
        CancelledAtUtc = now;
        UpdatedAtUtc = now;
        AddDomainEvent(new ReturnRequestCancelledEvent(TenantId, Id, ReturnNumber, OrderId, CustomerId, now));
    }

    public void MarkReceived(Guid receivedByUserId, Guid warehouseId)
    {
        if (Status != ReturnRequestStatus.Approved)
        {
            throw new InvalidReturnRequestStateException(
                $"Return cannot be received from status '{Status}'.");
        }
        if (warehouseId == Guid.Empty)
        {
            throw new InvalidReturnRequestStateException("Warehouse is required to receive a return.");
        }
        var now = DateTime.UtcNow;
        Status = ReturnRequestStatus.Received;
        ReceivedByUserId = receivedByUserId;
        ReceivedAtWarehouseId = warehouseId;
        ReceivedAtUtc = now;
        UpdatedAtUtc = now;

        // Only restockable lines re-enter sellable inventory (and reverse their COGS). A damaged /
        // quarantined line (Restockable == false) must NOT be put back into stock — its cost stays
        // recognized as COGS (the goods are a loss), pending a dedicated scrap flow.
        var snapshot = Lines
            .Where(l => l.Restockable)
            .Select(l => new ReturnRequestLineSnapshot(l.Id, l.ProductId, l.QuantityReturned, l.UnitPrice, l.UnitCostSnapshot))
            .ToList();
        AddDomainEvent(new ReturnRequestReceivedEvent(
            TenantId, Id, ReturnNumber, OrderId, CustomerId, warehouseId, snapshot, now));
    }

    public void AttachCreditNote(Guid creditNoteId)
    {
        if (Status != ReturnRequestStatus.Received)
        {
            throw new InvalidReturnRequestStateException(
                $"Credit note can only be attached after the return is received (current: {Status}).");
        }
        if (CreditNoteId.HasValue)
        {
            throw new InvalidReturnRequestStateException("A credit note is already attached to this return.");
        }
        var now = DateTime.UtcNow;
        Status = ReturnRequestStatus.CreditNoted;
        CreditNoteId = creditNoteId;
        CreditNoteIssuedAtUtc = now;
        UpdatedAtUtc = now;
        AddDomainEvent(new ReturnRequestCreditNotedEvent(
            TenantId, Id, ReturnNumber, OrderId, CustomerId, creditNoteId, now));
    }

    public void MarkRefunded(Guid refundPaymentId)
    {
        if (Status != ReturnRequestStatus.CreditNoted)
        {
            throw new InvalidReturnRequestStateException(
                $"Return cannot be refunded from status '{Status}'.");
        }
        var now = DateTime.UtcNow;
        Status = ReturnRequestStatus.Refunded;
        RefundPaymentId = refundPaymentId;
        RefundedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public void SetInternalNotes(string? notes)
    {
        InternalNotes = notes;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private void EnsureRequested()
    {
        if (Status != ReturnRequestStatus.Requested)
        {
            throw new InvalidReturnRequestStateException(
                $"Return lines can only be modified while in Requested status (current: {Status}).");
        }
    }
}
