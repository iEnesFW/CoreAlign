using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Common;
using CoreAlign.Application.Invoices.Queries;
using CoreAlign.Application.Orders.Commands;
using CoreAlign.Application.Orders.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrdersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetOrdersAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] Guid? customerId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetOrdersQuery(page, pageSize, search, customerId), cancellationToken);
        return result.ToOk();
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOrderByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetOrderByIdQuery(id), cancellationToken);
        return result.ToOk();
    }

    [HttpGet("{id:guid}/invoices")]
    public async Task<IActionResult> GetOrderInvoicesAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetInvoicesByOrderQuery(id), cancellationToken);
        return result.ToOk();
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrderAsync([FromBody] CreateOrderCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return result.ToCreated();
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateOrderAsync(Guid id, [FromBody] UpdateOrderCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id)
        {
            return BadRequest(ApiResponse<object>.Failure("Route id does not match command id.", 400));
        }

        var result = await _mediator.Send(command, cancellationToken);
        return result.ToOk();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> DeleteOrderAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteOrderCommand(id), cancellationToken);
        return result.ToOk();
    }

    [HttpPost("{id:guid}/submit")]
    public async Task<IActionResult> SubmitOrderAsync(Guid id, CancellationToken cancellationToken)
        => (await _mediator.Send(new SubmitOrderCommand(id), cancellationToken)).ToOk();

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> ApproveOrderAsync(Guid id, [FromBody] ApproveOrderCommand? cmd, CancellationToken cancellationToken)
        => (await _mediator.Send(new ApproveOrderCommand(id, cmd?.ApprovedByUserId), cancellationToken)).ToOk();

    [HttpPost("{id:guid}/allocate")]
    public async Task<IActionResult> AllocateOrderAsync(Guid id, [FromBody] AllocateOrderCommand? cmd, CancellationToken cancellationToken)
        => (await _mediator.Send(new AllocateOrderCommand(id, cmd?.PreferredWarehouseId), cancellationToken)).ToOk();

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> CancelOrderAsync(Guid id, [FromBody] CancelOrderCommand? cmd, CancellationToken cancellationToken)
        => (await _mediator.Send(new CancelOrderCommand(id, cmd?.Reason), cancellationToken)).ToOk();

    [HttpPost("{id:guid}/deliver")]
    public async Task<IActionResult> DeliverOrderAsync(Guid id, [FromBody] DeliverOrderCommand? cmd, CancellationToken cancellationToken)
        => (await _mediator.Send(new DeliverOrderCommand(id, cmd?.DeliveredAtUtc), cancellationToken)).ToOk();

    [HttpPost("{id:guid}/close")]
    public async Task<IActionResult> CloseOrderAsync(Guid id, CancellationToken cancellationToken)
        => (await _mediator.Send(new CloseOrderCommand(id), cancellationToken)).ToOk();
}
