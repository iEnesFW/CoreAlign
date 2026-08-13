using System.Security.Claims;
using Asp.Versioning;
using CoreAlign.API.Authorization;
using CoreAlign.API.Common;
using CoreAlign.Application.Common;
using CoreAlign.Application.Payments.Commands;
using CoreAlign.Application.Payments.Queries;
using CoreAlign.Application.Providers.Payment;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize(Policy = PersonaPolicies.Tenant)]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;
    public PaymentsController(IMediator mediator) => _mediator = mediator;

    private Guid CurrentUserId =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : Guid.Empty;

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
        => (await _mediator.Send(new ConfirmPaymentCommand(id, CurrentUserId), ct)).ToOk();

    [HttpPost("{id:guid}/apply")]
    public async Task<IActionResult> Apply(Guid id, [FromBody] ApplyPaymentCommand cmd, CancellationToken ct)
    {
        if (id != cmd.Id) return BadRequest(ApiResponse<object>.Failure("Route id does not match command id.", 400));
        return (await _mediator.Send(cmd, ct)).ToOk();
    }

    [HttpPost("{id:guid}/apply-fifo")]
    public async Task<IActionResult> ApplyFifo(Guid id, CancellationToken ct)
        => (await _mediator.Send(new ApplyPaymentFifoCommand(id), ct)).ToOk();

    [HttpPost("{id:guid}/offset-advance")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> OffsetAdvance(Guid id, [FromBody] OffsetAdvanceRequest? body, CancellationToken ct)
        => (await _mediator.Send(
            new OffsetCustomerAdvanceCommand(id, body?.Applications ?? new List<PaymentApplyLine>()), ct)).ToOk();

    public sealed record OffsetAdvanceRequest(List<PaymentApplyLine> Applications);

    [HttpPost("{id:guid}/unapply/{applicationId:guid}")]
    public async Task<IActionResult> Unapply(Guid id, Guid applicationId, CancellationToken ct)
        => (await _mediator.Send(new UnapplyPaymentCommand(id, applicationId), ct)).ToOk();

    [HttpPost("{id:guid}/void")]
    public async Task<IActionResult> Void(Guid id, [FromBody] VoidPaymentCommand? cmd, CancellationToken ct)
        => (await _mediator.Send(new VoidPaymentCommand(id, cmd?.Reason), ct)).ToOk();

    [HttpPost("transactions/{transactionId}/refund")]
    [Authorize(Policy = "Payment.Refund")]
    public async Task<IActionResult> RefundTransaction(
        string transactionId,
        [FromBody] PaymentRefundApiRequest body,
        [FromServices] IPaymentDispatcher dispatcher,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(transactionId))
        {
            return BadRequest(ApiResponse<object>.Failure("transactionId is required.", 400));
        }
        if (body is null)
        {
            return BadRequest(ApiResponse<object>.Failure("Refund body is required.", 400));
        }
        if (body.Amount is decimal amt && amt <= 0m)
        {
            return BadRequest(ApiResponse<object>.Failure("Refund amount must be positive.", 400));
        }

        try
        {
            var result = await dispatcher.RefundAsync(transactionId, body.Amount, body.Reason ?? string.Empty, ct);
            if (!result.Success)
            {
                return BadRequest(ApiResponse<PaymentRefundResult>.Failure(result.FailureMessage ?? "Refund declined.", 400));
            }
            return Ok(ApiResponse<PaymentRefundResult>.Success(result));
        }
        catch (PaymentTransactionNotFoundException)
        {
            return NotFound(ApiResponse<object>.Failure($"Payment transaction '{transactionId}' not found.", 404));
        }
    }
}

public sealed record PaymentRefundApiRequest(decimal? Amount, string? Reason);

[ApiController]
[Authorize(Policy = PersonaPolicies.Tenant)]
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
