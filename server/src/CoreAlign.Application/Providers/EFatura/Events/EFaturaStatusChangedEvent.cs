namespace CoreAlign.Application.Providers.EFatura.Events;

public sealed record EFaturaStatusChangedEvent(
    Guid TenantId,
    string Ettn,
    string ProviderName,
    string? PreviousStatus,
    string CurrentStatus,
    DateTime ChangedAtUtc);
