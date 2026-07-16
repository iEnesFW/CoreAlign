using System.Security.Claims;
using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.GlassPlates.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize(Roles = "TenantAdmin")]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/user-warehouse-access")]
public class UserWarehouseAccessController : ControllerBase
{
    private readonly IMediator _mediator;
    public UserWarehouseAccessController(IMediator mediator) => _mediator = mediator;

    private Guid CurrentUserId =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : Guid.Empty;

    [HttpPost]
    public async Task<IActionResult> Assign([FromBody] AssignUserWarehousesCommand command, CancellationToken ct)
        => (await _mediator.Send(command with { GrantedByUserId = CurrentUserId }, ct)).ToOk();

    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> Get(Guid userId, CancellationToken ct)
        => (await _mediator.Send(new GetUserWarehouseAccessQuery(userId), ct)).ToOk();
}
