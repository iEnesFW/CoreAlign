namespace CoreAlign.Application.Documents;

public sealed record DocumentResult(byte[] Content, string FileName, string ContentType = "application/pdf");

public interface IDocumentService
{
    Task<DocumentResult> RenderInvoicePdfAsync(Guid invoiceId, CancellationToken cancellationToken = default);
    Task<DocumentResult> RenderCreditNotePdfAsync(Guid invoiceId, CancellationToken cancellationToken = default);
    Task<DocumentResult> RenderOrderPdfAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<DocumentResult> RenderShipmentPdfAsync(Guid shipmentId, CancellationToken cancellationToken = default);

    Task<DocumentResult> RenderInvoicePdfForCustomerAsync(Guid invoiceId, Guid customerId, CancellationToken cancellationToken = default);
    Task<DocumentResult> RenderOrderPdfForCustomerAsync(Guid orderId, Guid customerId, CancellationToken cancellationToken = default);
    Task<DocumentResult> RenderOrderPdfForDealerAsync(Guid orderId, Guid dealerAccountId, CancellationToken cancellationToken = default);
    Task<DocumentResult> RenderQuotePdfAsync(Guid quoteId, CancellationToken cancellationToken = default);
    Task<DocumentResult> RenderInvoicePdfForDealerAsync(Guid invoiceId, Guid dealerAccountId, IReadOnlyCollection<Guid> allowedCustomerIds, CancellationToken cancellationToken = default);
    Task<DocumentResult> RenderDealerCommissionStatementPdfAsync(Guid dealerAccountId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);
}
