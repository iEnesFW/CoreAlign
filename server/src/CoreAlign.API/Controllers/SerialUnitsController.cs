using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Inventory.Serials;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/serial-units")]
public class SerialUnitsController : ControllerBase
{
    private readonly IMediator _mediator;
    public SerialUnitsController(IMediator mediator) => _mediator = mediator;

    [HttpPost("register")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Register([FromBody] RegisterSerialUnitsCommand command, CancellationToken ct)
        => (await _mediator.Send(command, ct)).ToOk();

    [HttpPost("ship")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Ship([FromBody] ShipSerialUnitsCommand command, CancellationToken ct)
        => (await _mediator.Send(command, ct)).ToOk();

    [HttpGet("where-used/{serialNumber}")]
    public async Task<IActionResult> WhereUsed(string serialNumber, CancellationToken ct)
        => (await _mediator.Send(new GetSerialWhereUsedQuery(serialNumber), ct)).ToOk();
}
