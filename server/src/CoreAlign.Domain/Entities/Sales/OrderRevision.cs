using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Domain.Entities.Sales;

public sealed class RevisionLineSnapshot
{
    public Guid ProductId { get; set; }
    public string ProductSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int LineNumber { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineDiscountPercent { get; set; }
    public decimal LineDiscountAmount { get; set; }
    public decimal TaxRatePercent { get; set; }
    public bool IsTaxInclusive { get; set; }
    public decimal WithholdingRatePercent { get; set; }
    public string? LineNotes { get; set; }
}

public class OrderRevision : TenantEntity
{
    public Guid OrderId { get; private set; }
    public int RevisionNumber { get; private set; }
    public Guid RequestedByUserId { get; private set; }
    public string RequestedByPersona { get; private set; } = string.Empty;
    public DateTime RequestedAtUtc { get; private set; }
    public RevisionStatus Status { get; private set; } = RevisionStatus.Proposed;

    public IList<RevisionLineSnapshot> ProposedLines { get; private set; } = new List<RevisionLineSnapshot>();

    public Guid? CounterpartyDecisionByUserId { get; private set; }
    public DateTime? DecidedAtUtc { get; private set; }
    public string? RejectionReason { get; private set; }
    public string? RequestNotes { get; private set; }

    protected OrderRevision() { }

    public OrderRevision(
        Guid orderId,
        int revisionNumber,
        Guid requestedByUserId,
        string requestedByPersona,
        IEnumerable<RevisionLineSnapshot> proposedLines,
        string? requestNotes,
        DateTime nowUtc)
    {
        if (orderId == Guid.Empty) throw new ArgumentException("Order id is required.", nameof(orderId));
        if (revisionNumber <= 0) throw new ArgumentException("Revision number must be positive.", nameof(revisionNumber));
        if (requestedByUserId == Guid.Empty) throw new ArgumentException("Requested-by user id is required.", nameof(requestedByUserId));
        if (string.IsNullOrWhiteSpace(requestedByPersona))
        {
            throw new ArgumentException("Requested-by persona is required.", nameof(requestedByPersona));
        }
        if (proposedLines is null) throw new ArgumentNullException(nameof(proposedLines));
        var snapshots = proposedLines.ToList();
        if (snapshots.Count == 0)
        {
            throw new InvalidRevisionStateException("A revision must contain at least one line.");
        }

        OrderId = orderId;
        RevisionNumber = revisionNumber;
        RequestedByUserId = requestedByUserId;
        RequestedByPersona = requestedByPersona;
        RequestedAtUtc = nowUtc;
        ProposedLines = snapshots;
        RequestNotes = string.IsNullOrWhiteSpace(requestNotes) ? null : requestNotes.Trim();
        Status = RevisionStatus.Proposed;
    }

    public bool IsPending => Status == RevisionStatus.Proposed;
    public bool IsTerminal =>
        Status is RevisionStatus.Approved
            or RevisionStatus.Rejected
            or RevisionStatus.Cancelled
            or RevisionStatus.Superseded;

    public void Approve(Guid decidedByUserId, DateTime nowUtc)
    {
        EnsurePending();
        if (decidedByUserId == Guid.Empty)
        {
            throw new ArgumentException("Decided-by user id is required.", nameof(decidedByUserId));
        }
        Status = RevisionStatus.Approved;
        CounterpartyDecisionByUserId = decidedByUserId;
        DecidedAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
    }

    public void Reject(Guid decidedByUserId, string reason, DateTime nowUtc)
    {
        EnsurePending();
        if (decidedByUserId == Guid.Empty)
        {
            throw new ArgumentException("Decided-by user id is required.", nameof(decidedByUserId));
        }
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new InvalidRevisionStateException("Rejection reason is required.");
        }
        Status = RevisionStatus.Rejected;
        CounterpartyDecisionByUserId = decidedByUserId;
        DecidedAtUtc = nowUtc;
        RejectionReason = reason.Trim();
        UpdatedAtUtc = nowUtc;
    }

    public void Cancel(Guid cancelledByUserId, DateTime nowUtc)
    {
        EnsurePending();
        if (cancelledByUserId != RequestedByUserId)
        {
            throw new RevisionPersonaNotAuthorizedException(RequestedByPersona, "cancel");
        }
        Status = RevisionStatus.Cancelled;
        DecidedAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
    }

    public void Supersede(DateTime nowUtc)
    {
        if (Status != RevisionStatus.Proposed)
        {
            return;
        }
        Status = RevisionStatus.Superseded;
        DecidedAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
    }

    private void EnsurePending()
    {
        if (Status != RevisionStatus.Proposed)
        {
            throw new InvalidRevisionStateException(
                $"Revision is in terminal state '{Status}' and cannot change.");
        }
    }
}
