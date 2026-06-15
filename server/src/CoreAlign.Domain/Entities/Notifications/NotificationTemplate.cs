using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Entities.Notifications;

public class NotificationTemplate : BaseEntity, IHasConcurrencyToken, ISoftDeletable
{
    public Guid? TenantId { get; private set; }
    public string Key { get; private set; } = string.Empty;
    public NotificationChannel Channel { get; private set; }
    public string Locale { get; private set; } = "en";
    public string? Subject { get; private set; }
    public string BodyTemplate { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

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

    protected NotificationTemplate() { }

    public NotificationTemplate(
        Guid? tenantId,
        string key,
        NotificationChannel channel,
        string locale,
        string? subject,
        string bodyTemplate)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key is required.", nameof(key));
        if (string.IsNullOrWhiteSpace(locale)) throw new ArgumentException("Locale is required.", nameof(locale));
        if (string.IsNullOrWhiteSpace(bodyTemplate)) throw new ArgumentException("BodyTemplate is required.", nameof(bodyTemplate));

        TenantId = tenantId;
        Key = key.Trim();
        Channel = channel;
        Locale = locale.Trim();
        Subject = subject?.Trim();
        BodyTemplate = bodyTemplate;
        IsActive = true;
    }

    public void Update(string? subject, string bodyTemplate)
    {
        if (string.IsNullOrWhiteSpace(bodyTemplate)) throw new ArgumentException("BodyTemplate is required.", nameof(bodyTemplate));
        Subject = subject?.Trim();
        BodyTemplate = bodyTemplate;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Deactivate() { IsActive = false; UpdatedAtUtc = DateTime.UtcNow; }
    public void Activate() { IsActive = true; UpdatedAtUtc = DateTime.UtcNow; }
}
