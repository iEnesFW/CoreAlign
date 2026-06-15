using Asp.Versioning;
using CoreAlign.Application.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documents;

    public DocumentsController(IDocumentService documents)
    {
        _documents = documents;
    }

    [HttpGet("invoices/{id:guid}/pdf")]
    public async Task<IActionResult> DownloadInvoicePdf(Guid id, CancellationToken cancellationToken)
    {
        var doc = await _documents.RenderInvoicePdfAsync(id, cancellationToken);
        return PdfFile(doc);
    }

    [HttpGet("invoices/{id:guid}/credit-note/pdf")]
    public async Task<IActionResult> DownloadCreditNotePdf(Guid id, CancellationToken cancellationToken)
    {
        var doc = await _documents.RenderCreditNotePdfAsync(id, cancellationToken);
        return PdfFile(doc);
    }

    [HttpGet("orders/{id:guid}/pdf")]
    public async Task<IActionResult> DownloadOrderPdf(Guid id, CancellationToken cancellationToken)
    {
        var doc = await _documents.RenderOrderPdfAsync(id, cancellationToken);
        return PdfFile(doc);
    }

    [HttpGet("shipments/{id:guid}/packing-slip/pdf")]
    public async Task<IActionResult> DownloadPackingSlipPdf(Guid id, CancellationToken cancellationToken)
    {
        var doc = await _documents.RenderShipmentPdfAsync(id, cancellationToken);
        return PdfFile(doc);
    }

    private FileContentResult PdfFile(DocumentResult doc)
    {
        Response.Headers.ContentDisposition = $"attachment; filename=\"{doc.FileName}\"";
        return File(doc.Content, doc.ContentType, doc.FileName);
    }
}
