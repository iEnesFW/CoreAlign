using System.Net.Http.Headers;
using System.Text;
using CoreAlign.Application.Notifications.Providers;
using CoreAlign.Application.Providers;
using CoreAlign.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreAlign.Infrastructure.Notifications.WhatsApp;

public sealed class TwilioWhatsAppProvider : IWhatsAppProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TwilioOptions _options;
    private readonly ILogger<TwilioWhatsAppProvider> _logger;

    public TwilioWhatsAppProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<TwilioOptions> options,
        ILogger<TwilioWhatsAppProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public string Name => "twilio-whatsapp";
    public string DisplayName => "Twilio WhatsApp";
    public ProviderCapabilities Capabilities => new(
        ProviderCapability.BulkSend | ProviderCapability.Webhook,
        new Dictionary<string, string> { ["channel"] = "whatsapp" });

    public async Task<NotificationSendResult> SendAsync(WhatsAppMessage message, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (string.IsNullOrWhiteSpace(_options.AccountSid) || string.IsNullOrWhiteSpace(_options.AuthToken))
        {
            return NotificationSendResult.Fail("Twilio credentials not configured");
        }

        try
        {
            using var client = _httpClientFactory.CreateClient(nameof(TwilioWhatsAppProvider));
            client.BaseAddress = new Uri(_options.ApiBaseUrl.TrimEnd('/') + "/");
            var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.AccountSid}:{_options.AuthToken}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basic);

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["From"] = $"whatsapp:{(string.IsNullOrEmpty(message.From) ? _options.FromNumber : message.From)}",
                ["To"] = $"whatsapp:{message.To}",
                ["Body"] = message.Body
            });

            var url = $"Accounts/{_options.AccountSid}/Messages.json";
            using var response = await client.PostAsync(url, content, ct).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return NotificationSendResult.Fail($"Twilio WhatsApp HTTP {(int)response.StatusCode}: {body}");
            }
            return NotificationSendResult.Ok(Guid.NewGuid().ToString("N"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Twilio WhatsApp send failed for {To}", message.To);
            return NotificationSendResult.Fail(ex.Message);
        }
    }
}
