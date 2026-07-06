using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Providers.EFatura;

public enum EFaturaDocumentType
{
    Invoice,
    Despatch,
    ProducerReceipt,
    EArchive,
    SelfEmployedReceipt
}

public sealed record EFaturaLine(
    decimal Quantity,
    string Name,
    decimal UnitPrice,
    decimal VatRate);

public sealed record EFaturaDocument(
    EFaturaDocumentType Type,
    string DocumentNumber,
    DateTime IssueDate,
    string BuyerVkn,
    string BuyerName,
    IReadOnlyList<EFaturaLine> Lines,
    string Currency,
    decimal TotalAmount,
    // The full UBL-TR XML (invoice or DespatchAdvice) produced upstream. When present it is sent
    // to the provider verbatim; the scalar fields above are a parsed summary for logging/routing.
    string? RawUblTrXml = null);

public sealed record EFaturaCredentials(
    string Username,
    string Password,
    string ApiUrl);

public sealed record EFaturaSubmitResult(
    string Ettn,
    string Status,
    string ProviderRefId,
    DateTime SubmittedAtUtc);

public sealed record EFaturaStatus(
    string Ettn,
    string Status,
    DateTime LastCheckedUtc);

public sealed record EFaturaCancelResult(
    string Ettn,
    bool Cancelled,
    string Reason);

public sealed record EFaturaXmlBuildResult(
    string XmlPayload,
    string ProfileId,
    string DocumentUuid);

public sealed record EFaturaIssueRequest(
    EFaturaDocument Document,
    string UblXmlBase64,
    string? InvoiceType = null,
    Guid TenantId = default,
    object? Credentials = null);

public sealed record EFaturaIssueResult(
    string Uuid,
    string Status,
    string? GibStatus,
    DateTime SentAtUtc);

public sealed record EFaturaProviderStatus(
    string Uuid,
    string CurrentStatus,
    string? GibResponseCode,
    DateTime? DeliveredAtUtc);

public sealed record EFaturaInboxItem(
    string Uuid,
    string SenderVkn,
    string DocumentNumber,
    DateTime IssueDate,
    string Status);

public sealed record EFaturaCreditNoteRequest(
    string OriginalUuid,
    decimal RefundAmount,
    string Currency,
    string? Reason = null,
    Guid TenantId = default,
    object? Credentials = null);

public sealed record EFaturaCreditNoteResult(
    string Uuid,
    string Status,
    DateTime IssuedAtUtc);

public sealed record EFaturaCancelInvoiceRequest(
    string Uuid,
    string Reason,
    Guid TenantId = default,
    object? Credentials = null);

public sealed record EFaturaGetStatusRequest(
    string Uuid,
    Guid TenantId = default,
    object? Credentials = null);

public sealed record EFaturaListReceivedRequest(
    DateTime FromUtc,
    DateTime ToUtc,
    Guid TenantId = default,
    object? Credentials = null);

public sealed record EFaturaTaxpayerCheckRequest(
    string TaxNumber,
    Guid TenantId = default,
    object? Credentials = null);

public sealed record EFaturaTaxpayerStatus(
    string TaxNumber,
    bool IsEFaturaRegistered,
    string? Alias = null,
    string? Title = null);

[Flags]
public enum EFaturaProviderCapabilities
{
    None = 0,
    CanIssue = 1,
    CanCancel = 2,
    CanCreditNote = 4,
    CanQueryStatus = 8,
    CanListReceived = 16,
    CanWebhook = 32,
    CanCheckTaxpayer = 64
}

public interface IEFaturaProvider : IExternalProvider
{
    /// <summary>Capability surface advertised by the concrete provider for the F2.x flows.</summary>
    EFaturaProviderCapabilities SupportedCapabilities => EFaturaProviderCapabilities.CanIssue;

    /// <summary>
    /// Decrypts the tenant-scoped credential blob into the strongly-typed credential
    /// record this provider expects, so the dispatcher can inject typed credentials
    /// into request DTOs without provider-side credential coupling.
    /// </summary>
    object? UnprotectCredentials(IProviderCredentialProtector protector, Guid tenantId, string? encryptedJson);

    /// <summary>Submit a UBL-TR invoice using the F2.x tenant-resolved credential pipeline.</summary>
    Task<EFaturaIssueResult> IssueAsync(EFaturaIssueRequest request, CancellationToken ct);

    /// <summary>Cancel a previously issued invoice by UUID.</summary>
    Task<EFaturaCancelResult> CancelAsync(EFaturaCancelInvoiceRequest request, CancellationToken ct);

    /// <summary>Query the latest delivery/status for an invoice UUID.</summary>
    Task<EFaturaProviderStatus> GetStatusAsync(EFaturaGetStatusRequest request, CancellationToken ct);

    /// <summary>List inbound invoices addressed to the tenant within a date range.</summary>
    Task<IReadOnlyList<EFaturaInboxItem>> ListReceivedAsync(EFaturaListReceivedRequest request, CancellationToken ct) =>
        throw new NotSupportedException($"{Name} does not implement ListReceivedAsync.");

    /// <summary>Issue a credit note (iade faturasi) against a previous invoice.</summary>
    Task<EFaturaCreditNoteResult> CreditNoteAsync(EFaturaCreditNoteRequest request, CancellationToken ct) =>
        throw new NotSupportedException($"{Name} does not implement CreditNoteAsync.");

    /// <summary>Check whether a VKN/TCKN is a registered e-Fatura taxpayer (mukellef sorgusu).</summary>
    Task<EFaturaTaxpayerStatus> CheckTaxpayerAsync(EFaturaTaxpayerCheckRequest request, CancellationToken ct) =>
        throw new NotSupportedException($"{Name} does not implement CheckTaxpayerAsync.");
}
