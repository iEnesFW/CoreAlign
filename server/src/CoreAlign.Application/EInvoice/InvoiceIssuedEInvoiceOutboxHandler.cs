using CoreAlign.Application.Common.Outbox;
using CoreAlign.Domain.Common;
using CoreAlign.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Application.EInvoice;

public sealed class InvoiceIssuedEInvoiceOutboxHandler : IOutboxMessageHandler
{
    public string MessageType => EInvoiceSubmissionOutbox.SubmissionMessageType;

    private readonly IInvoiceRepository _invoices;
    private readonly ICustomerRepository _customers;
    private readonly IElectronicInvoiceGateway _gateway;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<InvoiceIssuedEInvoiceOutboxHandler> _logger;

    public InvoiceIssuedEInvoiceOutboxHandler(
        IInvoiceRepository invoices,
        ICustomerRepository customers,
        IElectronicInvoiceGateway gateway,
        IUnitOfWork unitOfWork,
        ILogger<InvoiceIssuedEInvoiceOutboxHandler> logger)
    {
        _invoices = invoices;
        _customers = customers;
        _gateway = gateway;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<OutboxHandlerResult> HandleAsync(string payloadJson, CancellationToken cancellationToken)
    {
        var payload = EInvoiceSubmissionOutbox.Deserialize<EInvoiceSubmissionRequestedPayload>(payloadJson);
        if (payload is null) return OutboxHandlerResult.Failed("Payload deserialized to null.");

        var invoice = await _invoices.GetWithLinesAsync(payload.InvoiceId, cancellationToken);
        if (invoice is null) return OutboxHandlerResult.Failed($"Invoice {payload.InvoiceId} not found.");

        if (!string.IsNullOrEmpty(invoice.EInvoiceUuid))
        {
            return OutboxHandlerResult.Processed("AlreadySubmitted");
        }

        var customer = await _customers.GetByIdAsync(invoice.CustomerId, cancellationToken);
        var seller = BuildSellerParty(invoice);
        var buyer = BuildBuyerParty(invoice, customer);

        var buyerTaxNumber = invoice.CustomerSnapshot?.TaxNumber ?? customer?.TaxNumber;
        var documentKind = await ResolveDocumentKindAsync(buyerTaxNumber, cancellationToken);
        invoice.SetEInvoiceProfile(documentKind == EInvoiceDocumentKind.EArchive ? "EARSIV" : "TICARIFATURA");

        var xml = UblTrInvoiceXmlBuilder.Build(invoice, seller, buyer);

        var request = new EInvoiceSubmissionRequest(
            invoice.TenantId,
            invoice.Id,
            xml,
            buyerTaxNumber,
            invoice.CustomerNameSnapshot,
            documentKind);

        var result = await _gateway.SubmitAsync(request, cancellationToken);

        if (string.Equals(result.Status, "Failed", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(result.RemoteUuid))
        {
            invoice.RegisterEInvoice(null, "Failed", null);
            _invoices.Update(invoice);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogWarning(
                "e-Invoice submission failed for invoice {InvoiceId} via gateway {Gateway}: {Reason}",
                invoice.Id, _gateway.GatewayName, result.FailureReason ?? "unknown");
            return OutboxHandlerResult.Failed(result.FailureReason ?? "Gateway returned no remote uuid.");
        }

        invoice.RegisterEInvoice(result.RemoteUuid, result.Status, result.PdfUrl);
        _invoices.Update(invoice);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "e-Invoice submitted for invoice {InvoiceId} via {Gateway}: uuid={Uuid}, status={Status}",
            invoice.Id, _gateway.GatewayName, result.RemoteUuid, result.Status);

        return OutboxHandlerResult.Processed($"Submitted:{result.RemoteUuid}");
    }

    private async Task<EInvoiceDocumentKind> ResolveDocumentKindAsync(string? buyerTaxNumber, CancellationToken cancellationToken)
    {
        // WHY: VKN'si e-Fatura mükellefi olan alıcıya e-Fatura, aksi halde (bireysel/kayıtsız) e-Arşiv kesilir.
        if (string.IsNullOrWhiteSpace(buyerTaxNumber))
        {
            return EInvoiceDocumentKind.EArchive;
        }

        var taxpayer = await _gateway.CheckTaxpayerAsync(buyerTaxNumber, cancellationToken);
        return taxpayer.IsEFaturaRegistered
            ? EInvoiceDocumentKind.EFatura
            : EInvoiceDocumentKind.EArchive;
    }

    private static SellerParty BuildSellerParty(Domain.Entities.Invoice invoice) =>
        new(
            Name: "Tenant Seller",
            TaxNumber: null,
            NationalId: null,
            TaxOffice: null,
            AddressLine: null,
            City: null,
            PostalCode: null,
            Country: "Türkiye");

    private static BuyerParty BuildBuyerParty(Domain.Entities.Invoice invoice, Domain.Entities.Customer? customer)
    {
        var snapshot = invoice.CustomerSnapshot;
        AddressSnapshot? billing = invoice.BillingAddressSnapshot;
        return new BuyerParty(
            Name: snapshot?.LegalName ?? invoice.CustomerNameSnapshot,
            TaxNumber: snapshot?.TaxNumber ?? customer?.TaxNumber,
            NationalId: snapshot?.NationalId ?? customer?.NationalId,
            TaxOffice: snapshot?.TaxOffice ?? customer?.TaxOffice,
            AddressLine: billing?.Line1,
            City: billing?.City,
            PostalCode: billing?.PostalCode,
            Country: billing?.Country ?? "Türkiye");
    }
}
