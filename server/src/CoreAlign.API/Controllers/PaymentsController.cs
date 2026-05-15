using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Common;
using CoreAlign.Application.Payments.Commands;
using CoreAlign.Application.Payments.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;
    public PaymentsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? search = null,
        [FromQuery] Guid? customerId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
        => (await _mediator.Send(new SearchPaymentsQuery(search, customerId, page, pageSize), ct)).ToOk();

    [HttpGet("by-customer/{customerId:guid}")]
    public async Task<IActionResult> GetByCustomer(Guid customerId, CancellationToken ct)
        => (await _mediator.Send(new GetPaymentsByCustomerQuery(customerId), ct)).ToOk();

    [HttpGet("by-invoice/{invoiceId:guid}")]
    public async Task<IActionResult> GetByInvoice(Guid invoiceId, CancellationToken ct)
        => (await _mediator.Send(new GetPaymentsByInvoiceQuery(invoiceId), ct)).ToOk();

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => (await _mediator.Send(new GetPaymentByIdQuery(id), ct)).ToOk();

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePaymentCommand cmd, CancellationToken ct)
        => (await _mediator.Send(cmd, ct)).ToCreated();

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePaymentCommand cmd, CancellationToken ct)
    {
        if (id != cmd.Id) return BadRequest(ApiResponse<object>.Failure("Route id does not match command id.", 400));
        return (await _mediator.Send(cmd, ct)).ToOk();
    }

    [HttpPost("{id:guid}/confirm")]
    public async Task<IActionResult> Confirm(Guid id, [FromBody] ConfirmPaymentCommand? cmd, CancellationToken ct)
        => (await _mediator.Send(new ConfirmPaymentCommand(id, cmd?.PostedByUserId), ct)).ToOk();

    [HttpPost("{id:guid}/apply")]
    public async Task<IActionResult> Apply(Guid id, [FromBody] ApplyPaymentCommand cmd, CancellationToken ct)
    {
        if (id != cmd.Id) return BadRequest(ApiResponse<object>.Failure("Route id does not match command id.", 400));
        return (await _mediator.Send(cmd, ct)).ToOk();
    }

    [HttpPost("{id:guid}/unapply/{applicationId:guid}")]
    public async Task<IActionResult> Unapply(Guid id, Guid applicationId, CancellationToken ct)
        => (await _mediator.Send(new UnapplyPaymentCommand(id, applicationId), ct)).ToOk();

    [HttpPost("{id:guid}/void")]
    public async Task<IActionResult> Void(Guid id, [FromBody] VoidPaymentCommand? cmd, CancellationToken ct)
        => (await _mediator.Send(new VoidPaymentCommand(id, cmd?.Reason), ct)).ToOk();
}

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/customers/{customerId:guid}")]
public class CustomerLedgerController : ControllerBase
{
    private readonly IMediator _mediator;
    public CustomerLedgerController(IMediator mediator) => _mediator = mediator;

    [HttpGet("ledger")]
    public async Task<IActionResult> GetLedger(
        Guid customerId,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
        => (await _mediator.Send(new GetCustomerLedgerQuery(customerId, fromUtc, toUtc, page, pageSize), ct)).ToOk();

    [HttpGet("aging")]
    public async Task<IActionResult> GetAging(Guid customerId, [FromQuery] DateTime? asOfUtc, CancellationToken ct)
        => (await _mediator.Send(new GetCustomerAgingQuery(customerId, asOfUtc), ct)).ToOk();

    [HttpGet("open-invoices")]
    public async Task<IActionResult> GetOpenInvoices(Guid customerId, CancellationToken ct)
        => (await _mediator.Send(new GetOpenInvoicesForCustomerQuery(customerId), ct)).ToOk();
}
