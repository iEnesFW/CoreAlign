using System.Xml.Linq;
using CoreAlign.Application.EInvoice;
using CoreAlign.Application.Providers.EFatura;
using CoreAlign.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Infrastructure.EInvoice;

public sealed class EFaturaProviderGatewayAdapter : IElectronicInvoiceGateway
{
    public const string GatewayKey = "EFaturaDispatcher";

    private readonly IEFaturaDispatcher _dispatcher;
    private readonly ILogger<EFaturaProviderGatewayAdapter> _logger;

    public EFaturaProviderGatewayAdapter(
        IEFaturaDispatcher dispatcher,
        ILogger<EFaturaProviderGatewayAdapter> logger)
    {
        _dispatcher = dispatcher;
        _logger = logger;
    }

    public string GatewayName => GatewayKey;

    public async Task<EInvoiceSubmissionResult> SubmitAsync(EInvoiceSubmissionRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.UblTrXml))
        {
            return new EInvoiceSubmissionResult(null, "Failed", "Empty UBL-TR payload.", null);
        }

        EFaturaDocument document;
        try
        {
            document = MapToDocument(request);
        }
        catch (System.Xml.XmlException ex)
        {
            _logger.LogWarning(
                ex,
                "EFatura gateway adapter rejected payload for invoice {InvoiceId} (malformed UBL-TR XML).",
                request.InvoiceId);
            return new EInvoiceSubmissionResult(null, "Failed", $"Invalid UBL-TR XML: {ex.Message}", null);
        }

        try
        {
            var dispatch = await _dispatcher.SubmitAsync(document, ct).ConfigureAwait(false);
            return new EInvoiceSubmissionResult(
                RemoteUuid: dispatch.Result.Ettn,
                Status: dispatch.Result.Status,
                FailureReason: null,
                PdfUrl: null);
        }
        catch (AllProvidersFailedException ex)
        {
            _logger.LogError(
                ex,
                "All EFatura providers failed for invoice {InvoiceId} (tenant {TenantId}).",
                request.InvoiceId,
                request.TenantId);
            return new EInvoiceSubmissionResult(null, "Failed", "All EFatura providers failed.", null);
        }
        catch (ProviderNotConfiguredException ex)
        {
            _logger.LogWarning(
                ex,
                "EFatura provider not configured for invoice {InvoiceId} (tenant {TenantId}).",
                request.InvoiceId,
                request.TenantId);
            return new EInvoiceSubmissionResult(null, "Failed", "EFatura provider not configured.", null);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "EFatura gateway adapter dispatch failed for invoice {InvoiceId} (tenant {TenantId}).",
                request.InvoiceId,
                request.TenantId);
            return new EInvoiceSubmissionResult(null, "Failed", ex.GetBaseException().Message, null);
        }
    }

    public async Task<EInvoiceStatusResult> CheckStatusAsync(string remoteUuid, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(remoteUuid))
        {
            return new EInvoiceStatusResult("Unknown", "Empty remote uuid.", null);
        }

        try
        {
            var status = await _dispatcher.GetStatusAsync(remoteUuid, providerNameOverride: null, ct).ConfigureAwait(false);
            return new EInvoiceStatusResult(status.Status, null, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "EFatura status query failed for remote uuid {RemoteUuid}.",
                remoteUuid);
            return new EInvoiceStatusResult("Unknown", ex.GetBaseException().Message, null);
        }
    }

    public async Task<EInvoiceTaxpayerResult> CheckTaxpayerAsync(string taxNumber, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(taxNumber))
        {
            return new EInvoiceTaxpayerResult(taxNumber ?? string.Empty, false, null);
        }

        try
        {
            var status = await _dispatcher.CheckTaxpayerAsync(taxNumber, providerNameOverride: null, ct).ConfigureAwait(false);
            return new EInvoiceTaxpayerResult(status.TaxNumber, status.IsEFaturaRegistered, status.Alias);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "EFatura taxpayer check failed for tax number {TaxNumber}; defaulting to e-Arşiv.",
                taxNumber);
            return new EInvoiceTaxpayerResult(taxNumber, false, null);
        }
    }

    private static EFaturaDocument MapToDocument(EInvoiceSubmissionRequest request)
    {
        var doc = XDocument.Parse(request.UblTrXml);

        var documentNumber = ReadValue(doc, "ID") ?? request.InvoiceId.ToString("N");
        var issueDate = ParseDate(ReadValue(doc, "IssueDate")) ?? DateTime.UtcNow.Date;
        var currency = ReadValue(doc, "DocumentCurrencyCode") ?? "TRY";
        var buyerName = request.CustomerName ?? "Unknown";
        var buyerVkn = request.CustomerTaxNumber ?? string.Empty;

        var totalAmount = ParseDecimal(ReadValue(doc, "PayableAmount")) ?? 0m;

        var documentType = request.DocumentKind switch
        {
            EInvoiceDocumentKind.EArchive => EFaturaDocumentType.EArchive,
            EInvoiceDocumentKind.Despatch => EFaturaDocumentType.Despatch,
            _ => EFaturaDocumentType.Invoice,
        };

        return new EFaturaDocument(
            Type: documentType,
            DocumentNumber: documentNumber,
            IssueDate: issueDate,
            BuyerVkn: buyerVkn,
            BuyerName: buyerName,
            Lines: Array.Empty<EFaturaLine>(),
            Currency: currency,
            TotalAmount: totalAmount);
    }

    private static string? ReadValue(XDocument doc, string localName) =>
        doc.Descendants().FirstOrDefault(e => string.Equals(e.Name.LocalName, localName, StringComparison.Ordinal))?.Value?.Trim();

    private static DateTime? ParseDate(string? raw) =>
        DateTime.TryParse(raw, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out var dt)
            ? dt
            : null;

    private static decimal? ParseDecimal(string? raw) =>
        decimal.TryParse(raw, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var d)
            ? d
            : null;
}
