using System.Security.Claims;
using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.GlassPlates.Commands;
using CoreAlign.Application.GlassPlates.Queries;
using CoreAlign.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/glass-plates")]
public class GlassPlatesController : ControllerBase
{
    private readonly IMediator _mediator;
    public GlassPlatesController(IMediator mediator) => _mediator = mediator;

    private Guid CurrentUserId =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : Guid.Empty;

    [HttpPost("receive")]
    public async Task<IActionResult> Receive([FromBody] ReceiveGlassPlatesCommand command, CancellationToken ct)
        => (await _mediator.Send(command with { PostedByUserId = CurrentUserId }, ct)).ToOk();

    [HttpPost("consume")]
    public async Task<IActionResult> Consume([FromBody] ConsumeGlassPlateCommand command, CancellationToken ct)
        => (await _mediator.Send(command with { PostedByUserId = CurrentUserId }, ct)).ToOk();

    [HttpPost("scrap")]
    public async Task<IActionResult> Scrap([FromBody] ScrapGlassPlateCommand command, CancellationToken ct)
        => (await _mediator.Send(command with { PostedByUserId = CurrentUserId }, ct)).ToOk();

    [HttpPost("{id:guid}/move")]
    public async Task<IActionResult> Move(Guid id, [FromBody] MoveGlassPlateCommand command, CancellationToken ct)
        => (await _mediator.Send(command with { PlateId = id }, ct)).ToOk();

    [HttpPost("definitions")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> SetTracking([FromBody] SetGlassPlateTrackingCommand command, CancellationToken ct)
        => (await _mediator.Send(command, ct)).ToOk();

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid? productId,
        [FromQuery] Guid? warehouseId,
        [FromQuery] Guid? storageLocationId,
        [FromQuery] GlassPlateStatus? status,
        [FromQuery] PlateKind? kind,
        [FromQuery] int take = 200,
        CancellationToken ct = default)
        => (await _mediator.Send(
            new ListGlassPlatesQuery(productId, warehouseId, storageLocationId, status, kind, take), ct)).ToOk();

    [HttpGet("usable")]
    public async Task<IActionResult> Usable(
        [FromQuery] Guid productId,
        [FromQuery] decimal widthMm,
        [FromQuery] decimal heightMm,
        [FromQuery] Guid? warehouseId,
        [FromQuery] int take = 20,
        CancellationToken ct = default)
        => (await _mediator.Send(
            new UsablePlatesForCutQuery(productId, widthMm, heightMm, warehouseId, take), ct)).ToOk();

    [HttpGet("{id:guid}/where-used")]
    public async Task<IActionResult> WhereUsed(Guid id, CancellationToken ct)
        => (await _mediator.Send(new GlassPlateWhereUsedQuery(id), ct)).ToOk();

    [HttpGet("low-stock")]
    public async Task<IActionResult> LowStock(CancellationToken ct)
        => (await _mediator.Send(new LowStockPlatesQuery(), ct)).ToOk();
}
