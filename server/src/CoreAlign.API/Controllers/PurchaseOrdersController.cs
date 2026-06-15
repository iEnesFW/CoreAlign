using System.Security.Claims;
using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Common;
using CoreAlign.Application.Purchasing;
using CoreAlign.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/purchase-orders")]
public class PurchaseOrdersController : ControllerBase
{
    private readonly IMediator _mediator;
    public PurchaseOrdersController(IMediator mediator) => _mediator = mediator;

    private static IActionResult RouteIdMismatch() =>
        new BadRequestObjectResult(ApiResponse<object>.Failure("Route id does not match command id.", 400));

    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] Guid? vendorId,
        [FromQuery] PurchaseOrderStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
        => (await _mediator.Send(new SearchPurchaseOrdersQuery(vendorId, status, page, pageSize), ct)).ToOk();

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => (await _mediator.Send(new GetPurchaseOrderByIdQuery(id), ct)).ToOk();

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePurchaseOrderCommand cmd, CancellationToken ct)
        => (await _mediator.Send(cmd, ct)).ToCreated();

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePurchaseOrderCommand cmd, CancellationToken ct)
        => id != cmd.Id ? RouteIdMismatch() : (await _mediator.Send(cmd, ct)).ToOk();

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        => (await _mediator.Send(new DeletePurchaseOrderCommand(id), ct)).ToOk();

    [HttpPost("{id:guid}/submit")]
    public async Task<IActionResult> Submit(Guid id, CancellationToken ct)
        => (await _mediator.Send(new SubmitPurchaseOrderCommand(id), ct)).ToOk();

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
    {
        var userId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : Guid.Empty;
        return (await _mediator.Send(new ApprovePurchaseOrderCommand(id, userId), ct)).ToOk();
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelPurchaseOrderCommand? cmd, CancellationToken ct)
        => (await _mediator.Send(new CancelPurchaseOrderCommand(id, cmd?.Reason), ct)).ToOk();

    [HttpPost("{id:guid}/close")]
    public async Task<IActionResult> Close(Guid id, CancellationToken ct)
        => (await _mediator.Send(new ClosePurchaseOrderCommand(id), ct)).ToOk();

    [HttpPost("{id:guid}/receive")]
    public async Task<IActionResult> Receive(Guid id, [FromBody] ReceivePurchaseOrderCommand cmd, CancellationToken ct)
        => id != cmd.Id ? RouteIdMismatch() : (await _mediator.Send(cmd, ct)).ToOk();
}
