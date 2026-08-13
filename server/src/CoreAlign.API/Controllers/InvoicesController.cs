using Asp.Versioning;
using CoreAlign.API.Authorization;
using CoreAlign.API.Common;
using CoreAlign.Application.Invoices.Commands;
using CoreAlign.Application.Invoices.Queries;
using CoreAlign.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize(Policy = PersonaPolicies.Tenant)]
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
        [FromQuery] string? statusBucket = null,
        [FromQuery] bool dueSoonOnly = false,
        [FromQuery] int? fiscalYear = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetInvoicesQuery(page, pageSize, search, customerId, statusBucket, dueSoonOnly, fiscalYear),
            cancellationToken);
        return result.ToOk();
    }

    [HttpGet("aggregates")]
    public async Task<IActionResult> GetInvoiceAggregatesAsync(
        [FromQuery] string? search = null,
        [FromQuery] Guid? customerId = null,
        [FromQuery] int? fiscalYear = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetInvoiceAggregatesQuery(search, customerId, fiscalYear), cancellationToken);
        return result.ToOk();
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetInvoiceByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetInvoiceByIdQuery(id), cancellationToken);
        return result.ToOk();
    }

    [HttpPost("from-order/{orderId:guid}")]
    [Authorize(Roles = PersonaPolicies.TenantAdminRole)]
    public async Task<IActionResult> GenerateFromOrderAsync(
        Guid orderId,
        [FromBody] GenerateInvoiceRequest? request,
        CancellationToken cancellationToken)
    {
        var command = new GenerateInvoiceFromOrderCommand(orderId, request?.DueDays ?? 30, request?.Notes);
        var result = await _mediator.Send(command, cancellationToken);
        return result.ToOk();
    }

    [HttpPost("standalone")]
    [Authorize(Roles = PersonaPolicies.TenantAdminRole)]
    public async Task<IActionResult> CreateStandaloneAsync(
        [FromBody] CreateStandaloneInvoiceCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return result.ToCreated();
    }

    [HttpPost("{id:guid}/credit-notes")]
    [Authorize(Roles = PersonaPolicies.TenantAdminRole)]
    public async Task<IActionResult> IssueCreditNoteAsync(
        Guid id,
        [FromBody] IssueCreditNoteRequest request,
        CancellationToken cancellationToken)
    {
        var command = new IssueCreditNoteCommand(
            id,
            request.Lines,
            request.Reason,
            request.ReturnRequestId,
            request.OperationId);
        var result = await _mediator.Send(command, cancellationToken);
        return result.ToCreated();
    }

    [HttpPost("{id:guid}/mark-paid")]
    [Authorize(Roles = PersonaPolicies.TenantAdminRole)]
    public async Task<IActionResult> MarkPaidAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new MarkInvoiceAsPaidCommand(id), cancellationToken);
        return result.ToOk();
    }

    [HttpPost("{id:guid}/payments")]
    [Authorize(Roles = PersonaPolicies.TenantAdminRole)]
    public async Task<IActionResult> RecordPaymentAsync(
        Guid id,
        [FromBody] RecordInvoicePaymentRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RecordInvoicePaymentCommand(
            id,
            request.Amount,
            request.Method,
            request.PaymentDate,
            request.ReferenceNumber,
            request.Notes);
        var result = await _mediator.Send(command, cancellationToken);
        return result.ToOk();
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = PersonaPolicies.TenantAdminRole)]
    public async Task<IActionResult> CancelAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CancelInvoiceCommand(id), cancellationToken);
        return result.ToOk();
    }

    [HttpPost("{id:guid}/write-off")]
    [Authorize(Roles = PersonaPolicies.TenantAdminRole)]
    public async Task<IActionResult> WriteOffAsync(
        Guid id,
        [FromBody] WriteOffInvoiceRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new WriteOffInvoiceCommand(id, request?.Reason), cancellationToken);
        return result.ToOk();
    }

    [HttpGet("{id:guid}/credit-notes")]
    public async Task<IActionResult> GetCreditNotesAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCreditNotesForInvoiceQuery(id), cancellationToken);
        return result.ToOk();
    }

    [HttpGet("{id:guid}/credited-by-line")]
    public async Task<IActionResult> GetCreditedByLineAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCreditedQuantitiesByLineQuery(id), cancellationToken);
        return result.ToOk();
    }
}

public record GenerateInvoiceRequest(int DueDays = 30, string? Notes = null);

public record IssueCreditNoteRequest(
    IReadOnlyList<IssueCreditNoteLineInput> Lines,
    string? Reason = null,
    Guid? ReturnRequestId = null,
    Guid? OperationId = null);

public record WriteOffInvoiceRequest(string? Reason = null);

public record RecordInvoicePaymentRequest(
    decimal Amount,
    PaymentMethod Method = PaymentMethod.BankTransfer,
    DateTime? PaymentDate = null,
    string? ReferenceNumber = null,
    string? Notes = null);
