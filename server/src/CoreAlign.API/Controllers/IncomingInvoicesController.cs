using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.IncomingInvoices;
using CoreAlign.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/incoming-invoices")]
public class IncomingInvoicesController : ControllerBase
{
    private readonly IMediator _mediator;
    public IncomingInvoicesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] IncomingInvoiceStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => (await _mediator.Send(new ListIncomingInvoicesQuery(status, page, pageSize), ct)).ToOk();

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        => (await _mediator.Send(new GetIncomingInvoiceQuery(id), ct)).ToOk();

    [HttpPost("{id:guid}/process")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Process(Guid id, [FromBody] ProcessIncomingInvoiceRequest request, CancellationToken ct)
        => (await _mediator.Send(
            new ProcessIncomingInvoiceCommand(id, request.Subtotal, request.TaxAmount, request.VendorName, request.Currency), ct)).ToOk();

    [HttpPost("{id:guid}/ignore")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Ignore(Guid id, [FromBody] IgnoreIncomingInvoiceRequest request, CancellationToken ct)
        => (await _mediator.Send(new IgnoreIncomingInvoiceCommand(id, request.Reason), ct)).ToOk();

    public sealed record ProcessIncomingInvoiceRequest(decimal Subtotal, decimal TaxAmount, string? VendorName, string? Currency);

    public sealed record IgnoreIncomingInvoiceRequest(string? Reason);
}
