using System.Security.Claims;
using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Users;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize(Roles = "TenantAdmin")]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/users")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    public UsersController(IMediator mediator) => _mediator = mediator;

    private Guid CurrentUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
        => (await _mediator.Send(new ListUsersQuery(), ct)).ToOk();

    [HttpGet("roles")]
    public async Task<IActionResult> ListRoles(CancellationToken ct)
        => (await _mediator.Send(new ListRolesQuery(), ct)).ToOk();

    [HttpPost]
    public async Task<IActionResult> Invite([FromBody] InviteUserCommand cmd, CancellationToken ct)
        => (await _mediator.Send(cmd with { InvitedByUserId = CurrentUserId() }, ct)).ToCreated();

    [HttpPut("{id:guid}/roles")]
    public async Task<IActionResult> UpdateRoles(Guid id, [FromBody] UpdateUserRolesCommand cmd, CancellationToken ct)
        => (await _mediator.Send(cmd with { UserId = id }, ct)).ToOk();

    [HttpPut("{id:guid}/active")]
    public async Task<IActionResult> SetActive(Guid id, [FromBody] SetUserActiveCommand cmd, CancellationToken ct)
        => (await _mediator.Send(cmd with { UserId = id, CurrentUserId = CurrentUserId() }, ct)).ToOk();
}
