namespace CoreAlign.Application.Providers.Payment.Events;

public sealed record PaymentFailedEvent(
    Guid TenantId,
    Guid? PaymentTransactionId,
    string ProviderName,
    string? ExternalTransactionId,
    string OrderReference,
    decimal Amount,
    string Currency,
    string? FailureCode,
    string? FailureMessage,
    DateTime FailedAtUtc);
