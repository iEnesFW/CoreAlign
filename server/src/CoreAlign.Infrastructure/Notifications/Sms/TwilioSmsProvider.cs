using System.Net.Http.Headers;
using System.Text;
using CoreAlign.Application.Notifications.Providers;
using CoreAlign.Application.Providers;
using CoreAlign.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreAlign.Infrastructure.Notifications.Sms;

public sealed class TwilioSmsProvider : ISmsProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TwilioOptions _options;
    private readonly ILogger<TwilioSmsProvider> _logger;

    public TwilioSmsProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<TwilioOptions> options,
        ILogger<TwilioSmsProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public string Name => "twilio";
    public string DisplayName => "Twilio SMS Provider";
    public ProviderCapabilities Capabilities => new(
        ProviderCapability.BulkSend | ProviderCapability.Webhook | ProviderCapability.RealTimeStatus,
        new Dictionary<string, string> { ["region"] = "global" });

    public async Task<NotificationSendResult> SendAsync(SmsMessage message, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (string.IsNullOrWhiteSpace(_options.AccountSid) || string.IsNullOrWhiteSpace(_options.AuthToken))
        {
            return NotificationSendResult.Fail("Twilio credentials not configured");
        }

        try
        {
            using var client = _httpClientFactory.CreateClient(nameof(TwilioSmsProvider));
            client.BaseAddress = new Uri(_options.ApiBaseUrl.TrimEnd('/') + "/");
            var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.AccountSid}:{_options.AuthToken}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basic);

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["From"] = string.IsNullOrEmpty(message.From) ? _options.FromNumber : message.From,
                ["To"] = message.To,
                ["Body"] = message.Body
            });

            var url = $"Accounts/{_options.AccountSid}/Messages.json";
            using var response = await client.PostAsync(url, content, ct).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return NotificationSendResult.Fail($"Twilio HTTP {(int)response.StatusCode}: {body}");
            }
            return NotificationSendResult.Ok(ExtractSid(body));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Twilio send failed for {To}", message.To);
            return NotificationSendResult.Fail(ex.Message);
        }
    }

    private static string? ExtractSid(string responseJson)
    {
        const string token = "\"sid\":\"";
        var idx = responseJson.IndexOf(token, StringComparison.Ordinal);
        if (idx < 0) return null;
        var start = idx + token.Length;
        var end = responseJson.IndexOf('"', start);
        return end > start ? responseJson[start..end] : null;
    }
}
