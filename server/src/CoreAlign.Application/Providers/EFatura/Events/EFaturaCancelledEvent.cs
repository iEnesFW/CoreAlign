namespace CoreAlign.Application.Providers.EFatura.Events;

public sealed record EFaturaCancelledEvent(
    Guid TenantId,
    string Ettn,
    string ProviderName,
    string Reason,
    bool Confirmed,
    DateTime CancelledAtUtc);
