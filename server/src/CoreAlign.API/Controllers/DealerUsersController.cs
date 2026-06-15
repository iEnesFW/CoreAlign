using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.B2B;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/dealer-users")]
public class DealerUsersController : B2BControllerBase
{
    private readonly IMediator _mediator;
    public DealerUsersController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid dealerAccountId, CancellationToken ct)
        => (await _mediator.Send(new ListDealerUsersQuery(dealerAccountId, CurrentUserId(), CurrentRoles()), ct)).ToOk();

    [HttpPost]
    public async Task<IActionResult> Invite([FromBody] InviteDealerUserCommand cmd, CancellationToken ct)
        => (await _mediator.Send(cmd with { CurrentUserId = CurrentUserId(), CurrentUserRoles = CurrentRoles() }, ct)).ToCreated();

    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateDealerUserStatusCommand cmd, CancellationToken ct)
        => (await _mediator.Send(cmd with { Id = id, CurrentUserId = CurrentUserId(), CurrentUserRoles = CurrentRoles() }, ct)).ToOk();
}
