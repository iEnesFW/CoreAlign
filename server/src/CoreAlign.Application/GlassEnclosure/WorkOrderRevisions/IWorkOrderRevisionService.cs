using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.GlassEnclosure.WorkOrderRevisions;

public interface IWorkOrderRevisionService
{
    Task<RevisionDecision?> CreateRevisionAsync(
        Guid workOrderId,
        string newSnapshotJson,
        decimal newTotal,
        string reason,
        CancellationToken cancellationToken = default);

    Task ApproveRevisionAsync(Guid revisionId, string? overrideReason, CancellationToken cancellationToken = default);

    Task RejectRevisionAsync(Guid revisionId, string reason, CancellationToken cancellationToken = default);
}

public sealed record RevisionDecision(
    Guid RevisionId,
    int RevisionNumber,
    WorkOrderRevisionStatus Status,
    decimal DeltaPercent);
