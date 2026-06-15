using System.Net.Http.Headers;
using System.Net.Http.Json;
using CoreAlign.Application.Notifications.Providers;
using CoreAlign.Application.Providers;
using CoreAlign.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreAlign.Infrastructure.Notifications.WhatsApp;

public sealed class MetaWhatsAppProvider : IWhatsAppProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MetaWhatsAppOptions _options;
    private readonly ILogger<MetaWhatsAppProvider> _logger;

    public MetaWhatsAppProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<MetaWhatsAppOptions> options,
        ILogger<MetaWhatsAppProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public string Name => "meta";
    public string DisplayName => "Meta WhatsApp Business";
    public ProviderCapabilities Capabilities => new(
        ProviderCapability.BulkSend | ProviderCapability.Webhook | ProviderCapability.RealTimeStatus,
        new Dictionary<string, string> { ["channel"] = "whatsapp" });

    public async Task<NotificationSendResult> SendAsync(WhatsAppMessage message, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (string.IsNullOrWhiteSpace(_options.AccessToken) || string.IsNullOrWhiteSpace(_options.PhoneNumberId))
        {
            return NotificationSendResult.Fail("Meta WhatsApp credentials not configured");
        }

        try
        {
            using var client = _httpClientFactory.CreateClient(nameof(MetaWhatsAppProvider));
            client.BaseAddress = new Uri(_options.ApiBaseUrl.TrimEnd('/') + "/");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.AccessToken);

            var payload = new
            {
                messaging_product = "whatsapp",
                to = message.To,
                type = "template",
                template = new
                {
                    name = message.TemplateName,
                    language = new { code = message.Locale }
                }
            };

            var url = $"{_options.PhoneNumberId}/messages";
            using var response = await client.PostAsJsonAsync(url, payload, ct).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return NotificationSendResult.Fail($"Meta WhatsApp HTTP {(int)response.StatusCode}: {body}");
            }
            return NotificationSendResult.Ok(Guid.NewGuid().ToString("N"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Meta WhatsApp send failed for {To}", message.To);
            return NotificationSendResult.Fail(ex.Message);
        }
    }
}
