using System.Net;
using CoreAlign.Application.Auth.DTOs;
using CoreAlign.Application.Common.Email;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreAlign.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IEmailSender _sender;
    private readonly IOptions<EmailOptions> _options;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IEmailSender sender, IOptions<EmailOptions> options, ILogger<EmailService> logger)
    {
        _sender = sender;
        _options = options;
        _logger = logger;
    }

    public Task SendPasswordResetEmailAsync(string email, string resetToken, CancellationToken cancellationToken = default)
    {
        var link = BuildLink("/reset-password", resetToken);
        var body = Wrap(
            "Şifre sıfırlama",
            "<p>Hesabınız için bir şifre sıfırlama talebi aldık. Yeni şifrenizi belirlemek için aşağıdaki bağlantıya tıklayın. " +
            "Bu bağlantı 1 saat boyunca geçerlidir.</p>" +
            LinkOrToken(link, "Şifremi sıfırla", resetToken) +
            "<p>Bu talebi siz yapmadıysanız bu e-postayı yok sayabilirsiniz.</p>");
        return TrySendAsync(email, "Şifre sıfırlama talebi", body, cancellationToken);
    }

    public Task SendEmailVerificationAsync(string email, string verificationToken, CancellationToken cancellationToken = default)
    {
        var link = BuildLink("/verify-email", verificationToken);
        var body = Wrap(
            "E-posta doğrulama",
            "<p>Kaydınızı tamamlamak için e-posta adresinizi doğrulayın.</p>" +
            LinkOrToken(link, "E-postamı doğrula", verificationToken));
        return TrySendAsync(email, "E-posta adresinizi doğrulayın", body, cancellationToken);
    }

    public Task SendDuplicateRegistrationNoticeAsync(string email, CancellationToken cancellationToken = default)
    {
        var body = Wrap(
            "Kayıt denemesi",
            "<p>Bu e-posta adresiyle zaten bir hesap bulunuyor. Yeni bir hesap oluşturmayı denediyseniz, " +
            "mevcut hesabınızla giriş yapabilir ya da şifrenizi sıfırlayabilirsiniz.</p>" +
            "<p>Bu işlemi siz yapmadıysanız herhangi bir şey yapmanıza gerek yoktur.</p>");
        return TrySendAsync(email, "Hesap kayıt denemesi", body, cancellationToken);
    }

    public Task SendSecurityAlertAsync(object payload, CancellationToken cancellationToken = default)
    {
        string inner;
        string? recipient = null;
        if (payload is SecurityAlertEmailPayload alert)
        {
            recipient = alert.Email;
            inner =
                $"<p><strong>Olay:</strong> {Enc(alert.AlertType)}</p>" +
                $"<p><strong>Zaman:</strong> {alert.OccurredAtUtc:yyyy-MM-dd HH:mm} UTC</p>" +
                $"<p><strong>IP:</strong> {Enc(alert.IpAddress)}</p>" +
                $"<p><strong>Cihaz:</strong> {Enc(alert.UserAgent)}</p>";
        }
        else
        {
            inner = "<p>Hesabınızda güvenlikle ilgili bir olay tespit edildi.</p>";
        }

        if (string.IsNullOrWhiteSpace(recipient))
        {
            _logger.LogWarning("Security alert email has no recipient address; not dispatched (payload type {PayloadType}).", payload?.GetType().Name ?? "<null>");
            return Task.CompletedTask;
        }

        var body = Wrap(
            "Güvenlik uyarısı",
            inner + "<p>Bu işlemi siz yapmadıysanız lütfen hemen şifrenizi değiştirin.</p>");
        return TrySendAsync(recipient, "Güvenlik uyarısı", body, cancellationToken);
    }

    public Task SendInvoiceIssuedAsync(string email, string invoiceNumber, string customerName, decimal total, string currency, CancellationToken cancellationToken = default)
    {
        var body = Wrap(
            "Faturanız hazır",
            $"<p>Sayın {Enc(customerName)},</p>" +
            $"<p><strong>{Enc(invoiceNumber)}</strong> numaralı faturanız düzenlenmiştir.</p>" +
            $"<p><strong>Tutar:</strong> {total:N2} {Enc(currency)}</p>");
        return TrySendAsync(email, $"Fatura {invoiceNumber}", body, cancellationToken);
    }

    public Task SendOrderCommentPostedAsync(string email, string authorPersona, string body, CancellationToken cancellationToken = default)
    {
        var html = Wrap(
            "Siparişinize yeni yorum",
            $"<p><strong>{Enc(authorPersona)}</strong> siparişinize bir yorum ekledi:</p>" +
            $"<blockquote style=\"border-left:3px solid #e2e8f0;margin:0;padding:8px 16px;color:#334155\">{Enc(body)}</blockquote>");
        return TrySendAsync(email, "Siparişinize yeni bir yorum eklendi", html, cancellationToken);
    }

    public Task SendDealerOrderPendingApprovalAsync(string email, string dealerName, int lineCount, decimal total, string currency, CancellationToken cancellationToken = default)
    {
        var body = Wrap(
            "Onay bekleyen bayi siparişi",
            $"<p><strong>{Enc(dealerName)}</strong> bayisinden onay bekleyen bir sipariş var.</p>" +
            $"<p><strong>Kalem sayısı:</strong> {lineCount}</p>" +
            $"<p><strong>Toplam:</strong> {total:N2} {Enc(currency)}</p>");
        return TrySendAsync(email, "Onay bekleyen bayi siparişi", body, cancellationToken);
    }

    private async Task TrySendAsync(string recipient, string subject, string bodyHtml, CancellationToken cancellationToken)
    {
        var options = _options.Value;
        if (!string.Equals(options.Provider, "Smtp", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(options.Smtp.Host))
        {
            _logger.LogInformation(
                "Email '{Subject}' to {Recipient} not dispatched — email provider '{Provider}' is not configured for delivery.",
                subject, MaskEmail(recipient), options.Provider);
            return;
        }

        try
        {
            await _sender.SendAsync(
                new EmailMessage(recipient, subject, bodyHtml, null, null, Guid.Empty),
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // WHY: auth flows (forgot-password/register) return a fixed response for account-enumeration
            // protection — a delivery failure must never surface as a 5xx nor reveal which addresses exist.
            _logger.LogWarning(ex, "Email '{Subject}' to {Recipient} could not be delivered.", subject, MaskEmail(recipient));
        }
    }

    private string? BuildLink(string relativePath, string token)
    {
        var baseUrl = _options.Value.AppBaseUrl;
        if (string.IsNullOrWhiteSpace(baseUrl)) return null;
        return $"{baseUrl.TrimEnd('/')}{relativePath}?token={Uri.EscapeDataString(token)}";
    }

    private static string LinkOrToken(string? link, string label, string token) =>
        link is null
            ? $"<p>Doğrulama kodunuz: <code>{Enc(token)}</code></p>"
            : $"<p><a href=\"{Enc(link)}\" style=\"display:inline-block;background:#4f46e5;color:#ffffff;padding:12px 20px;border-radius:8px;text-decoration:none\">{Enc(label)}</a></p>";

    private static string Wrap(string heading, string innerHtml) =>
        "<div style=\"font-family:Arial,Helvetica,sans-serif;max-width:560px;margin:0 auto;color:#0f172a\">" +
        $"<h2 style=\"color:#4f46e5\">{Enc(heading)}</h2>{innerHtml}" +
        "<hr style=\"border:none;border-top:1px solid #e2e8f0;margin:24px 0\"/>" +
        "<p style=\"font-size:12px;color:#64748b\">CoreAlign</p></div>";

    private static string Enc(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

    private static string MaskEmail(string email)
    {
        if (string.IsNullOrEmpty(email)) return "<empty>";
        var at = email.IndexOf('@');
        if (at <= 1) return "*@" + email[(at + 1)..];
        return email[0] + new string('*', at - 1) + email[at..];
    }
}
