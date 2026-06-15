using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.GlassEnclosure.WorkOrderRevisions.Queries;

public record ListWorkOrderRevisionsQuery(Guid WorkOrderId)
    : IRequest<IReadOnlyList<WorkOrderRevisionDto>>;

public class ListWorkOrderRevisionsQueryHandler
    : IRequestHandler<ListWorkOrderRevisionsQuery, IReadOnlyList<WorkOrderRevisionDto>>
{
    private readonly IGlassWorkOrderRevisionRepository _revisions;

    public ListWorkOrderRevisionsQueryHandler(IGlassWorkOrderRevisionRepository revisions)
    {
        _revisions = revisions;
    }

    public async Task<IReadOnlyList<WorkOrderRevisionDto>> Handle(
        ListWorkOrderRevisionsQuery request,
        CancellationToken cancellationToken)
    {
        var revisions = await _revisions.ListByWorkOrderAsync(request.WorkOrderId, cancellationToken);

        var result = new List<WorkOrderRevisionDto>(revisions.Count);
        foreach (var revision in revisions)
        {
            result.Add(new WorkOrderRevisionDto(
                revision.Id,
                revision.WorkOrderId,
                revision.RevisionNumber,
                revision.DeltaPercent,
                revision.Status,
                revision.Reason,
                revision.RejectionReason,
                revision.OverrideReason,
                revision.PreviousSnapshotJson,
                revision.NewSnapshotJson,
                revision.DeltaJson,
                revision.CreatedByUserId,
                revision.ApprovedByUserId,
                revision.CreatedAtUtc,
                revision.ApprovedAtUtc));
        }

        return result;
    }
}
