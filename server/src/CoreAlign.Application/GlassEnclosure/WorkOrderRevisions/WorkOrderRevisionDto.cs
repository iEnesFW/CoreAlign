using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.GlassEnclosure.WorkOrderRevisions;

public sealed record WorkOrderRevisionDto(
    Guid Id,
    Guid WorkOrderId,
    int RevisionNumber,
    decimal DeltaPercent,
    WorkOrderRevisionStatus Status,
    string Reason,
    string? RejectionReason,
    string? OverrideReason,
    string? PreviousSnapshotJson,
    string NewSnapshotJson,
    string? DeltaJson,
    Guid CreatedByUserId,
    Guid? ApprovedByUserId,
    DateTime CreatedAtUtc,
    DateTime? ApprovedAtUtc);
