using Asp.Versioning;
using CoreAlign.Application.Authorization;
using CoreAlign.Application.Common;
using CoreAlign.Application.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers.CustomerPortal;

// Invoice list + detail are owned by CustomerPortalController
// (GET customer-portal/invoices, customer-portal/invoices/{id}); declaring them here too
// produced an AmbiguousMatchException (INVARIANTS §52). This controller keeps only the
// PDF-download route the admin SPA calls.
[ApiController]
[Authorize(Policy = CustomerPortalPolicies.SelfService)]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/customer-portal/invoices")]
public class MyInvoicesController : ControllerBase
{
    private readonly ICurrentCustomerAccessor _currentCustomer;
    private readonly IDocumentService _documents;

    public MyInvoicesController(
        ICurrentCustomerAccessor currentCustomer,
        IDocumentService documents)
    {
        _currentCustomer = currentCustomer;
        _documents = documents;
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
