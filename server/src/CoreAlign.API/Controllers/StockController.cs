using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Common;
using CoreAlign.Application.Inventory.Commands;
using CoreAlign.Application.Inventory.Queries;
using CoreAlign.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/stock")]
public class StockController : ControllerBase
{
    private readonly IMediator _mediator;
    public StockController(IMediator mediator) => _mediator = mediator;

    private static IActionResult RouteIdMismatch() =>
        new BadRequestObjectResult(ApiResponse<object>.Failure("Route id does not match command id.", 400));

    [HttpGet("items")]
    public async Task<IActionResult> GetStockItems(
        [FromQuery] Guid? productId = null,
        [FromQuery] Guid? warehouseId = null,
        [FromQuery] bool onlyBelowReorder = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
        => (await _mediator.Send(new GetStockItemsQuery(productId, warehouseId, onlyBelowReorder, page, pageSize), ct)).ToOk();

    [HttpGet("items/by-product/{productId:guid}")]
    public async Task<IActionResult> GetStockByProduct(Guid productId, CancellationToken ct)
        => (await _mediator.Send(new GetStockByProductQuery(productId), ct)).ToOk();

    [HttpGet("summary/{productId:guid}")]
    public async Task<IActionResult> GetStockSummary(Guid productId, CancellationToken ct)
        => (await _mediator.Send(new GetStockSummaryQuery(productId), ct)).ToOk();

    [HttpGet("movements")]
    public async Task<IActionResult> GetMovements(
        [FromQuery] Guid? productId = null,
        [FromQuery] Guid? warehouseId = null,
        [FromQuery] StockMovementType? type = null,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
        => (await _mediator.Send(new GetStockMovementsQuery(productId, warehouseId, type, fromUtc, toUtc, page, pageSize), ct)).ToOk();

    [HttpGet("allocations/by-order/{orderId:guid}")]
    public async Task<IActionResult> GetAllocationsByOrder(Guid orderId, CancellationToken ct)
        => (await _mediator.Send(new GetStockAllocationsByOrderQuery(orderId), ct)).ToOk();

    [HttpGet("lots/by-product/{productId:guid}")]
    public async Task<IActionResult> GetLotsByProduct(Guid productId, CancellationToken ct)
        => (await _mediator.Send(new GetLotsByProductQuery(productId), ct)).ToOk();

    [HttpGet("reason-codes")]
    public async Task<IActionResult> ListReasonCodes(
        [FromQuery] StockReasonCategory? category = null,
        [FromQuery] bool? isActive = null,
        CancellationToken ct = default)
        => (await _mediator.Send(new ListStockReasonCodesQuery(category, isActive), ct)).ToOk();

    [HttpPost("adjust")]
    public async Task<IActionResult> Adjust([FromBody] AdjustStockCommand cmd, CancellationToken ct)
        => (await _mediator.Send(cmd, ct)).ToCreated();

    [HttpPost("receive")]
    public async Task<IActionResult> Receive([FromBody] ReceiveStockCommand cmd, CancellationToken ct)
        => (await _mediator.Send(cmd, ct)).ToCreated();

    [HttpPost("issue")]
    public async Task<IActionResult> Issue([FromBody] IssueStockCommand cmd, CancellationToken ct)
        => (await _mediator.Send(cmd, ct)).ToCreated();

    [HttpPost("lots")]
    public async Task<IActionResult> CreateLot([FromBody] CreateLotCommand cmd, CancellationToken ct)
        => (await _mediator.Send(cmd, ct)).ToCreated();

    [HttpPut("lots/{id:guid}")]
    public async Task<IActionResult> UpdateLot(Guid id, [FromBody] UpdateLotCommand cmd, CancellationToken ct)
        => id != cmd.Id ? RouteIdMismatch() : (await _mediator.Send(cmd, ct)).ToOk();

    [HttpPost("reason-codes")]
    public async Task<IActionResult> CreateReason([FromBody] CreateStockReasonCodeCommand cmd, CancellationToken ct)
        => (await _mediator.Send(cmd, ct)).ToCreated();

    [HttpPut("reason-codes/{id:guid}")]
    public async Task<IActionResult> UpdateReason(Guid id, [FromBody] UpdateStockReasonCodeCommand cmd, CancellationToken ct)
        => id != cmd.Id ? RouteIdMismatch() : (await _mediator.Send(cmd, ct)).ToOk();
}
