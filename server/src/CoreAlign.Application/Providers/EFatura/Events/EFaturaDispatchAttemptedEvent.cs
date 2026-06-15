namespace CoreAlign.Application.Providers.EFatura.Events;

public sealed record EFaturaDispatchAttemptedEvent(
    Guid TenantId,
    string ProviderName,
    string DocumentNumber,
    bool Succeeded,
    string? ErrorMessage,
    DateTime AttemptedAtUtc,
    TimeSpan Duration);
