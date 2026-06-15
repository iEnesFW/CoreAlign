using CoreAlign.Application.Common;
using CoreAlign.Domain.Common;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.GlassEnclosure.WorkOrderRevisions.Commands;

public record ApproveWorkOrderRevisionCommand(Guid RevisionId, string? OverrideReason)
    : IRequest<Unit>, ITransactionalRequest, IAuditableMutation
{
    public Guid? ParentWorkOrderId { get; init; }
    public Guid AggregateId => RevisionId;
    public string AggregateType => "GlassWorkOrderRevision";
}

public class ApproveWorkOrderRevisionCommandHandler : IRequestHandler<ApproveWorkOrderRevisionCommand, Unit>
{
    private readonly IWorkOrderRevisionService _service;
    private readonly IGlassWorkOrderRevisionRepository _revisions;

    public ApproveWorkOrderRevisionCommandHandler(
        IWorkOrderRevisionService service,
        IGlassWorkOrderRevisionRepository revisions)
    {
        _service = service;
        _revisions = revisions;
    }

    public async Task<Unit> Handle(ApproveWorkOrderRevisionCommand request, CancellationToken cancellationToken)
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

        await _service.ApproveRevisionAsync(request.RevisionId, request.OverrideReason, cancellationToken);
        return Unit.Value;
    }
}
