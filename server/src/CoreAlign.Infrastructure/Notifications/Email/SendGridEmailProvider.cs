using System.Net.Http.Headers;
using System.Net.Http.Json;
using CoreAlign.Application.Notifications.Providers;
using CoreAlign.Application.Providers;
using CoreAlign.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreAlign.Infrastructure.Notifications.Email;

public sealed class SendGridEmailProvider : IEmailProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SendGridOptions _options;
    private readonly ILogger<SendGridEmailProvider> _logger;

    public SendGridEmailProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<SendGridOptions> options,
        ILogger<SendGridEmailProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public string Name => "sendgrid";
    public string DisplayName => "SendGrid Email Provider";
    public ProviderCapabilities Capabilities => new(
        ProviderCapability.BulkSend | ProviderCapability.Webhook,
        new Dictionary<string, string> { ["transport"] = "https" });

    public async Task<NotificationSendResult> SendAsync(EmailMessage message, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return NotificationSendResult.Fail("SendGrid ApiKey not configured");
        }

        try
        {
            using var client = _httpClientFactory.CreateClient(nameof(SendGridEmailProvider));
            client.BaseAddress = new Uri(_options.ApiBaseUrl.TrimEnd('/') + "/");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

            var payload = new
            {
                personalizations = new[] { new { to = new[] { new { email = message.To } } } },
                from = new { email = message.From, name = message.FromName },
                subject = message.Subject,
                content = new[]
                {
                    new { type = "text/plain", value = message.BodyText },
                    new { type = "text/html", value = message.BodyHtml }
                }
            };

            using var response = await client.PostAsJsonAsync("mail/send", payload, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                return NotificationSendResult.Fail($"SendGrid HTTP {(int)response.StatusCode}: {body}");
            }

            response.Headers.TryGetValues("X-Message-Id", out var msgIdValues);
            return NotificationSendResult.Ok(msgIdValues?.FirstOrDefault());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SendGrid send failed for {To}", message.To);
            return NotificationSendResult.Fail(ex.Message);
        }
    }
}
