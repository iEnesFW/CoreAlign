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
[Route("api/v{version:apiVersion}/warranty-contracts")]
public class WarrantyContractsController : ControllerBase
{
    private readonly IMediator _mediator;
    public WarrantyContractsController(IMediator mediator) => _mediator = mediator;

    private static IActionResult RouteIdMismatch() =>
        new BadRequestObjectResult(ApiResponse<object>.Failure("Route id does not match command id.", 400));

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] WarrantyContractStatus? status,
        [FromQuery] Guid? customerId,
        [FromQuery] Guid? orderId,
        CancellationToken ct)
    {
        if (orderId.HasValue)
        {
            var single = await _mediator.Send(new GetWarrantyContractByOrderIdQuery(orderId.Value), ct);
            return single is null
                ? Array.Empty<WarrantyContractDto>().ToOk()
                : new[] { single }.ToOk();
        }
        return (await _mediator.Send(new ListWarrantyContractsQuery(status, customerId), ct)).ToOk();
    }

    [HttpGet("expiring")]
    public async Task<IActionResult> Expiring([FromQuery] int withinDays = 30, CancellationToken ct = default)
        => (await _mediator.Send(new ListExpiringWarrantyAlertsQuery(withinDays), ct)).ToOk();

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => (await _mediator.Send(new GetWarrantyContractByIdQuery(id), ct)).ToOk();

    [HttpPost]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateWarrantyContractCommand cmd, CancellationToken ct)
        => (await _mediator.Send(cmd, ct)).ToCreated();

    [HttpPost("{id:guid}/activate")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Activate(Guid id, [FromBody] ActivateWarrantyContractCommand cmd, CancellationToken ct)
        => id != cmd.Id ? RouteIdMismatch() : (await _mediator.Send(cmd, ct)).ToOk();

    [HttpPost("{id:guid}/extend")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Extend(Guid id, [FromBody] ExtendWarrantyContractCommand cmd, CancellationToken ct)
        => id != cmd.Id ? RouteIdMismatch() : (await _mediator.Send(cmd, ct)).ToOk();

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelWarrantyContractCommand cmd, CancellationToken ct)
        => id != cmd.Id ? RouteIdMismatch() : (await _mediator.Send(cmd, ct)).ToOk();

    [HttpPost("{id:guid}/suspend")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Suspend(Guid id, [FromBody] SuspendWarrantyContractCommand cmd, CancellationToken ct)
        => id != cmd.Id ? RouteIdMismatch() : (await _mediator.Send(cmd, ct)).ToOk();

    [HttpPost("{id:guid}/resume")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Resume(Guid id, CancellationToken ct)
        => (await _mediator.Send(new ResumeWarrantyContractCommand(id), ct)).ToOk();
}
