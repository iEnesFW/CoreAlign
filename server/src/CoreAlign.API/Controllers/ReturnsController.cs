using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Returns.Commands;
using CoreAlign.Application.Returns.Queries;
using CoreAlign.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize(Policy = Authorization.PersonaPolicies.Tenant)]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class ReturnsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReturnsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> ListAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] Guid? customerId = null,
        [FromQuery] Guid? orderId = null,
        [FromQuery] ReturnRequestStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetReturnRequestsQuery(page, pageSize, search, customerId, orderId, status),
            cancellationToken);
        return result.ToOk();
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetReturnRequestByIdQuery(id), cancellationToken);
        return result.ToOk();
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync(
        [FromBody] CreateReturnRequestCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return result.ToCreated();
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> ApproveAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ApproveReturnRequestCommand(id), cancellationToken);
        return result.ToOk();
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> RejectAsync(
        Guid id,
        [FromBody] RejectReturnRequestRequest? body,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RejectReturnRequestCommand(id, body?.Reason), cancellationToken);
        return result.ToOk();
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> CancelAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CancelReturnRequestCommand(id), cancellationToken);
        return result.ToOk();
    }

    [HttpPost("{id:guid}/receive")]
    public async Task<IActionResult> ReceiveAsync(
        Guid id,
        [FromBody] ReceiveReturnedItemsRequest body,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ReceiveReturnedItemsCommand(id, body.WarehouseId, body.AutoIssueCreditNote),
            cancellationToken);
        return result.ToOk();
    }

    [HttpGet("by-order/{orderId:guid}")]
    public async Task<IActionResult> ListByOrderAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetReturnRequestsByOrderQuery(orderId), cancellationToken);
        return result.ToOk();
    }
}

public record RejectReturnRequestRequest(string? Reason);

public record ReceiveReturnedItemsRequest(Guid WarehouseId, bool AutoIssueCreditNote = true);
