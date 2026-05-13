using System.Security.Cryptography;
using System.Text;
using CoreAlign.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    public Task SendPasswordResetEmailAsync(string email, string resetToken, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Password reset email queued for {Email} (token hash prefix {TokenHashPrefix})",
            MaskEmail(email),
            ShortHash(resetToken));
        return Task.CompletedTask;
    }

    public Task SendEmailVerificationAsync(string email, string verificationToken, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Email verification queued for {Email} (token hash prefix {TokenHashPrefix})",
            MaskEmail(email),
            ShortHash(verificationToken));
        return Task.CompletedTask;
    }

    public Task SendDuplicateRegistrationNoticeAsync(string email, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Duplicate registration attempt notice queued for {Email}",
            MaskEmail(email));
        return Task.CompletedTask;
    }

    private static string ShortHash(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes, 0, 4);
    }

    private static string MaskEmail(string email)
    {
        if (string.IsNullOrEmpty(email)) return "<empty>";
        var at = email.IndexOf('@');
        if (at <= 1) return "*@" + email[(at + 1)..];
        return email[0] + new string('*', at - 1) + email[at..];
    }
}
