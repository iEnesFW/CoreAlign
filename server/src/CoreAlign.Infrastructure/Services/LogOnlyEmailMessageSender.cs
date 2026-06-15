using CoreAlign.Application.Common.Email;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Infrastructure.Services;

public sealed class LogOnlyEmailMessageSender : IEmailSender
{
    private readonly ILogger<LogOnlyEmailMessageSender> _logger;

    public LogOnlyEmailMessageSender(ILogger<LogOnlyEmailMessageSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[Email log-only] to={Recipient} subject={Subject} bodyLength={Length} tenant={TenantId}",
            message.To, message.Subject, message.BodyHtml?.Length ?? 0, message.TenantId);
        return Task.CompletedTask;
    }
}
