using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Entities.Notifications;

public class NotificationPreference : TenantEntity
{
    public Guid UserId { get; private set; }
    public string CategoryKey { get; private set; } = string.Empty;
    public NotificationChannel Channel { get; private set; }
    public bool IsEnabled { get; private set; } = true;

    protected NotificationPreference() { }

    public NotificationPreference(Guid tenantId, Guid userId, string categoryKey, NotificationChannel channel, bool isEnabled)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (userId == Guid.Empty) throw new ArgumentException("UserId is required.", nameof(userId));
        if (string.IsNullOrWhiteSpace(categoryKey)) throw new ArgumentException("CategoryKey is required.", nameof(categoryKey));

        TenantId = tenantId;
        UserId = userId;
        CategoryKey = categoryKey.Trim();
        Channel = channel;
        IsEnabled = isEnabled;
    }

    public void Update(bool isEnabled)
    {
        IsEnabled = isEnabled;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
