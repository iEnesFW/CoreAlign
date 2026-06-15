namespace CoreAlign.Application.Providers.Payment.Events;

public sealed record PaymentInitiatedEvent(
    Guid TenantId,
    Guid? PaymentTransactionId,
    string ProviderName,
    string OrderReference,
    decimal Amount,
    string Currency,
    DateTime InitiatedAtUtc);
