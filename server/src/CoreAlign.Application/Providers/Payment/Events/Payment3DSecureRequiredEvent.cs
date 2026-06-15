namespace CoreAlign.Application.Providers.Payment.Events;

public sealed record Payment3DSecureRequiredEvent(
    Guid TenantId,
    Guid? PaymentTransactionId,
    string ProviderName,
    string TransactionId,
    string OrderReference,
    decimal Amount,
    string Currency,
    string? RedirectUrl,
    DateTime RequiredAtUtc);
