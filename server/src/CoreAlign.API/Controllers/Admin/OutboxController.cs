using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Common;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers.Admin;

/// <summary>
/// Operator view over the tenant's outbox. A message whose handler was missing dead-letters
/// silently — the drain logs a success and nothing is ever posted — so there has to be a way to
/// see what is stuck and to requeue it once the handler exists.
/// </summary>
[ApiController]
[Authorize(Roles = "PlatformAdmin,TenantAdmin")]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/outbox")]
public class OutboxController : ControllerBase
{
    private readonly IMediator _mediator;

    public OutboxController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [ProducesResponseType<ApiResponse<IReadOnlyList<OutboxMessageDto>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] OutboxStatus? status,
        [FromQuery] int max,
        CancellationToken cancellationToken)
        => (await _mediator.Send(new ListOutboxMessagesQuery(status, max <= 0 ? 100 : max), cancellationToken)).ToOk();

    /// <summary>
    /// Requeues every Deferred/Failed/DeadLetter message of the CURRENT tenant and drains at once.
    /// Safe to repeat: each handler dedups on its own natural key (GL posting on
    /// (SourceType, SourceDocumentId)), so a replay of an already-posted message is a no-op.
    /// </summary>
    [HttpPost("replay")]
    [ProducesResponseType<ApiResponse<int>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Replay(CancellationToken cancellationToken)
        => (await _mediator.Send(new ReplayOutboxCommand(), cancellationToken)).ToOk();
}
