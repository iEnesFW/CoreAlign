using CoreAlign.Application.GlassEnclosure.Services;
using CoreAlign.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Infrastructure.Services;

public class LogOnlyEmailSender : INotificationChannelSender
{
    private readonly ILogger<LogOnlyEmailSender> _logger;
    public LogOnlyEmailSender(ILogger<LogOnlyEmailSender> logger) => _logger = logger;
    public GlassNotificationChannel Channel => GlassNotificationChannel.Email;

    public Task<(string? ProviderMessageId, string? ErrorMessage)> SendAsync(
        string recipientAddress, string? subject, string body, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[Email simulated] to={Recipient} subject={Subject} bodyLength={Length}",
            recipientAddress, subject ?? "(no subject)", body.Length);
        return Task.FromResult<(string?, string?)>(($"simulated-email-{Guid.NewGuid():N}", null));
    }
}

public class LogOnlySmsSender : INotificationChannelSender
{
    private readonly ILogger<LogOnlySmsSender> _logger;
    public LogOnlySmsSender(ILogger<LogOnlySmsSender> logger) => _logger = logger;
    public GlassNotificationChannel Channel => GlassNotificationChannel.Sms;

    public Task<(string? ProviderMessageId, string? ErrorMessage)> SendAsync(
        string recipientAddress, string? subject, string body, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[SMS simulated] to={Recipient} bodyLength={Length}", recipientAddress, body.Length);
        return Task.FromResult<(string?, string?)>(($"simulated-sms-{Guid.NewGuid():N}", null));
    }
}

public class LogOnlyWhatsAppSender : INotificationChannelSender
{
    private readonly ILogger<LogOnlyWhatsAppSender> _logger;
    public LogOnlyWhatsAppSender(ILogger<LogOnlyWhatsAppSender> logger) => _logger = logger;
    public GlassNotificationChannel Channel => GlassNotificationChannel.WhatsApp;

    public Task<(string? ProviderMessageId, string? ErrorMessage)> SendAsync(
        string recipientAddress, string? subject, string body, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[WhatsApp simulated] to={Recipient} bodyLength={Length}", recipientAddress, body.Length);
        return Task.FromResult<(string?, string?)>(($"simulated-wa-{Guid.NewGuid():N}", null));
    }
}

public class InAppNotificationSender : INotificationChannelSender
{
    private readonly ILogger<InAppNotificationSender> _logger;
    public InAppNotificationSender(ILogger<InAppNotificationSender> logger) => _logger = logger;
    public GlassNotificationChannel Channel => GlassNotificationChannel.InApp;

    public Task<(string? ProviderMessageId, string? ErrorMessage)> SendAsync(
        string recipientAddress, string? subject, string body, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[InApp queued] to={Recipient} bodyLength={Length}", recipientAddress, body.Length);
        return Task.FromResult<(string?, string?)>(($"inapp-{Guid.NewGuid():N}", null));
    }
}
