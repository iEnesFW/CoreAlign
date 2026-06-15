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
[Route("api/v{version:apiVersion}/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public NotificationsController(IMediator mediator) => _mediator = mediator;

    private Guid CurrentUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] bool unreadOnly = false,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
        => (await _mediator.Send(new ListNotificationsQuery(unreadOnly, take, CurrentUserId()), cancellationToken)).ToOk();

    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount(CancellationToken cancellationToken)
        => (await _mediator.Send(new UnreadNotificationCountQuery(CurrentUserId()), cancellationToken)).ToOk();

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken cancellationToken)
        => (await _mediator.Send(new MarkNotificationReadCommand(id, CurrentUserId()), cancellationToken)).ToOk();

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
        => (await _mediator.Send(new MarkAllNotificationsReadCommand(CurrentUserId()), cancellationToken)).ToOk();
}
