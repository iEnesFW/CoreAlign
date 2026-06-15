using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Entities;

/// <summary>
/// Audit record for one payment-gateway interaction tied to a
/// <see cref="SubscriptionOrder"/>. <c>RawResponseJson</c> stores the unparsed
/// provider payload so disputes/refunds can be reconstructed end-to-end.
/// </summary>
public class PaymentAttempt : TenantEntity
{
    public Guid SubscriptionOrderId { get; private set; }
    public string GatewayName { get; private set; } = string.Empty;
    public string? IntentId { get; private set; }
    public PaymentAttemptStatus Status { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "TRY";
    public string? RawResponseJson { get; private set; }
    public DateTime AttemptedAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; private set; }
    public string? FailureReason { get; private set; }

    public SubscriptionOrder Order { get; private set; } = null!;

    protected PaymentAttempt() { }

    public PaymentAttempt(
        Guid subscriptionOrderId,
        string gatewayName,
        string? intentId,
        PaymentAttemptStatus status,
        decimal amount,
        string currency,
        string? rawResponseJson,
        string? failureReason = null)
    {
        if (subscriptionOrderId == Guid.Empty) throw new ArgumentException("SubscriptionOrderId is required.", nameof(subscriptionOrderId));
        if (string.IsNullOrWhiteSpace(gatewayName)) throw new ArgumentException("GatewayName is required.", nameof(gatewayName));
        if (amount < 0m) throw new ArgumentOutOfRangeException(nameof(amount));
        if (string.IsNullOrWhiteSpace(currency) || currency.Length > 3) throw new ArgumentException("Currency must be a 1-3 char code.", nameof(currency));

        SubscriptionOrderId = subscriptionOrderId;
        GatewayName = gatewayName.Trim();
        IntentId = string.IsNullOrWhiteSpace(intentId) ? null : intentId.Trim();
        Status = status;
        Amount = amount;
        Currency = currency.Trim().ToUpperInvariant();
        RawResponseJson = rawResponseJson;
        FailureReason = failureReason?.Trim();
        if (status is PaymentAttemptStatus.Succeeded or PaymentAttemptStatus.Failed or PaymentAttemptStatus.Cancelled or PaymentAttemptStatus.Refunded)
        {
            CompletedAtUtc = AttemptedAtUtc;
        }
    }
}
