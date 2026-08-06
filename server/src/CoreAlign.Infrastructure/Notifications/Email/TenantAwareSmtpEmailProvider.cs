using System.Diagnostics;
using CoreAlign.Application.Notifications.Providers;
using CoreAlign.Application.Notifications.Smtp;
using CoreAlign.Application.Providers;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace CoreAlign.Infrastructure.Notifications.Email;

public sealed class TenantAwareSmtpEmailProvider : IEmailProvider
{
    private readonly ITenantContext _tenantContext;
    private readonly ITenantProviderConfigResolver _resolver;
    private readonly IProviderCredentialProtector _protector;
    private readonly ISmtpAccessTokenProvider _tokenProvider;
    private readonly SmtpEmailOptions _globalOptions;
    private readonly ILogger<TenantAwareSmtpEmailProvider> _logger;

    public TenantAwareSmtpEmailProvider(
        ITenantContext tenantContext,
        ITenantProviderConfigResolver resolver,
        IProviderCredentialProtector protector,
        ISmtpAccessTokenProvider tokenProvider,
        IOptions<SmtpEmailOptions> globalOptions,
        ILogger<TenantAwareSmtpEmailProvider> logger)
    {
        _tenantContext = tenantContext;
        _resolver = resolver;
        _protector = protector;
        _tokenProvider = tokenProvider;
        _globalOptions = globalOptions.Value;
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

        var credentials = await ResolveCredentialsAsync(_tenantContext.CurrentTenantId, ct).ConfigureAwait(false);
        if (credentials is null)
        {
            _logger.LogWarning("SMTP host not configured for tenant {TenantId}; skipping send to {To}", _tenantContext.CurrentTenantId, message.To);
            return NotificationSendResult.Fail("SMTP host not configured");
        }

        var fromAddress = FirstNonEmpty(credentials.FromAddress, message.From);
        if (string.IsNullOrWhiteSpace(fromAddress))
        {
            return NotificationSendResult.Fail("SMTP sender address not configured");
        }

        try
        {
            var mime = BuildMessage(message, credentials, fromAddress);
            using var client = new SmtpClient();
            var socketOptions = credentials.UseSsl ? SecureSocketOptions.StartTlsWhenAvailable : SecureSocketOptions.None;
            await client.ConnectAsync(credentials.Host, credentials.Port, socketOptions, ct).ConfigureAwait(false);
            await SmtpAuthenticator.AuthenticateAsync(client, credentials, _tokenProvider, ct).ConfigureAwait(false);
            await client.SendAsync(mime, ct).ConfigureAwait(false);
            await client.DisconnectAsync(true, ct).ConfigureAwait(false);
            return NotificationSendResult.Ok(mime.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP send failed for {To}", message.To);
            return NotificationSendResult.Fail(ex.Message);
        }
    }

    public async Task<ProviderHealthCheckResult> CheckHealthAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var credentials = await ResolveCredentialsAsync(tenantId, cancellationToken).ConfigureAwait(false);
        if (credentials is null)
        {
            return ProviderHealthCheckResult.Unhealthy(Name, "SMTP host not configured", stopwatch.Elapsed);
        }

        try
        {
            using var client = new SmtpClient();
            var socketOptions = credentials.UseSsl ? SecureSocketOptions.StartTlsWhenAvailable : SecureSocketOptions.None;
            await client.ConnectAsync(credentials.Host, credentials.Port, socketOptions, cancellationToken).ConfigureAwait(false);
            await SmtpAuthenticator.AuthenticateAsync(client, credentials, _tokenProvider, cancellationToken).ConfigureAwait(false);
            await client.DisconnectAsync(true, cancellationToken).ConfigureAwait(false);
            return ProviderHealthCheckResult.Healthy(Name, stopwatch.Elapsed, $"{credentials.Host}:{credentials.Port}");
        }
        catch (Exception ex)
        {
            return ProviderHealthCheckResult.Unhealthy(Name, ex.Message, stopwatch.Elapsed, $"{credentials.Host}:{credentials.Port}");
        }
    }

    private async Task<SmtpCredentials?> ResolveCredentialsAsync(Guid? tenantId, CancellationToken ct)
    {
        if (tenantId is Guid id && id != Guid.Empty)
        {
            var encrypted = await _resolver.GetEncryptedCredentialsAsync(id, ProviderCategory.Email, Name, ct).ConfigureAwait(false);
            var tenantCredentials = _protector.UnprotectAs<SmtpCredentials>(id, ProviderCategory.Email, encrypted);
            if (tenantCredentials is not null && !string.IsNullOrWhiteSpace(tenantCredentials.Host))
            {
                return tenantCredentials;
            }
        }

        if (!string.IsNullOrWhiteSpace(_globalOptions.Host))
        {
            return new SmtpCredentials(
                _globalOptions.Host,
                _globalOptions.Port,
                _globalOptions.UseSsl,
                _globalOptions.Username,
                _globalOptions.Password,
                _globalOptions.FromAddress,
                _globalOptions.FromName,
                _globalOptions.AuthMode,
                _globalOptions.OAuthProvider,
                _globalOptions.OAuthTenantId,
                _globalOptions.OAuthClientId,
                _globalOptions.OAuthClientSecret,
                _globalOptions.OAuthRefreshToken,
                _globalOptions.OAuthTokenEndpoint,
                _globalOptions.OAuthScope);
        }

        return null;
    }

    private static MimeMessage BuildMessage(EmailMessage message, SmtpCredentials credentials, string fromAddress)
    {
        var mime = new MimeMessage();
        var fromName = FirstNonEmpty(credentials.FromName, message.FromName, fromAddress);
        mime.From.Add(new MailboxAddress(fromName, fromAddress));
        mime.To.Add(MailboxAddress.Parse(message.To));
        AddRecipients(mime.Cc, message.Cc);
        AddRecipients(mime.Bcc, message.Bcc);
        if (!string.IsNullOrWhiteSpace(message.ReplyTo))
        {
            mime.ReplyTo.Add(MailboxAddress.Parse(message.ReplyTo));
        }
        mime.Subject = message.Subject ?? string.Empty;

        var builder = new BodyBuilder
        {
            HtmlBody = message.BodyHtml,
            TextBody = message.BodyText,
        };
        foreach (var attachment in message.Attachments ?? Array.Empty<EmailAttachment>())
        {
            builder.Attachments.Add(attachment.FileName, attachment.Content, ContentType.Parse(attachment.ContentType));
        }
        mime.Body = builder.ToMessageBody();
        return mime;
    }

    private static void AddRecipients(InternetAddressList list, IReadOnlyList<string>? addresses)
    {
        if (addresses is null) return;
        foreach (var address in addresses)
        {
            if (string.IsNullOrWhiteSpace(address)) continue;
            list.Add(MailboxAddress.Parse(address));
        }
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value)) return value!;
        }
        return string.Empty;
    }
}
