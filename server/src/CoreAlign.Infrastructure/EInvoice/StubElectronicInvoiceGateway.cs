using System.Xml.Linq;
using CoreAlign.Application.EInvoice;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Infrastructure.EInvoice;

public sealed class StubElectronicInvoiceGateway : IElectronicInvoiceGateway
{
    private readonly ILogger<StubElectronicInvoiceGateway> _logger;

    public StubElectronicInvoiceGateway(ILogger<StubElectronicInvoiceGateway> logger)
    {
        _logger = logger;
    }

    public string GatewayName => "Stub";

    public Task<EInvoiceSubmissionResult> SubmitAsync(EInvoiceSubmissionRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.UblTrXml))
        {
            return Task.FromResult(new EInvoiceSubmissionResult(null, "Failed", "Empty UBL-TR payload.", null));
        }

        try
        {
            XDocument.Parse(request.UblTrXml);
        }
        catch (System.Xml.XmlException ex)
        {
            _logger.LogWarning(
                "Stub e-invoice gateway rejected payload for invoice {InvoiceId}: {Reason}",
                request.InvoiceId, ex.Message);
            return Task.FromResult(new EInvoiceSubmissionResult(null, "Failed", $"Invalid UBL-TR XML: {ex.Message}", null));
        }

        var remoteUuid = $"STUB-{Guid.NewGuid():N}";
        _logger.LogInformation(
            "Stub e-invoice gateway accepted invoice {InvoiceId} for tenant {TenantId} (assigned uuid {RemoteUuid}).",
            request.InvoiceId, request.TenantId, remoteUuid);

        return Task.FromResult(new EInvoiceSubmissionResult(remoteUuid, "Submitted", null, null));
    }

    public Task<EInvoiceStatusResult> CheckStatusAsync(string remoteUuid, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(remoteUuid))
        {
            return Task.FromResult(new EInvoiceStatusResult("Unknown", "Empty remote uuid.", null));
        }
        return Task.FromResult(new EInvoiceStatusResult("Accepted", null, null));
    }

    public Task<EInvoiceTaxpayerResult> CheckTaxpayerAsync(string taxNumber, CancellationToken ct)
    {
        var registered = !string.IsNullOrWhiteSpace(taxNumber)
            && taxNumber.Length == 10
            && taxNumber.All(char.IsDigit);
        return Task.FromResult(new EInvoiceTaxpayerResult(taxNumber ?? string.Empty, registered, null));
    }
}
