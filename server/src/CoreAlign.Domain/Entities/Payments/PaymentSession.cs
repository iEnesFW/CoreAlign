using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Entities.Payments;

public class PaymentSession : TenantEntity
{
    public Guid InvoiceId { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid InitiatedByUserId { get; private set; }
    public string GatewayName { get; private set; } = string.Empty;
    public string IntentId { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "TRY";
    public PaymentSessionStatus Status { get; private set; } = PaymentSessionStatus.Initiated;
    public string? RedirectUrl { get; private set; }
    public string? ProviderReference { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }

    protected PaymentSession() { }

    public PaymentSession(
        Guid invoiceId,
        Guid customerId,
        Guid initiatedByUserId,
        string gatewayName,
        string intentId,
        decimal amount,
        string currency,
        string? redirectUrl)
    {
        if (invoiceId == Guid.Empty) throw new ArgumentException("InvoiceId is required.", nameof(invoiceId));
        if (customerId == Guid.Empty) throw new ArgumentException("CustomerId is required.", nameof(customerId));
        if (initiatedByUserId == Guid.Empty) throw new ArgumentException("InitiatedByUserId is required.", nameof(initiatedByUserId));
        if (string.IsNullOrWhiteSpace(gatewayName)) throw new ArgumentException("GatewayName is required.", nameof(gatewayName));
        if (string.IsNullOrWhiteSpace(intentId)) throw new ArgumentException("IntentId is required.", nameof(intentId));
        if (amount <= 0m) throw new ArgumentException("Amount must be positive.", nameof(amount));

        InvoiceId = invoiceId;
        CustomerId = customerId;
        InitiatedByUserId = initiatedByUserId;
        GatewayName = gatewayName.Trim();
        IntentId = intentId.Trim();
        Amount = amount;
        Currency = string.IsNullOrWhiteSpace(currency) ? "TRY" : currency.Trim();
        RedirectUrl = redirectUrl;
        Status = PaymentSessionStatus.Initiated;
    }

    public void MarkSucceeded(string? providerReference)
    {
        Status = PaymentSessionStatus.Succeeded;
        ProviderReference = providerReference;
        CompletedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = CompletedAtUtc.Value;
    }

    public void MarkFailed(string? reason)
    {
        Status = PaymentSessionStatus.Failed;
        FailureReason = reason;
        CompletedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = CompletedAtUtc.Value;
    }

    public void MarkCancelled(string? reason)
    {
        Status = PaymentSessionStatus.Cancelled;
        FailureReason = reason;
        CompletedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = CompletedAtUtc.Value;
    }
}
