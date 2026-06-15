namespace CoreAlign.Application.Providers.Payment.Events;

public sealed record PaymentRefundedEvent(
    Guid TenantId,
    Guid? PaymentTransactionId,
    string ProviderName,
    string ExternalTransactionId,
    string? RefundId,
    decimal RefundedAmount,
    string Currency,
    string Reason,
    bool FullyRefunded,
    DateTime RefundedAtUtc);
