using CoreAlign.Application.Common;
using CoreAlign.Domain.Common;
using MediatR;

namespace CoreAlign.Application.Providers.Payment.Commands;

/// <summary>
/// Charges a payment via the tenant's primary payment provider. The
/// command is wrapped by the MediatR <c>TransactionBehavior</c> so the
/// underlying <c>PaymentTransaction</c> ledger row, audit, and outbox
/// envelope all commit atomically.
///
/// <para><b>Idempotency.</b> <see cref="IdempotencyKey"/> is required and
/// is supplied by the frontend (typically the checkout session id). A
/// duplicate submission with the same key returns the prior outcome
/// instead of re-charging the card.</para>
/// </summary>
public sealed record ChargePaymentCommand(
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
    string IdempotencyKey)
    : IRequest<PaymentDispatchResult>, ITransactionalRequest, IAuditableMutation
{
    public Guid AggregateId => OrderId ?? InvoiceId ?? Guid.Empty;
    public string AggregateType => "PaymentTransaction";
}

/// <summary>
/// Initiates a 3-D Secure challenge with the tenant's primary payment
/// provider. The browser-side flow is responsible for redirecting the
/// cardholder; <see cref="VerifyThreeDSecureCommand"/> closes the loop
/// when the issuer posts back.
/// </summary>
public sealed record InitiateThreeDSecureCommand(
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
    string IdempotencyKey)
    : IRequest<Payment3DSecureInitResult>, ITransactionalRequest, IAuditableMutation
{
    public Guid AggregateId => OrderId ?? InvoiceId ?? Guid.Empty;
    public string AggregateType => "PaymentTransaction";
}

/// <summary>
/// Verifies a 3-D Secure issuer callback. Always runs in a transaction so
/// the resulting <c>PaymentTransaction</c> state transition (Captured /
/// Failed) is durable before the success/fail redirect is sent.
/// </summary>
public sealed record VerifyThreeDSecureCommand(
    string ProviderName,
    string TransactionId,
    IReadOnlyDictionary<string, string> CallbackFields)
    : IRequest<Payment3DSecureVerifyResult>, ITransactionalRequest, IAuditableMutation
{
    public Guid AggregateId => Guid.Empty;
    public string AggregateType => "PaymentTransaction";
}

/// <summary>
/// Refunds (full or partial) a previously captured payment transaction.
/// </summary>
public sealed record RefundPaymentCommand(
    string TransactionId,
    decimal? Amount,
    string Reason)
    : IRequest<PaymentRefundResult>, ITransactionalRequest, IAuditableMutation
{
    public Guid AggregateId => Guid.TryParse(TransactionId, out var id) ? id : Guid.Empty;
    public string AggregateType => "PaymentTransaction";
}
