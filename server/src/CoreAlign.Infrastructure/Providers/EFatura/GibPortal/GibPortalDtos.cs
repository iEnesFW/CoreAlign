namespace CoreAlign.Infrastructure.Providers.EFatura.GibPortal;

public sealed record GibPortalCredentials(
    string IssuerVkn,
    string TaxOfficeCode,
    string PortalBaseUrl,
    bool IsSandbox,
    string? SessionCookie = null);

public sealed record GibPortalConfig(
    string IssuerVkn,
    string TaxOfficeCode,
    string PortalBaseUrl,
    bool IsSandbox);

public sealed record GibPortalPreparedInvoiceResult(
    string Uuid,
    string UblXmlContent,
    string DownloadFileName,
    string? DownloadUrl,
    DateTime PreparedAtUtc);

public sealed record GibPortalCreditNoteResult(
    string Uuid,
    string UblXmlContent,
    string DownloadFileName,
    DateTime PreparedAtUtc);
