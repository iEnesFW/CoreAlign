using System.Net;
using System.Net.Mail;
using CoreAlign.Application.Notifications.Providers;
using CoreAlign.Application.Providers;
using CoreAlign.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreAlign.Infrastructure.Notifications.Email;

public sealed class SmtpEmailProvider : IEmailProvider
{
    private readonly SmtpEmailOptions _options;
    private readonly ILogger<SmtpEmailProvider> _logger;

    public SmtpEmailProvider(IOptions<SmtpEmailOptions> options, ILogger<SmtpEmailProvider> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public string Name => "smtp";
    public string DisplayName => "SMTP Email Provider";
    public ProviderCapabilities Capabilities => new(
        ProviderCapability.BulkSend,
        new Dictionary<string, string> { ["transport"] = "smtp" });

    public async Task<NotificationSendResult> SendAsync(EmailMessage message, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (string.IsNullOrWhiteSpace(_options.Host))
        {
            _logger.LogWarning("SMTP host not configured; skipping send for {To}", message.To);
            return NotificationSendResult.Fail("SMTP host not configured");
        }

        try
        {
            using var client = new SmtpClient(_options.Host, _options.Port)
            {
                EnableSsl = _options.UseSsl,
                Credentials = string.IsNullOrEmpty(_options.Username)
                    ? CredentialCache.DefaultNetworkCredentials
                    : new NetworkCredential(_options.Username, _options.Password)
            };

            using var mail = new MailMessage
            {
                From = new MailAddress(message.From, message.FromName),
                Subject = message.Subject,
                Body = message.BodyHtml,
                IsBodyHtml = true
            };
            mail.To.Add(message.To);
            if (!string.IsNullOrWhiteSpace(message.ReplyTo))
            {
                mail.ReplyToList.Add(new MailAddress(message.ReplyTo));
            }

            await client.SendMailAsync(mail, ct).ConfigureAwait(false);
            return NotificationSendResult.Ok(Guid.NewGuid().ToString("N"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP send failed for {To}", message.To);
            return NotificationSendResult.Fail(ex.Message);
        }
    }
}
