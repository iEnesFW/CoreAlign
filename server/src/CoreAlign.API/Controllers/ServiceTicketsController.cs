using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Common;
using CoreAlign.Application.Warranty;
using CoreAlign.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/service-tickets")]
public class ServiceTicketsController : ControllerBase
{
    private readonly IMediator _mediator;
    public ServiceTicketsController(IMediator mediator) => _mediator = mediator;

    private static IActionResult RouteIdMismatch() =>
        new BadRequestObjectResult(ApiResponse<object>.Failure("Route id does not match command id.", 400));

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] ServiceTicketStatus? status,
        [FromQuery] ServiceTicketType? type,
        [FromQuery] ServiceTicketPriority? priority,
        [FromQuery] Guid? customerId,
        CancellationToken ct)
        => (await _mediator.Send(new ListServiceTicketsQuery(status, type, priority, customerId), ct)).ToOk();

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateServiceTicketCommand cmd, CancellationToken ct)
        => (await _mediator.Send(cmd, ct)).ToCreated();

    [HttpPost("{id:guid}/assign")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Assign(Guid id, [FromBody] AssignServiceTicketCommand cmd, CancellationToken ct)
        => id != cmd.Id ? RouteIdMismatch() : (await _mediator.Send(cmd, ct)).ToOk();

    [HttpPost("{id:guid}/resolve")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Resolve(Guid id, [FromBody] ResolveServiceTicketCommand cmd, CancellationToken ct)
        => id != cmd.Id ? RouteIdMismatch() : (await _mediator.Send(cmd, ct)).ToOk();
}

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/customers/me/service-tickets")]
public class MyServiceTicketsController : ControllerBase
{
    private readonly IMediator _mediator;
    public MyServiceTicketsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> ListMine([FromQuery] Guid customerId, CancellationToken ct)
        => (await _mediator.Send(new ListMyServiceTicketsQuery(customerId), ct)).ToOk();
}
