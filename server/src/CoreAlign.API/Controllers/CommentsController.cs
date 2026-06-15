using System.Security.Claims;
using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Collaboration;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/comments")]
public class CommentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public CommentsController(IMediator mediator) => _mediator = mediator;

    private Guid CurrentUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private bool IsTenantAdmin() => User.IsInRole("TenantAdmin");

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string entityType,
        [FromQuery] Guid entityId,
        CancellationToken cancellationToken)
        => (await _mediator.Send(new ListCommentsQuery(entityType, entityId), cancellationToken)).ToOk();

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCommentCommand command, CancellationToken cancellationToken)
        => (await _mediator.Send(command with { AuthorUserId = CurrentUserId() }, cancellationToken)).ToCreated();

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Edit(Guid id, [FromBody] EditCommentCommand command, CancellationToken cancellationToken)
        => (await _mediator.Send(command with { Id = id, CurrentUserId = CurrentUserId() }, cancellationToken)).ToOk();

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        => (await _mediator.Send(new DeleteCommentCommand(id, CurrentUserId(), IsTenantAdmin()), cancellationToken)).ToOk();
}
