using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities;

public class UserNotificationPreference : TenantEntity
{
    public Guid UserId { get; private set; }
    public string NotificationKind { get; private set; } = string.Empty;
    public bool EmailEnabled { get; private set; } = true;
    public bool InAppEnabled { get; private set; } = true;

    protected UserNotificationPreference() { }

    public UserNotificationPreference(Guid userId, string notificationKind, bool emailEnabled, bool inAppEnabled)
    {
        if (userId == Guid.Empty) throw new ArgumentException("UserId is required.", nameof(userId));
        if (string.IsNullOrWhiteSpace(notificationKind)) throw new ArgumentException("NotificationKind is required.", nameof(notificationKind));
        UserId = userId;
        NotificationKind = notificationKind.Trim();
        EmailEnabled = emailEnabled;
        InAppEnabled = inAppEnabled;
    }

    public void Update(bool emailEnabled, bool inAppEnabled)
    {
        EmailEnabled = emailEnabled;
        InAppEnabled = inAppEnabled;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
