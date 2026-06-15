namespace CoreAlign.Application.Notifications.Webhooks;

public interface INotificationStatusUpdater
{
    Task<NotificationStatusUpdateResult> UpdateFromWebhookAsync(
        Guid tenantId,
        string providerName,
        string rawBody,
        CancellationToken cancellationToken = default);
}

public sealed record NotificationStatusUpdateResult(
    bool MessageFound,
    bool StatusChanged,
    string? AppliedEventType,
    string? ProviderMessageId);
