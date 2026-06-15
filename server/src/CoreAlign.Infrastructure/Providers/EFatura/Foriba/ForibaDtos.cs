namespace CoreAlign.Infrastructure.Providers.EFatura.Foriba;

public sealed record ForibaCredentials(
    string Username,
    string Password,
    string WebhookSecret,
    bool IsSandbox);

public sealed record ForibaInvoiceRequest(
    string DocumentUuid,
    string DocumentNumber,
    string BuyerVkn,
    string UblXml,
    string Action);

public sealed record ForibaInvoiceResult(
    string Uuid,
    string Status,
    string GibResponseCode,
    string? ProviderRefId);

public sealed record ForibaStatusResult(
    string Uuid,
    string Status,
    string? GibResponseCode,
    DateTime LastUpdatedUtc);

public sealed record ForibaInboxItem(
    string Uuid,
    string SenderVkn,
    string DocumentNumber,
    DateTime IssueDate,
    string Status);
