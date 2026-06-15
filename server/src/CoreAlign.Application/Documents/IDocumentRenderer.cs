namespace CoreAlign.Application.Documents;

public interface IDocumentRenderer
{
    Task<byte[]> RenderInvoiceAsync(InvoiceDocumentModel model, CancellationToken cancellationToken = default);
    Task<byte[]> RenderCreditNoteAsync(InvoiceDocumentModel model, CancellationToken cancellationToken = default);
    Task<byte[]> RenderOrderConfirmationAsync(OrderDocumentModel model, CancellationToken cancellationToken = default);
    Task<byte[]> RenderPackingSlipAsync(ShipmentDocumentModel model, CancellationToken cancellationToken = default);
    Task<byte[]> RenderQuoteAsync(QuoteDocumentModel model, CancellationToken cancellationToken = default);
    Task<byte[]> RenderDealerCommissionStatementAsync(DealerCommissionStatementModel model, CancellationToken cancellationToken = default);
}
