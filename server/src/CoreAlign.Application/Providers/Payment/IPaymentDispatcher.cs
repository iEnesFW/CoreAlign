using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Providers.Payment;

/// <summary>
/// Payment orchestration entry point. Unlike <c>IEFaturaDispatcher</c> there is
/// NO failover here — a charge either succeeds against the tenant default
/// provider or fails outright. Retries are limited to transient HTTP errors;
/// declines, business failures, and refunds always surface to the caller.
/// </summary>
public interface IPaymentDispatcher
{
    Task<PaymentDispatchResult> ChargeAsync(PaymentChargeRequest request, CancellationToken cancellationToken = default);

    Task<Payment3DSecureInitResult> Initiate3DSecureAsync(Payment3DSecureRequest request, CancellationToken cancellationToken = default);

    Task<Payment3DSecureVerifyResult> Verify3DSecureAsync(Payment3DSecureCallback callback, CancellationToken cancellationToken = default);

    Task<PaymentRefundResult> RefundAsync(string transactionId, decimal? amount, string reason, CancellationToken cancellationToken = default);

    Task<PaymentTransactionInfo> GetTransactionAsync(string transactionId, CancellationToken cancellationToken = default);
}

public sealed record PaymentChargeRequest(
    Guid? OrderId,
    Guid? InvoiceId,
    decimal Amount,
    string Currency,
    string OrderReference,
    string BuyerName,
    string BuyerEmail,
    string? BuyerIp,
    string? CardToken,
    bool RequestThreeDSecure,
    string? CallbackUrl,
    IReadOnlyDictionary<string, string>? Metadata,
    string IdempotencyKey);

public sealed record PaymentDispatchResult(
    PaymentChargeOutcome Result,
    string ProviderUsed,
    string TransactionId,
    bool Requires3DSecure,
    string? RedirectUrl,
    IReadOnlyList<PaymentAttemptInfo> AttemptHistory);

public sealed record PaymentChargeOutcome(
    bool Success,
    string Status,
    decimal? AuthorizedAmount,
    string? Currency,
    string? FailureCode,
    string? FailureMessage,
    string? RawProviderJson);

public sealed record PaymentAttemptInfo(
    string ProviderName,
    bool Succeeded,
    string? ErrorCode,
    string? ErrorMessage,
    DateTime AttemptedAtUtc,
    TimeSpan Duration);

public sealed record Payment3DSecureRequest(
    Guid? OrderId,
    Guid? InvoiceId,
    decimal Amount,
    string Currency,
    string OrderReference,
    string CallbackUrl,
    string BuyerName,
    string BuyerEmail,
    string? BuyerIp,
    string? CardToken,
    IReadOnlyDictionary<string, string>? Metadata,
    string IdempotencyKey);

public sealed record Payment3DSecureInitResult(
    bool Initiated,
    string ProviderUsed,
    string TransactionId,
    string? HtmlContent,
    string? RedirectUrl,
    string? FailureCode,
    string? FailureMessage);

public sealed record Payment3DSecureCallback(
    string ProviderName,
    string TransactionId,
    IReadOnlyDictionary<string, string> CallbackFields);

public sealed record Payment3DSecureVerifyResult(
    bool Success,
    string ProviderUsed,
    string TransactionId,
    string Status,
    string? FailureCode,
    string? FailureMessage,
    string? RawProviderJson);

public sealed record PaymentRefundResult(
    bool Success,
    string ProviderUsed,
    string TransactionId,
    string? RefundId,
    decimal? RefundedAmount,
    string? FailureCode,
    string? FailureMessage);

public sealed record PaymentTransactionInfo(
    string ProviderName,
    string TransactionId,
    string Status,
    decimal? Amount,
    string? Currency,
    DateTime? CompletedAtUtc,
    string? RawProviderJson);

/// <summary>
/// Raised when payment orchestration cannot select a configured provider for
/// the current tenant or category. Mapped to HTTP 409 by the API pipeline.
/// </summary>
public sealed class PaymentProviderNotConfiguredException : Exception
{
    public Guid TenantId { get; }

    public PaymentProviderNotConfiguredException(Guid tenantId)
        : base($"No enabled payment provider is configured for tenant {tenantId}.")
    {
        TenantId = tenantId;
    }
}

/// <summary>
/// Raised when a charge or 3DS initiation is submitted with an idempotency
/// key that already exists for the tenant. The dispatcher honors the prior
/// outcome instead of double-charging.
/// </summary>
public sealed class DuplicatePaymentIdempotencyKeyException : Exception
{
    public Guid TenantId { get; }
    public string IdempotencyKey { get; }

    public DuplicatePaymentIdempotencyKeyException(Guid tenantId, string idempotencyKey)
        : base($"Payment idempotency key '{idempotencyKey}' already exists for tenant {tenantId}.")
    {
        TenantId = tenantId;
        IdempotencyKey = idempotencyKey;
    }
}

/// <summary>
/// Raised when the underlying provider returns a transaction lookup failure or
/// a stored transaction cannot be located. Distinct from a charge decline.
/// </summary>
public sealed class PaymentTransactionNotFoundException : Exception
{
    public string TransactionId { get; }

    public PaymentTransactionNotFoundException(string transactionId)
        : base($"Payment transaction '{transactionId}' could not be located.")
    {
        TransactionId = transactionId;
    }
}
