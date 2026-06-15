using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Entities.Payments;

/// <summary>
/// Provider-side payment transaction ledger. One row per dispatcher call so
/// reconciliation, refund, and audit pipelines have a deterministic
/// identifier even when the provider issues multiple sub-IDs. The lifecycle
/// state machine is driven by the dispatcher and reconciliation job; raw
/// provider payloads land in <see cref="MetadataJson"/>.
/// </summary>
public class PaymentTransaction : TenantEntity, IHasConcurrencyToken, ISoftDeletable
{
    public Guid? OrderId { get; private set; }
    public Guid? InvoiceId { get; private set; }
    public string OrderReference { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "TRY";

    public string ProviderName { get; private set; } = string.Empty;
    public string? ExternalTransactionId { get; private set; }

    public PaymentTransactionStatus Status { get; private set; } = PaymentTransactionStatus.Pending;
    public DateTime AttemptedAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; private set; }

    public bool RequiresThreeDSecure { get; private set; }
    public string? RedirectUrl { get; private set; }

    public string? FailureCode { get; private set; }
    public string? FailureReason { get; private set; }

    public decimal RefundedAmount { get; private set; }
    public string? MetadataJson { get; private set; }

    public string? IdempotencyKey { get; private set; }

    public long ConcurrencyToken { get; private set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public Guid? DeletedByUserId { get; set; }
    public string? DeletedReason { get; set; }

    protected PaymentTransaction() { }

    public PaymentTransaction(
        Guid tenantId,
        Guid? orderId,
        Guid? invoiceId,
        string orderReference,
        decimal amount,
        string currency,
        string providerName,
        string? externalTransactionId,
        bool requiresThreeDSecure,
        string? redirectUrl,
        string? metadataJson,
        string? idempotencyKey = null)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }
        if (string.IsNullOrWhiteSpace(orderReference))
        {
            throw new ArgumentException("OrderReference is required.", nameof(orderReference));
        }
        if (amount <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be positive.");
        }
        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency is required.", nameof(currency));
        }
        if (string.IsNullOrWhiteSpace(providerName))
        {
            throw new ArgumentException("ProviderName is required.", nameof(providerName));
        }

        TenantId = tenantId;
        OrderId = orderId;
        InvoiceId = invoiceId;
        OrderReference = orderReference.Trim();
        Amount = amount;
        Currency = currency.Trim().ToUpperInvariant();
        ProviderName = providerName.Trim();
        ExternalTransactionId = string.IsNullOrWhiteSpace(externalTransactionId) ? null : externalTransactionId.Trim();
        RequiresThreeDSecure = requiresThreeDSecure;
        RedirectUrl = redirectUrl;
        MetadataJson = metadataJson;
        IdempotencyKey = string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey.Trim();
        Status = PaymentTransactionStatus.Pending;
    }

    public void AttachExternalId(string externalTransactionId)
    {
        if (string.IsNullOrWhiteSpace(externalTransactionId))
        {
            throw new ArgumentException("ExternalTransactionId is required.", nameof(externalTransactionId));
        }
        ExternalTransactionId = externalTransactionId.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkAuthorized(string? externalTransactionId, string? metadataJson)
    {
        EnsureTransition(PaymentTransactionStatus.Authorized);
        if (!string.IsNullOrWhiteSpace(externalTransactionId))
        {
            ExternalTransactionId = externalTransactionId!.Trim();
        }
        if (metadataJson is not null)
        {
            MetadataJson = metadataJson;
        }
        Status = PaymentTransactionStatus.Authorized;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkCaptured(string? externalTransactionId, string? metadataJson)
    {
        EnsureTransition(PaymentTransactionStatus.Captured);
        if (!string.IsNullOrWhiteSpace(externalTransactionId))
        {
            ExternalTransactionId = externalTransactionId!.Trim();
        }
        if (metadataJson is not null)
        {
            MetadataJson = metadataJson;
        }
        Status = PaymentTransactionStatus.Captured;
        CompletedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = CompletedAtUtc.Value;
    }

    public void MarkFailed(string? failureCode, string? failureReason, string? metadataJson)
    {
        Status = PaymentTransactionStatus.Failed;
        FailureCode = failureCode;
        FailureReason = failureReason;
        if (metadataJson is not null)
        {
            MetadataJson = metadataJson;
        }
        CompletedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = CompletedAtUtc.Value;
    }

    public void MarkRequires3DSecure(string? externalTransactionId, string? redirectUrl, string? metadataJson)
    {
        if (Status != PaymentTransactionStatus.Pending)
        {
            throw new InvalidOperationException($"3DS required can only be set on a Pending transaction (was {Status}).");
        }
        if (!string.IsNullOrWhiteSpace(externalTransactionId))
        {
            ExternalTransactionId = externalTransactionId!.Trim();
        }
        RequiresThreeDSecure = true;
        RedirectUrl = redirectUrl;
        if (metadataJson is not null)
        {
            MetadataJson = metadataJson;
        }
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void RecordRefund(decimal refundAmount, string? metadataJson)
    {
        if (refundAmount <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(refundAmount), "Refund amount must be positive.");
        }
        if (Status is not PaymentTransactionStatus.Captured and not PaymentTransactionStatus.Authorized and not PaymentTransactionStatus.PartiallyRefunded)
        {
            throw new InvalidOperationException($"Cannot refund a transaction in status {Status}.");
        }

        var projected = Math.Round(RefundedAmount + refundAmount, 4);
        if (projected > Amount)
        {
            throw new InvalidOperationException($"Refund {projected} would exceed authorized amount {Amount}.");
        }

        RefundedAmount = projected;
        if (RefundedAmount >= Amount)
        {
            Status = PaymentTransactionStatus.Refunded;
        }
        else
        {
            Status = PaymentTransactionStatus.PartiallyRefunded;
        }

        if (metadataJson is not null)
        {
            MetadataJson = metadataJson;
        }
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkFullyRefunded(string? metadataJson)
    {
        RefundedAmount = Amount;
        Status = PaymentTransactionStatus.Refunded;
        if (metadataJson is not null)
        {
            MetadataJson = metadataJson;
        }
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkVoided(string? failureReason, string? metadataJson)
    {
        Status = PaymentTransactionStatus.Voided;
        FailureReason = failureReason;
        if (metadataJson is not null)
        {
            MetadataJson = metadataJson;
        }
        CompletedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = CompletedAtUtc.Value;
    }

    public void BumpConcurrencyToken()
    {
        ConcurrencyToken++;
    }

    public void MarkDeleted(Guid? userId, string? reason, DateTime utcNow)
    {
        if (IsDeleted) return;
        IsDeleted = true;
        DeletedAtUtc = utcNow;
        DeletedByUserId = userId;
        DeletedReason = reason;
        UpdatedAtUtc = utcNow;
    }

    public void Restore()
    {
        IsDeleted = false;
        DeletedAtUtc = null;
        DeletedByUserId = null;
        DeletedReason = null;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private void EnsureTransition(PaymentTransactionStatus target)
    {
        var allowed = (Status, target) switch
        {
            (PaymentTransactionStatus.Pending, PaymentTransactionStatus.Authorized) => true,
            (PaymentTransactionStatus.Pending, PaymentTransactionStatus.Captured) => true,
            (PaymentTransactionStatus.Authorized, PaymentTransactionStatus.Captured) => true,
            _ => false,
        };
        if (!allowed)
        {
            throw new InvalidOperationException($"Illegal payment transition {Status} -> {target}.");
        }
    }
}
