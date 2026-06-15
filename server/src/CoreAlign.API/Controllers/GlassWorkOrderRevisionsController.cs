using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.GlassEnclosure.Authorization;
using CoreAlign.Application.GlassEnclosure.WorkOrderRevisions.Commands;
using CoreAlign.Application.GlassEnclosure.WorkOrderRevisions.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/glass-enclosure/work-orders")]
public class GlassWorkOrderRevisionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public GlassWorkOrderRevisionsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("{id:guid}/revisions")]
    public async Task<IActionResult> List(Guid id, CancellationToken ct) =>
        (await _mediator.Send(new ListWorkOrderRevisionsQuery(id), ct)).ToOk();

    [HttpPost("{id:guid}/revisions/{revisionId:guid}/approve")]
    [Authorize(Policy = GlassEnclosurePolicies.WorkOrderRevisionApprove)]
    public async Task<IActionResult> Approve(Guid id, Guid revisionId, [FromBody] ApproveWorkOrderRevisionBody? body, CancellationToken ct)
    {
        var command = new ApproveWorkOrderRevisionCommand(revisionId, body?.OverrideReason)
        {
            ParentWorkOrderId = id,
        };
        await _mediator.Send(command, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/revisions/{revisionId:guid}/reject")]
    [Authorize(Policy = GlassEnclosurePolicies.WorkOrderRevisionReject)]
    public async Task<IActionResult> Reject(Guid id, Guid revisionId, [FromBody] RejectWorkOrderRevisionBody body, CancellationToken ct)
    {
        var command = new RejectWorkOrderRevisionCommand(revisionId, body.Reason)
        {
            ParentWorkOrderId = id,
        };
        await _mediator.Send(command, ct);
        return NoContent();
    }
}

public record RejectWorkOrderRevisionBody(string Reason);

public record ApproveWorkOrderRevisionBody(string? OverrideReason);
