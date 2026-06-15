using CoreAlign.Application.Providers;

namespace CoreAlign.Application.Notifications.Providers;

public interface IPushNotificationProvider : IExternalProvider
{
    Task<NotificationSendResult> SendAsync(PushMessage message, CancellationToken ct);
}
