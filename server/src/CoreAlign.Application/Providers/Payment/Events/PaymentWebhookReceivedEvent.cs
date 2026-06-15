namespace CoreAlign.Application.Providers.Payment.Events;

public sealed record PaymentWebhookReceivedEvent(
    Guid TenantId,
    string ProviderName,
    string SignatureHash,
    string EventType,
    DateTime ReceivedAtUtc);
