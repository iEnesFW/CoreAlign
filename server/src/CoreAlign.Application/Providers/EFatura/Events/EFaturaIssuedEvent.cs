namespace CoreAlign.Application.Providers.EFatura.Events;

public sealed record EFaturaIssuedEvent(
    Guid TenantId,
    Guid InvoiceId,
    string Ettn,
    string ProviderName,
    string Status,
    DateTime IssuedAtUtc);
