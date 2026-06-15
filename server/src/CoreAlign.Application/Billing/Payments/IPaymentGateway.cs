namespace CoreAlign.Application.Billing.Payments;

/// <summary>
/// Production-grade abstraction over a third-party payment provider. The shape
/// supports both redirect-flow (Iyzico, Stripe Checkout) and direct/manual
/// (bank transfer, mock dev gateway).
///
/// <para><b>Extension pattern</b></para>
/// To add a new provider:
/// <list type="number">
///   <item>Implement <see cref="IPaymentGateway"/> in CoreAlign.Infrastructure.Payments,
///   setting <see cref="Name"/> to a unique key (e.g. "iyzico", "stripe").</item>
///   <item>Verify the provider's webhook signature inside <see cref="HandleWebhookAsync"/>
///   — never trust the request body without verification.</item>
///   <item>Register the implementation in DI before <c>IPaymentGatewayRegistry</c>;
///   the registry picks it up automatically by <see cref="Name"/>.</item>
/// </list>
/// The <c>POST /api/v1/billing/webhooks/{gatewayName}</c> route dispatches to
/// the matching gateway with <see cref="ProcessPaymentWebhookCommand"/>; the
/// rest of the billing pipeline (order status transitions, fan-out,
/// provisioning, notifications) is provider-agnostic.
/// </summary>
public interface IPaymentGateway
{
    /// <summary>Unique key, used both for routing and to look the gateway up via the registry.</summary>
    string Name { get; }

    Task<PaymentIntentResult> CreateIntentAsync(PaymentIntentRequest request, CancellationToken cancellationToken);

    Task<WebhookProcessingResult> HandleWebhookAsync(string payload, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken);

    Task<CaptureResult> CaptureAsync(CaptureRequest request, CancellationToken cancellationToken);

    Task<RefundResult> RefundAsync(RefundRequest request, CancellationToken cancellationToken);
}

public enum PaymentIntentStatus
{
    Pending = 0,
    Succeeded = 1,
    Failed = 2,
    Cancelled = 3,
    RequiresAction = 4,
}

public sealed record PaymentIntentRequest(
    Guid OrderId,
    string OrderNumber,
    decimal Amount,
    string Currency,
    Guid TenantId,
    Guid CreatedByUserId,
    string? Description,
    IReadOnlyDictionary<string, string>? Metadata,
    PaymentBillingInfo? BillingInfo = null,
    IReadOnlyList<PaymentLineItem>? LineItems = null);

/// <summary>
/// Buyer / billing snapshot the gateway needs (real providers like Iyzico,
/// Stripe require these). Optional for the mock gateway.
/// </summary>
public sealed record PaymentBillingInfo(
    string Name,
    string Surname,
    string Email,
    string GsmNumber,
    string IdentityNumber,
    string IpAddress,
    string Address,
    string City,
    string Country,
    string ZipCode);

/// <summary>
/// Per-line snapshot passed to the gateway. Iyzico requires the basket items
/// to sum exactly to the total — caller composes these to match Order.TotalAmount.
/// </summary>
public sealed record PaymentLineItem(
    string Id,
    string Name,
    string Category,
    decimal UnitPrice);

public sealed record PaymentIntentResult(
    string IntentId,
    string? RedirectUrl,
    PaymentIntentStatus Status,
    IReadOnlyDictionary<string, string>? Metadata,
    string? RawJson);

public sealed record WebhookProcessingResult(
    string IntentId,
    PaymentIntentStatus Status,
    string? Reference,
    string? FailureReason,
    string RawJson);

public sealed record CaptureRequest(string IntentId, decimal? Amount);
public sealed record CaptureResult(bool Success, string? Reference, string? RawJson, string? FailureReason);

public sealed record RefundRequest(
    string IntentId,
    decimal? Amount,
    string? Reason,
    string? PaymentTransactionId = null,
    string? Currency = null);

public sealed record RefundResult(bool Success, string? RefundId, string? RawJson, string? FailureReason);

/// <summary>
/// Raised by a gateway when a webhook signature/payload could not be verified.
/// Routed to a 401 by the API layer so legitimate retries do not get cached as
/// failed by the provider's bounded-retry logic.
/// </summary>
public sealed class PaymentWebhookSignatureException : Exception
{
    public PaymentWebhookSignatureException(string message) : base(message) { }
}

/// <summary>
/// Raised by a gateway when an upstream call (intent creation, refund, etc.)
/// returned a known business error. Carries the safe provider-side error
/// message and code — NEVER the request body / signing material.
/// </summary>
public sealed class PaymentGatewayException : Exception
{
    public string? ErrorCode { get; }
    public PaymentGatewayException(string message, string? errorCode = null) : base(message)
    {
        ErrorCode = errorCode;
    }
}
