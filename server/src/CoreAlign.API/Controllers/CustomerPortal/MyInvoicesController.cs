using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Authorization;
using CoreAlign.Application.Common;
using CoreAlign.Application.Documents;
using CoreAlign.Application.Invoices.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers.CustomerPortal;

[ApiController]
[Authorize(Policy = CustomerPortalPolicies.SelfService)]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/customer-portal/invoices")]
public class MyInvoicesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentCustomerAccessor _currentCustomer;
    private readonly IDocumentService _documents;

    public MyInvoicesController(
        IMediator mediator,
        ICurrentCustomerAccessor currentCustomer,
        IDocumentService documents)
    {
        _mediator = mediator;
        _currentCustomer = currentCustomer;
        _documents = documents;
    }

    [HttpGet]
    public async Task<IActionResult> ListMy(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var customerId = await _currentCustomer.GetCustomerIdOrThrowAsync(ct);
        var query = new GetInvoicesQuery(page, pageSize, search, customerId);
        return (await _mediator.Send(query, ct)).ToOk();
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetMy(Guid id, CancellationToken ct)
    {
        var customerId = await _currentCustomer.GetCustomerIdOrThrowAsync(ct);
        var invoice = await _mediator.Send(new GetInvoiceByIdQuery(id), ct);
        if (invoice is null || invoice.CustomerId != customerId)
        {
            return NotFound(ApiResponse<object>.Failure("Invoice not found.", 404));
        }
        return invoice.ToOk();
    }

    [HttpGet("{id:guid}/download-pdf")]
    public async Task<IActionResult> DownloadPdf(Guid id, CancellationToken ct)
    {
        var customerId = await _currentCustomer.GetCustomerIdOrThrowAsync(ct);
        var doc = await _documents.RenderInvoicePdfForCustomerAsync(id, customerId, ct);
        Response.Headers.ContentDisposition = $"attachment; filename=\"{doc.FileName}\"";
        return File(doc.Content, doc.ContentType, doc.FileName);
    }
}
