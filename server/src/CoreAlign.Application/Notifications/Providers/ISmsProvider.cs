using CoreAlign.Application.Providers;

namespace CoreAlign.Application.Notifications.Providers;

public interface ISmsProvider : IExternalProvider
{
    Task<NotificationSendResult> SendAsync(SmsMessage message, CancellationToken ct);
}
