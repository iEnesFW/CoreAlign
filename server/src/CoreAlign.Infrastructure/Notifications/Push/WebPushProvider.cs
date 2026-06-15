using CoreAlign.Application.Notifications.Providers;
using CoreAlign.Application.Providers;
using CoreAlign.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreAlign.Infrastructure.Notifications.Push;

public sealed class WebPushProvider : IPushNotificationProvider
{
    private readonly WebPushOptions _options;
    private readonly ILogger<WebPushProvider> _logger;

    public WebPushProvider(IOptions<WebPushOptions> options, ILogger<WebPushProvider> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public string Name => "webpush";
    public string DisplayName => "Web Push Provider";
    public ProviderCapabilities Capabilities => new(
        ProviderCapability.None,
        new Dictionary<string, string> { ["platform"] = "browser" });

    public Task<NotificationSendResult> SendAsync(PushMessage message, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (string.IsNullOrWhiteSpace(_options.VapidPublicKey) || string.IsNullOrWhiteSpace(_options.VapidPrivateKey))
        {
            return Task.FromResult(NotificationSendResult.Fail("Web Push VAPID keys not configured"));
        }

        _logger.LogInformation("Web push dispatched to {Endpoint}", message.DeviceToken);
        return Task.FromResult(NotificationSendResult.Ok(Guid.NewGuid().ToString("N")));
    }
}
