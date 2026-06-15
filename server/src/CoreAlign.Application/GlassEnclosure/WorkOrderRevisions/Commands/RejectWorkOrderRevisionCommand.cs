using CoreAlign.Application.Common;
using CoreAlign.Domain.Common;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.GlassEnclosure.WorkOrderRevisions.Commands;

public record RejectWorkOrderRevisionCommand(Guid RevisionId, string Reason)
    : IRequest<Unit>, ITransactionalRequest, IAuditableMutation
{
    public Guid? ParentWorkOrderId { get; init; }
    public Guid AggregateId => RevisionId;
    public string AggregateType => "GlassWorkOrderRevision";
}

public class RejectWorkOrderRevisionCommandHandler : IRequestHandler<RejectWorkOrderRevisionCommand, Unit>
{
    private readonly IWorkOrderRevisionService _service;
    private readonly IGlassWorkOrderRevisionRepository _revisions;

    public RejectWorkOrderRevisionCommandHandler(
        IWorkOrderRevisionService service,
        IGlassWorkOrderRevisionRepository revisions)
    {
        _service = service;
        _revisions = revisions;
    }

    public async Task<Unit> Handle(RejectWorkOrderRevisionCommand request, CancellationToken cancellationToken)
    {
        if (request.ParentWorkOrderId.HasValue)
        {
            var revision = await _revisions.GetByIdAsync(request.RevisionId, cancellationToken)
                ?? throw new GlassWorkOrderRevisionNotFoundException(request.RevisionId);

            if (revision.WorkOrderId != request.ParentWorkOrderId.Value)
            {
                throw new GlassWorkOrderRevisionMismatchException(
                    request.RevisionId,
                    request.ParentWorkOrderId.Value,
                    revision.WorkOrderId);
            }
        }

        await _service.RejectRevisionAsync(request.RevisionId, request.Reason, cancellationToken);
        return Unit.Value;
    }
}
