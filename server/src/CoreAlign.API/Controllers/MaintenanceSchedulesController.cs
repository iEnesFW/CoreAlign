using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Common;
using CoreAlign.Application.Warranty;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/maintenance-schedules")]
public class MaintenanceSchedulesController : ControllerBase
{
    private readonly IMediator _mediator;
    public MaintenanceSchedulesController(IMediator mediator) => _mediator = mediator;

    private static IActionResult RouteIdMismatch() =>
        new BadRequestObjectResult(ApiResponse<object>.Failure("Route id does not match command id.", 400));

    [HttpGet("due")]
    public async Task<IActionResult> Due([FromQuery] DateTime? asOf, CancellationToken ct)
    {
        var date = asOf ?? DateTime.UtcNow;
        return (await _mediator.Send(new ListMaintenanceSchedulesDueQuery(date), ct)).ToOk();
    }

    [HttpPost]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateMaintenanceScheduleCommand cmd, CancellationToken ct)
        => (await _mediator.Send(cmd, ct)).ToCreated();

    [HttpPost("{id:guid}/complete")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Complete(Guid id, [FromBody] CompleteScheduledMaintenanceCommand cmd, CancellationToken ct)
        => id != cmd.Id ? RouteIdMismatch() : (await _mediator.Send(cmd, ct)).ToOk();
}
