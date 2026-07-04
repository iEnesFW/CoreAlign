namespace CoreAlign.Application.EInvoice;

public interface IElectronicInvoiceGateway
{
    string GatewayName { get; }
    Task<EInvoiceSubmissionResult> SubmitAsync(EInvoiceSubmissionRequest request, CancellationToken ct);
    Task<EInvoiceStatusResult> CheckStatusAsync(string remoteUuid, CancellationToken ct);

    Task<EInvoiceTaxpayerResult> CheckTaxpayerAsync(string taxNumber, CancellationToken ct) =>
        Task.FromResult(new EInvoiceTaxpayerResult(taxNumber, false, null));
}

public enum EInvoiceDocumentKind
{
    EFatura = 0,
    EArchive = 1
}

public record EInvoiceTaxpayerResult(string TaxNumber, bool IsEFaturaRegistered, string? Alias);

public record EInvoiceSubmissionRequest(
    Guid TenantId,
    Guid InvoiceId,
    string UblTrXml,
    string? CustomerTaxNumber,
    string CustomerName,
    EInvoiceDocumentKind DocumentKind = EInvoiceDocumentKind.EFatura);

public record EInvoiceSubmissionResult(
    string? RemoteUuid,
    string Status,
    string? FailureReason,
    string? PdfUrl);

public record EInvoiceStatusResult(
    string Status,
    string? FailureReason,
    string? PdfUrl);
