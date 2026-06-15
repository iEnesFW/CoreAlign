using System.Net.Http.Headers;
using System.Net.Http.Json;
using CoreAlign.Application.Notifications.Providers;
using CoreAlign.Application.Providers;
using CoreAlign.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreAlign.Infrastructure.Notifications.Push;

public sealed class FcmPushProvider : IPushNotificationProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly FcmPushOptions _options;
    private readonly ILogger<FcmPushProvider> _logger;

    public FcmPushProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<FcmPushOptions> options,
        ILogger<FcmPushProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public string Name => "fcm";
    public string DisplayName => "Firebase Cloud Messaging";
    public ProviderCapabilities Capabilities => new(
        ProviderCapability.BulkSend | ProviderCapability.RealTimeStatus,
        new Dictionary<string, string> { ["platform"] = "android-ios" });

    public async Task<NotificationSendResult> SendAsync(PushMessage message, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (string.IsNullOrWhiteSpace(_options.ServerKey))
        {
            return NotificationSendResult.Fail("FCM ServerKey not configured");
        }

        try
        {
            using var client = _httpClientFactory.CreateClient(nameof(FcmPushProvider));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("key", _options.ServerKey);

            var payload = new
            {
                to = message.DeviceToken,
                notification = new { title = message.Title, body = message.Body },
                data = message.Data ?? new Dictionary<string, string>()
            };

            using var response = await client.PostAsJsonAsync(_options.ApiBaseUrl, payload, ct).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return NotificationSendResult.Fail($"FCM HTTP {(int)response.StatusCode}: {body}");
            }
            return NotificationSendResult.Ok(Guid.NewGuid().ToString("N"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FCM send failed for device {Device}", message.DeviceToken);
            return NotificationSendResult.Fail(ex.Message);
        }
    }
}
