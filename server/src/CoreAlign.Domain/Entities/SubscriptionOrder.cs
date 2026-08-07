using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Entities;

/// <summary>
/// Per-tenant subscription purchase. Captures the order, totals snapshot, the
/// payment gateway it was routed through, and the lifecycle of the payment
/// (PendingPayment -> Paid/Failed/Cancelled -> Completed once modules are
/// provisioned). Items hold a price snapshot so the order survives catalog
/// changes.
/// </summary>
public class SubscriptionOrder : TenantEntity, IHasConcurrencyToken
{
    public long ConcurrencyToken { get; private set; }
    void IHasConcurrencyToken.BumpConcurrencyToken() => ConcurrencyToken++;

    public string OrderNumber { get; private set; } = string.Empty;
    /// <summary>Client-supplied replay key; a retry of the same checkout returns the first order.</summary>
    public Guid? OperationId { get; private set; }
    public SubscriptionOrderStatus Status { get; private set; }
    public decimal TotalAmount { get; private set; }
    public string Currency { get; private set; } = "TRY";
    public Guid CreatedByUserId { get; private set; }
    public string? GatewayName { get; private set; }
    public string? GatewayIntentId { get; private set; }
    public string? GatewayRedirectUrl { get; private set; }
    public string? PaymentReference { get; private set; }
    public string? PaymentTransactionId { get; private set; }
    public DateTime? PaidAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public string? Notes { get; private set; }

    public string? BuyerName { get; private set; }
    public string? BuyerSurname { get; private set; }
    public string? BuyerEmail { get; private set; }
    public string? BuyerGsmNumber { get; private set; }
    public string? BuyerIdentityNumber { get; private set; }
    public string? BuyerIpAddress { get; private set; }
    public string? BillingAddress { get; private set; }
    public string? BillingCity { get; private set; }
    public string? BillingCountry { get; private set; }
    public string? BillingZipCode { get; private set; }

    public ICollection<SubscriptionOrderItem> Items { get; private set; } = new List<SubscriptionOrderItem>();
    public ICollection<PaymentAttempt> Attempts { get; private set; } = new List<PaymentAttempt>();

    protected SubscriptionOrder() { }

    public SubscriptionOrder(string orderNumber, Guid createdByUserId, string currency, string? notes = null, Guid? operationId = null)
    {
        if (string.IsNullOrWhiteSpace(orderNumber)) throw new ArgumentException("OrderNumber is required.", nameof(orderNumber));
        if (createdByUserId == Guid.Empty) throw new ArgumentException("CreatedByUserId is required.", nameof(createdByUserId));
        if (string.IsNullOrWhiteSpace(currency) || currency.Length > 3) throw new ArgumentException("Currency must be a 1-3 char code.", nameof(currency));

        OrderNumber = orderNumber.Trim();
        OperationId = operationId == Guid.Empty ? null : operationId;
        CreatedByUserId = createdByUserId;
        Currency = currency.Trim().ToUpperInvariant();
        Notes = notes?.Trim();
        Status = SubscriptionOrderStatus.Draft;
    }

    public void AddItem(SubscriptionOrderItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        Items.Add(item);
        TotalAmount = Items.Sum(i => i.UnitPrice);
    }

    public void MoveToPendingPayment()
    {
        if (Status != SubscriptionOrderStatus.Draft) throw new InvalidOperationException($"Cannot move from {Status} to PendingPayment.");
        if (Items.Count == 0) throw new InvalidOperationException("Cannot move an empty order to PendingPayment.");
        Status = SubscriptionOrderStatus.PendingPayment;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    // WHY the redirect URL is stored: a replayed checkout must hand back the SAME hosted-payment
    // page. Re-creating the intent to recover it would charge the buyer twice.
    public void AttachIntent(string gatewayName, string? intentId, string? redirectUrl = null)
    {
        if (string.IsNullOrWhiteSpace(gatewayName)) throw new ArgumentException("GatewayName is required.", nameof(gatewayName));
        GatewayName = gatewayName.Trim();
        GatewayIntentId = string.IsNullOrWhiteSpace(intentId) ? null : intentId.Trim();
        GatewayRedirectUrl = Normalize(redirectUrl, 1000);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkPaid(string? paymentReference, string? paymentTransactionId = null)
    {
        if (Status == SubscriptionOrderStatus.Paid || Status == SubscriptionOrderStatus.Expired) return;
        if (Status == SubscriptionOrderStatus.Cancelled || Status == SubscriptionOrderStatus.Failed)
        {
            throw new InvalidOperationException($"Cannot mark a {Status} order as Paid.");
        }
        Status = SubscriptionOrderStatus.Paid;
        PaymentReference = string.IsNullOrWhiteSpace(paymentReference) ? null : paymentReference.Trim();
        if (!string.IsNullOrWhiteSpace(paymentTransactionId))
        {
            PaymentTransactionId = paymentTransactionId.Trim();
        }
        PaidAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = PaidAtUtc.Value;
    }

    /// <summary>
    /// Atomically snapshots the buyer + billing information passed in at checkout.
    /// Stored per-order (not on the tenant) so a future invoice / audit trail keeps
    /// the exact details the customer entered for this purchase.
    /// IdentityNumber is treated as PII: trim/store as-is, never echo it back in DTOs.
    /// </summary>
    public void AttachBillingInfo(
        string? name,
        string? surname,
        string? email,
        string? gsmNumber,
        string? identityNumber,
        string? ipAddress,
        string? address,
        string? city,
        string? country,
        string? zipCode)
    {
        BuyerName = Normalize(name, 100);
        BuyerSurname = Normalize(surname, 100);
        BuyerEmail = Normalize(email, 256);
        BuyerGsmNumber = Normalize(gsmNumber, 32);
        BuyerIdentityNumber = Normalize(identityNumber, 32);
        BuyerIpAddress = Normalize(ipAddress, 64);
        BillingAddress = Normalize(address, 500);
        BillingCity = Normalize(city, 100);
        BillingCountry = Normalize(country, 100);
        BillingZipCode = Normalize(zipCode, 32);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private static string? Normalize(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }

    public void MarkFailed(string? reason)
    {
        if (Status == SubscriptionOrderStatus.Paid) throw new InvalidOperationException("Cannot fail a Paid order.");
        Status = SubscriptionOrderStatus.Failed;
        Notes = AppendNote(Notes, reason);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkCancelled(string? reason)
    {
        if (Status == SubscriptionOrderStatus.Paid) throw new InvalidOperationException("Cannot cancel a Paid order.");
        Status = SubscriptionOrderStatus.Cancelled;
        Notes = AppendNote(Notes, reason);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkCompleted()
    {
        if (Status != SubscriptionOrderStatus.Paid) throw new InvalidOperationException($"Cannot complete from {Status}.");
        CompletedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = CompletedAtUtc.Value;
    }

    private static string? AppendNote(string? existing, string? addition)
    {
        if (string.IsNullOrWhiteSpace(addition)) return existing;
        var trimmed = addition.Trim();
        return string.IsNullOrWhiteSpace(existing) ? trimmed : existing + " | " + trimmed;
    }
}
