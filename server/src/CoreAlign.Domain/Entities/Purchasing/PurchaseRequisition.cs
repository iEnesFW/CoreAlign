using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Domain.Entities.Purchasing;

public class PurchaseRequisition : TenantEntity, IHasConcurrencyToken, ISoftDeletable
{
    public string Number { get; private set; } = string.Empty;
    public PurchaseRequisitionStatus Status { get; private set; } = PurchaseRequisitionStatus.Draft;
    public PurchaseRequisitionReason Reason { get; private set; } = PurchaseRequisitionReason.Manual;
    public DateTime RequestedAtUtc { get; private set; } = DateTime.UtcNow;
    public Guid RequestedByUserId { get; private set; }
    public string? Notes { get; private set; }

    public DateTime? SubmittedAtUtc { get; private set; }
    public Guid? ApprovedByUserId { get; private set; }
    public DateTime? ApprovedAtUtc { get; private set; }
    public DateTime? RejectedAtUtc { get; private set; }
    public string? RejectReason { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }
    public string? CancelReason { get; private set; }
    public DateTime? ConvertedAtUtc { get; private set; }
    public Guid? ConvertedPurchaseOrderId { get; private set; }

    public long ConcurrencyToken { get; private set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public Guid? DeletedByUserId { get; set; }
    public string? DeletedReason { get; set; }

    public ICollection<PurchaseRequisitionLine> Lines { get; private set; } = new List<PurchaseRequisitionLine>();

    public bool IsEditable => Status == PurchaseRequisitionStatus.Draft;

    protected PurchaseRequisition() { }

    public PurchaseRequisition(
        string number,
        Guid requestedByUserId,
        PurchaseRequisitionReason reason,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(number))
        {
            throw new ArgumentException("Requisition number is required.", nameof(number));
        }
        Number = number.Trim();
        RequestedByUserId = requestedByUserId;
        Reason = reason;
        Notes = notes;
        RequestedAtUtc = DateTime.UtcNow;
    }

    public void BumpConcurrencyToken() => ConcurrencyToken++;

    public void MarkDeleted(Guid? userId, string? reason, DateTime utcNow)
    {
        IsDeleted = true;
        DeletedAtUtc = utcNow;
        DeletedByUserId = userId;
        DeletedReason = reason;
        UpdatedAtUtc = utcNow;
    }

    public void Restore()
    {
        IsDeleted = false;
        DeletedAtUtc = null;
        DeletedByUserId = null;
        DeletedReason = null;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void ReplaceLines(IEnumerable<PurchaseRequisitionLine> newLines)
    {
        EnsureEditable();
        Lines.Clear();
        var i = 1;
        foreach (var line in newLines)
        {
            line.SetLineNumber(i++);
            Lines.Add(line);
        }
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateHeader(PurchaseRequisitionReason reason, string? notes)
    {
        EnsureEditable();
        Reason = reason;
        Notes = notes;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Submit()
    {
        if (Status != PurchaseRequisitionStatus.Draft)
        {
            throw new InvalidOrderStatusTransitionException(Status.ToString(), PurchaseRequisitionStatus.Submitted.ToString());
        }
        if (Lines.Count == 0)
        {
            throw new InvalidOrderLineException("Cannot submit a requisition with no lines.");
        }
        Status = PurchaseRequisitionStatus.Submitted;
        SubmittedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = SubmittedAtUtc.Value;
    }

    public void Approve(Guid approvedByUserId)
    {
        if (Status != PurchaseRequisitionStatus.Submitted)
        {
            throw new InvalidOrderStatusTransitionException(Status.ToString(), PurchaseRequisitionStatus.Approved.ToString());
        }
        Status = PurchaseRequisitionStatus.Approved;
        ApprovedByUserId = approvedByUserId;
        ApprovedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = ApprovedAtUtc.Value;
    }

    public void Reject(string? reason)
    {
        if (Status != PurchaseRequisitionStatus.Submitted)
        {
            throw new InvalidOrderStatusTransitionException(Status.ToString(), PurchaseRequisitionStatus.Rejected.ToString());
        }
        Status = PurchaseRequisitionStatus.Rejected;
        RejectedAtUtc = DateTime.UtcNow;
        RejectReason = reason;
        UpdatedAtUtc = RejectedAtUtc.Value;
    }

    public void Cancel(string? reason)
    {
        if (Status is PurchaseRequisitionStatus.Converted or PurchaseRequisitionStatus.Cancelled)
        {
            throw new InvalidOrderStatusTransitionException(Status.ToString(), PurchaseRequisitionStatus.Cancelled.ToString());
        }
        Status = PurchaseRequisitionStatus.Cancelled;
        CancelledAtUtc = DateTime.UtcNow;
        CancelReason = reason;
        UpdatedAtUtc = CancelledAtUtc.Value;
    }

    public void MarkConverted(Guid purchaseOrderId)
    {
        if (Status != PurchaseRequisitionStatus.Approved)
        {
            throw new InvalidOrderStatusTransitionException(Status.ToString(), PurchaseRequisitionStatus.Converted.ToString());
        }
        Status = PurchaseRequisitionStatus.Converted;
        ConvertedPurchaseOrderId = purchaseOrderId;
        ConvertedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = ConvertedAtUtc.Value;
    }

    private void EnsureEditable()
    {
        if (!IsEditable)
        {
            throw new OrderImmutableException(Status.ToString());
        }
    }
}
