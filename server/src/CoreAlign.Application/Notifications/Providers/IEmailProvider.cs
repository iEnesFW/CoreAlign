using CoreAlign.Application.Providers;

namespace CoreAlign.Application.Notifications.Providers;

public interface IEmailProvider : IExternalProvider
{
    Task<NotificationSendResult> SendAsync(EmailMessage message, CancellationToken ct);
}
