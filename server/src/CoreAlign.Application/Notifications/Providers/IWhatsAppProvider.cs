using CoreAlign.Application.Providers;

namespace CoreAlign.Application.Notifications.Providers;

public interface IWhatsAppProvider : IExternalProvider
{
    Task<NotificationSendResult> SendAsync(WhatsAppMessage message, CancellationToken ct);
}
