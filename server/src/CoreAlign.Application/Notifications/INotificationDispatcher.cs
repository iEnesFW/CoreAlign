using CoreAlign.Application.Notifications.Providers;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Notifications;

public interface INotificationDispatcher
{
    Task<IReadOnlyList<NotificationSendResult>> DispatchAsync(NotificationRequest request, CancellationToken ct = default);
}

public sealed record NotificationRequest(
    Guid TenantId,
    Guid? UserId,
    Guid? CustomerId,
    string CategoryKey,
    string TemplateKey,
    string Locale,
    object Payload,
    IReadOnlyList<NotificationChannel>? ChannelsOverride = null,
    string? RecipientEmailOverride = null,
    string? RecipientPhoneOverride = null,
    string? RecipientDeviceTokenOverride = null,
    string? ReplyToOverride = null,
    IReadOnlyList<EmailAttachment>? Attachments = null,
    string? MarketingConsentPurpose = null);
