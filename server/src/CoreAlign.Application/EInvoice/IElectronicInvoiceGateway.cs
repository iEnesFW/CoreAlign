namespace CoreAlign.Application.EInvoice;

public interface IElectronicInvoiceGateway
{
    string GatewayName { get; }
    Task<EInvoiceSubmissionResult> SubmitAsync(EInvoiceSubmissionRequest request, CancellationToken ct);
    Task<EInvoiceStatusResult> CheckStatusAsync(string remoteUuid, CancellationToken ct);
}

public record EInvoiceSubmissionRequest(
    Guid TenantId,
    Guid InvoiceId,
    string UblTrXml,
    string? CustomerTaxNumber,
    string CustomerName);

public record EInvoiceSubmissionResult(
    string? RemoteUuid,
    string Status,
    string? FailureReason,
    string? PdfUrl);

public record EInvoiceStatusResult(
    string Status,
    string? FailureReason,
    string? PdfUrl);
