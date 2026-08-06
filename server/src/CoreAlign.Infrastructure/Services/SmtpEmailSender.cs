using CoreAlign.Application.Common.Email;
using CoreAlign.Application.Notifications.Smtp;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Infrastructure.Notifications.Email;
using CoreAlign.Infrastructure.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace CoreAlign.Infrastructure.Services;

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly IOptions<EmailOptions> _options;
    private readonly ISmtpAccessTokenProvider _tokenProvider;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(
        IOptions<EmailOptions> options,
        ISmtpAccessTokenProvider tokenProvider,
        ILogger<SmtpEmailSender> logger)
    {
        _options = options;
        _tokenProvider = tokenProvider;
        _logger = logger;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var smtp = _options.Value.Smtp;
        if (string.IsNullOrWhiteSpace(smtp.Host)) throw new InvalidOperationException("Email:Smtp:Host is not configured.");
        if (string.IsNullOrWhiteSpace(smtp.FromAddress)) throw new InvalidOperationException("Email:Smtp:FromAddress is not configured.");
        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(
            string.IsNullOrWhiteSpace(smtp.FromName) ? smtp.FromAddress : smtp.FromName,
            smtp.FromAddress));
        mime.To.Add(MailboxAddress.Parse(message.To));
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
        if (message.Attachments is not null)
        {
            foreach (var attachment in message.Attachments)
            {
                if (ContentType.TryParse(attachment.ContentType, out var contentType))
                {
                    builder.Attachments.Add(attachment.FileName, attachment.Content, contentType);
                }
                else
                {
                    builder.Attachments.Add(attachment.FileName, attachment.Content);
                }
            }
        }
        mime.Body = builder.ToMessageBody();

        using var client = new SmtpClient();
        try
        {
            var socketOptions = smtp.UseSsl ? SecureSocketOptions.StartTlsWhenAvailable : SecureSocketOptions.None;
            await client.ConnectAsync(smtp.Host, smtp.Port, socketOptions, cancellationToken);
            await SmtpAuthenticator.AuthenticateAsync(client, ToCredentials(smtp), _tokenProvider, cancellationToken);
            await client.SendAsync(mime, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
            _logger.LogInformation(
                "SMTP email sent to {Recipient} subject={Subject} tenant={TenantId}",
                message.To, message.Subject, message.TenantId);
        }
        catch (SmtpCommandException ex)
        {
            _logger.LogWarning(ex, "SMTP send rejected for {Recipient}: {Reason}", message.To, ex.Message);
            throw new EmailSendFailedException(message.To, ex.Message);
        }
        catch (SmtpProtocolException ex)
        {
            _logger.LogWarning(ex, "SMTP protocol error for {Recipient}: {Reason}", message.To, ex.Message);
            throw new EmailSendFailedException(message.To, ex.Message);
        }
    }

    private static SmtpCredentials ToCredentials(EmailSmtpOptions smtp) => new(
        smtp.Host ?? string.Empty,
        smtp.Port,
        smtp.UseSsl,
        smtp.Username,
        smtp.Password,
        smtp.FromAddress,
        smtp.FromName,
        smtp.AuthMode,
        smtp.OAuthProvider,
        smtp.OAuthTenantId,
        smtp.OAuthClientId,
        smtp.OAuthClientSecret,
        smtp.OAuthRefreshToken,
        smtp.OAuthTokenEndpoint,
        smtp.OAuthScope);
}
