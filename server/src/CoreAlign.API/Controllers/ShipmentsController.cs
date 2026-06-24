using System.Security.Claims;
using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Common;
using CoreAlign.Application.Shipments.Commands;
using CoreAlign.Application.Shipments.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class ShipmentsController : ControllerBase
{
    private readonly IMediator _mediator;
    public ShipmentsController(IMediator mediator) => _mediator = mediator;

    private Guid CurrentUserId =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : Guid.Empty;

    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? search = null,
        [FromQuery] Guid? customerId = null,
        [FromQuery] Guid? orderId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
        => (await _mediator.Send(new SearchShipmentsQuery(search, customerId, orderId, page, pageSize), ct)).ToOk();

    [HttpGet("by-order/{orderId:guid}")]
    public async Task<IActionResult> GetByOrder(Guid orderId, CancellationToken ct)
        => (await _mediator.Send(new GetShipmentsByOrderQuery(orderId), ct)).ToOk();

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => (await _mediator.Send(new GetShipmentByIdQuery(id), ct)).ToOk();

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateShipmentCommand cmd, CancellationToken ct)
        => (await _mediator.Send(cmd, ct)).ToCreated();

    [HttpPost("{id:guid}/pick")]
    public async Task<IActionResult> Pick(Guid id, [FromBody] PickShipmentCommand? cmd, CancellationToken ct)
        => (await _mediator.Send(new PickShipmentCommand(id, CurrentUserId), ct)).ToOk();

    [HttpPost("{id:guid}/pack")]
    public async Task<IActionResult> Pack(Guid id, CancellationToken ct)
        => (await _mediator.Send(new PackShipmentCommand(id), ct)).ToOk();

    [HttpPost("{id:guid}/dispatch")]
    public async Task<IActionResult> Dispatch(Guid id, [FromBody] DispatchShipmentCommand cmd, CancellationToken ct)
    {
        var enriched = new DispatchShipmentCommand(id, cmd.CarrierName, cmd.TrackingNumber, cmd.TrackingUrl, cmd.ShippingCost);
        return (await _mediator.Send(enriched, ct)).ToOk();
    }

    [HttpPost("{id:guid}/deliver")]
    public async Task<IActionResult> Deliver(Guid id, [FromBody] DeliverShipmentCommand cmd, CancellationToken ct)
    {
        var enriched = new DeliverShipmentCommand(id, cmd.ReceivedBy, cmd.DeliveredAtUtc);
        return (await _mediator.Send(enriched, ct)).ToOk();
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelShipmentCommand? cmd, CancellationToken ct)
        => (await _mediator.Send(new CancelShipmentCommand(id, cmd?.Reason), ct)).ToOk();
}
