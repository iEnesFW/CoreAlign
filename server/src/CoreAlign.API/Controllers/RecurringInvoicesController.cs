using Asp.Versioning;
using CoreAlign.API.Authorization;
using CoreAlign.API.Common;
using CoreAlign.Application.Common;
using CoreAlign.Application.Invoices.Recurring.Commands;
using CoreAlign.Application.Invoices.Recurring.Queries;
using CoreAlign.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize(Policy = PersonaPolicies.Tenant)]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/recurring-invoices")]
public class RecurringInvoicesController : ControllerBase
{
    private readonly IMediator _mediator;

    public RecurringInvoicesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> ListAsync(
        [FromQuery] string? search = null,
        [FromQuery] Guid? customerId = null,
        [FromQuery] RecurringInvoiceStatus? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetRecurringInvoiceTemplatesQuery(search, customerId, status, page, pageSize), cancellationToken);
        return result.ToOk();
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetAsync(Guid id, CancellationToken cancellationToken)
        => (await _mediator.Send(new GetRecurringInvoiceTemplateByIdQuery(id), cancellationToken)).ToOk();

    [HttpPost]
    [Authorize(Roles = PersonaPolicies.TenantAdminRole)]
    public async Task<IActionResult> CreateAsync(
        [FromBody] CreateRecurringInvoiceTemplateCommand command,
        CancellationToken cancellationToken)
        => (await _mediator.Send(command, cancellationToken)).ToCreated();

    [HttpPut("{id:guid}")]
    [Authorize(Roles = PersonaPolicies.TenantAdminRole)]
    public async Task<IActionResult> UpdateAsync(
        Guid id,
        [FromBody] UpdateRecurringInvoiceTemplateCommand command,
        CancellationToken cancellationToken)
    {
        if (id != command.Id)
        {
            return BadRequest(ApiResponse<object>.Failure("Route id does not match command id.", 400));
        }
        return (await _mediator.Send(command, cancellationToken)).ToOk();
    }

    [HttpPost("{id:guid}/pause")]
    [Authorize(Roles = PersonaPolicies.TenantAdminRole)]
    public async Task<IActionResult> PauseAsync(Guid id, CancellationToken cancellationToken)
        => (await _mediator.Send(new PauseRecurringInvoiceTemplateCommand(id), cancellationToken)).ToOk();

    [HttpPost("{id:guid}/resume")]
    [Authorize(Roles = PersonaPolicies.TenantAdminRole)]
    public async Task<IActionResult> ResumeAsync(Guid id, CancellationToken cancellationToken)
        => (await _mediator.Send(new ResumeRecurringInvoiceTemplateCommand(id), cancellationToken)).ToOk();

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = PersonaPolicies.TenantAdminRole)]
    public async Task<IActionResult> CancelAsync(Guid id, CancellationToken cancellationToken)
        => (await _mediator.Send(new CancelRecurringInvoiceTemplateCommand(id), cancellationToken)).ToOk();

    [HttpPost("{id:guid}/run-now")]
    [Authorize(Roles = PersonaPolicies.TenantAdminRole)]
    public async Task<IActionResult> RunNowAsync(Guid id, CancellationToken cancellationToken)
    {
        var invoiceId = await _mediator.Send(new RunRecurringInvoiceNowCommand(id), cancellationToken);
        return new { InvoiceId = invoiceId }.ToOk();
    }
}
