using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Invoices.Commands;
using CoreAlign.Application.Invoices.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class InvoicesController : ControllerBase
{
    private readonly IMediator _mediator;

    public InvoicesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetInvoicesAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] Guid? customerId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetInvoicesQuery(page, pageSize, search, customerId), cancellationToken);
        return result.ToOk();
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetInvoiceByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetInvoiceByIdQuery(id), cancellationToken);
        return result.ToOk();
    }

    [HttpPost("from-order/{orderId:guid}")]
    public async Task<IActionResult> GenerateFromOrderAsync(
        Guid orderId,
        [FromBody] GenerateInvoiceRequest? request,
        CancellationToken cancellationToken)
    {
        var command = new GenerateInvoiceFromOrderCommand(orderId, request?.DueDays ?? 30, request?.Notes);
        var result = await _mediator.Send(command, cancellationToken);
        return result.ToOk();
    }

    [HttpPost("{id:guid}/mark-paid")]
    public async Task<IActionResult> MarkPaidAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new MarkInvoiceAsPaidCommand(id), cancellationToken);
        return result.ToOk();
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> CancelAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CancelInvoiceCommand(id), cancellationToken);
        return result.ToOk();
    }
}

public record GenerateInvoiceRequest(int DueDays = 30, string? Notes = null);
