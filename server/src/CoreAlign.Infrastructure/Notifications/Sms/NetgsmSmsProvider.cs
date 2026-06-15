using CoreAlign.Application.Notifications.Providers;
using CoreAlign.Application.Providers;
using CoreAlign.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreAlign.Infrastructure.Notifications.Sms;

public sealed class NetgsmSmsProvider : ISmsProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly NetgsmSmsOptions _options;
    private readonly ILogger<NetgsmSmsProvider> _logger;

    public NetgsmSmsProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<NetgsmSmsOptions> options,
        ILogger<NetgsmSmsProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public string Name => "netgsm";
    public string DisplayName => "Netgsm SMS Provider";
    public ProviderCapabilities Capabilities => new(
        ProviderCapability.BulkSend | ProviderCapability.Webhook,
        new Dictionary<string, string> { ["region"] = "TR" });

    public async Task<NotificationSendResult> SendAsync(SmsMessage message, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (string.IsNullOrWhiteSpace(_options.UserCode) || string.IsNullOrWhiteSpace(_options.Password))
        {
            return NotificationSendResult.Fail("Netgsm credentials not configured");
        }

        try
        {
            using var client = _httpClientFactory.CreateClient(nameof(NetgsmSmsProvider));
            var url = $"{_options.ApiBaseUrl.TrimEnd('/')}/sms/send/get?usercode={Uri.EscapeDataString(_options.UserCode)}&password={Uri.EscapeDataString(_options.Password)}&gsmno={Uri.EscapeDataString(message.To)}&message={Uri.EscapeDataString(message.Body)}&msgheader={Uri.EscapeDataString(_options.MsgHeader)}";
            using var response = await client.GetAsync(url, ct).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode || body.StartsWith("20", StringComparison.OrdinalIgnoreCase) || body.StartsWith("30", StringComparison.OrdinalIgnoreCase))
            {
                return NotificationSendResult.Fail($"Netgsm error code: {body}");
            }

            return NotificationSendResult.Ok(body.Trim());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Netgsm send failed for {To}", message.To);
            return NotificationSendResult.Fail(ex.Message);
        }
    }
}
