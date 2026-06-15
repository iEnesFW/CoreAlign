using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Entities.Notifications;

public class NotificationMessage : TenantEntity, IHasConcurrencyToken, ISoftDeletable
{
    public Guid? UserId { get; private set; }
    public Guid? CustomerId { get; private set; }
    public NotificationChannel Channel { get; private set; }
    public string TemplateKey { get; private set; } = string.Empty;
    public string Locale { get; private set; } = "en";
    public string RecipientAddress { get; private set; } = string.Empty;
    public string? Subject { get; private set; }
    public string BodyMarkdown { get; private set; } = string.Empty;
    public string PayloadJson { get; private set; } = "{}";
    public string CategoryKey { get; private set; } = string.Empty;
    public NotificationStatus Status { get; private set; } = NotificationStatus.Pending;
    public DateTime? SentAtUtc { get; private set; }
    public DateTime? DeliveredAtUtc { get; private set; }
    public DateTime? ReadAtUtc { get; private set; }
    public string? FailureReason { get; private set; }
    public int RetryCount { get; private set; }
    public string? ProviderUsed { get; private set; }
    public string? ProviderMessageId { get; private set; }
    public string IdempotencyHash { get; private set; } = string.Empty;

    public long ConcurrencyToken { get; private set; }
    void IHasConcurrencyToken.BumpConcurrencyToken() => ConcurrencyToken++;

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public Guid? DeletedByUserId { get; set; }
    public string? DeletedReason { get; set; }

    public void MarkDeleted(Guid? userId, string? reason, DateTime utcNow)
    {
        ((ISoftDeletable)this).MarkDeletedInternal(userId, reason, utcNow);
        UpdatedAtUtc = utcNow;
    }

    public void Restore()
    {
        ((ISoftDeletable)this).RestoreInternal();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    protected NotificationMessage() { }

    public NotificationMessage(
        Guid tenantId,
        Guid? userId,
        Guid? customerId,
        NotificationChannel channel,
        string templateKey,
        string locale,
        string recipientAddress,
        string categoryKey,
        string? subject,
        string bodyMarkdown,
        string payloadJson,
        string? idempotencyHash = null)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(templateKey)) throw new ArgumentException("TemplateKey is required.", nameof(templateKey));
        if (string.IsNullOrWhiteSpace(locale)) throw new ArgumentException("Locale is required.", nameof(locale));
        if (string.IsNullOrWhiteSpace(recipientAddress)) throw new ArgumentException("RecipientAddress is required.", nameof(recipientAddress));
        if (string.IsNullOrWhiteSpace(categoryKey)) throw new ArgumentException("CategoryKey is required.", nameof(categoryKey));

        TenantId = tenantId;
        UserId = userId;
        CustomerId = customerId;
        Channel = channel;
        TemplateKey = templateKey.Trim();
        Locale = locale.Trim();
        RecipientAddress = recipientAddress.Trim();
        CategoryKey = categoryKey.Trim();
        Subject = subject?.Trim();
        BodyMarkdown = bodyMarkdown ?? string.Empty;
        PayloadJson = string.IsNullOrWhiteSpace(payloadJson) ? "{}" : payloadJson;
        IdempotencyHash = string.IsNullOrWhiteSpace(idempotencyHash) ? string.Empty : idempotencyHash.Trim();
        Status = NotificationStatus.Pending;
    }

    public void MarkQueued(DateTime utcNow)
    {
        Status = NotificationStatus.Queued;
        UpdatedAtUtc = utcNow;
    }

    public void MarkSending() { Status = NotificationStatus.Sending; UpdatedAtUtc = DateTime.UtcNow; }

    public void MarkSent(string? providerName, string? providerMessageId, DateTime utcNow)
    {
        Status = NotificationStatus.Sent;
        ProviderUsed = providerName;
        ProviderMessageId = providerMessageId;
        SentAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public void MarkDelivered(DateTime utcNow)
    {
        Status = NotificationStatus.Delivered;
        DeliveredAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public void MarkFailed(string failureReason, DateTime utcNow)
    {
        Status = NotificationStatus.Failed;
        FailureReason = failureReason;
        RetryCount++;
        UpdatedAtUtc = utcNow;
    }

    public void MarkBounced(string? reason, DateTime utcNow)
    {
        Status = NotificationStatus.Bounced;
        FailureReason = reason;
        UpdatedAtUtc = utcNow;
    }

    public void MarkRead(DateTime utcNow)
    {
        if (Status == NotificationStatus.Read) return;
        Status = NotificationStatus.Read;
        ReadAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }
}
