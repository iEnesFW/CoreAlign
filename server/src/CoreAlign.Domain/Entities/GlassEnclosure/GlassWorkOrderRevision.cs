using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Events;

namespace CoreAlign.Domain.Entities.GlassEnclosure;

public class GlassWorkOrderRevision : TenantEntity, IHasConcurrencyToken, ISoftDeletable
{
    public Guid WorkOrderId { get; private set; }
    public int RevisionNumber { get; private set; }
    public string? PreviousSnapshotJson { get; private set; }
    public string NewSnapshotJson { get; private set; } = string.Empty;
    public string? DeltaJson { get; private set; }
    public decimal DeltaPercent { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public WorkOrderRevisionStatus Status { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public Guid? ApprovedByUserId { get; private set; }
    public DateTime? ApprovedAtUtc { get; private set; }
    public string? RejectionReason { get; private set; }
    public string? OverrideReason { get; private set; }
    public long ConcurrencyToken { get; private set; }

    void IHasConcurrencyToken.BumpConcurrencyToken() => ConcurrencyToken++;

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public Guid? DeletedByUserId { get; set; }
    public string? DeletedReason { get; set; }

    public void MarkDeleted(Guid? userId, string? reason, DateTime utcNow)
    {
        ((ISoftDeletable)this).MarkDeletedInternal(userId, reason, utcNow);
        UpdatedAtUtc = utcNow;
    }

    public void Restore()
    {
        ((ISoftDeletable)this).RestoreInternal();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    protected GlassWorkOrderRevision() { }

    public GlassWorkOrderRevision(
        Guid workOrderId,
        int revisionNumber,
        string? previousSnapshotJson,
        string newSnapshotJson,
        string? deltaJson,
        decimal deltaPercent,
        string reason,
        WorkOrderRevisionStatus status,
        Guid createdByUserId)
    {
        WorkOrderId = workOrderId;
        RevisionNumber = revisionNumber;
        PreviousSnapshotJson = previousSnapshotJson;
        NewSnapshotJson = newSnapshotJson;
        DeltaJson = deltaJson;
        DeltaPercent = deltaPercent;
        Reason = reason;
        Status = status;
        CreatedByUserId = createdByUserId;
        AddDomainEvent(new GlassWorkOrderRevisionCreatedEvent(TenantId, Id, workOrderId, revisionNumber, status, deltaPercent, DateTime.UtcNow));
    }

    public void Approve(Guid userId, DateTime utcNow)
    {
        if (Status != WorkOrderRevisionStatus.PendingApproval)
            throw new InvalidOperationException("Only pending revisions can be approved");
        Status = WorkOrderRevisionStatus.Approved;
        ApprovedByUserId = userId;
        ApprovedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
        AddDomainEvent(new GlassWorkOrderRevisionApprovedEvent(TenantId, Id, WorkOrderId, RevisionNumber, utcNow));
    }

    public void OverrideBlock(Guid userId, string reason, DateTime utcNow)
    {
        if (Status != WorkOrderRevisionStatus.Blocked)
            throw new InvalidOperationException("Only blocked revisions can be overridden");
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Override reason is required for blocked revisions");
        Status = WorkOrderRevisionStatus.Approved;
        ApprovedByUserId = userId;
        ApprovedAtUtc = utcNow;
        OverrideReason = reason;
        UpdatedAtUtc = utcNow;
        AddDomainEvent(new GlassWorkOrderRevisionBlockOverriddenEvent(TenantId, Id, WorkOrderId, userId, reason, utcNow));
    }

    public void Reject(Guid userId, string reason, DateTime utcNow)
    {
        if (Status != WorkOrderRevisionStatus.PendingApproval && Status != WorkOrderRevisionStatus.Blocked)
            throw new InvalidOperationException("Only pending or blocked revisions can be rejected");
        Status = WorkOrderRevisionStatus.Rejected;
        ApprovedByUserId = userId;
        ApprovedAtUtc = utcNow;
        RejectionReason = reason;
        UpdatedAtUtc = utcNow;
        AddDomainEvent(new GlassWorkOrderRevisionRejectedEvent(TenantId, Id, WorkOrderId, RevisionNumber, reason, utcNow));
    }
}
