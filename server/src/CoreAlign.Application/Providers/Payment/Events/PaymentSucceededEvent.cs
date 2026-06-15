namespace CoreAlign.Application.Providers.Payment.Events;

public sealed record PaymentSucceededEvent(
    Guid TenantId,
    Guid? PaymentTransactionId,
    string ProviderName,
    string ExternalTransactionId,
    string OrderReference,
    decimal Amount,
    string Currency,
    DateTime CompletedAtUtc);
