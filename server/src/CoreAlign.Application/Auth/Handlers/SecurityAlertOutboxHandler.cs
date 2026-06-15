using CoreAlign.Application.Auth.DTOs;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Auth.Handlers;

public sealed class SecurityAlertOutboxHandler : IOutboxMessageHandler
{
    public string MessageType => SecurityAlertOutbox.MessageType;

    private readonly IEmailService _email;

    public SecurityAlertOutboxHandler(IEmailService email)
    {
        _email = email;
    }

    public async Task<OutboxHandlerResult> HandleAsync(string payloadJson, CancellationToken cancellationToken)
    {
        var payload = SecurityAlertOutbox.Deserialize<SecurityAlertEmailMessage>(payloadJson);
        if (payload is null)
        {
            return OutboxHandlerResult.Failed("Payload deserialized to null.");
        }

        await _email.SendSecurityAlertAsync(
            new SecurityAlertEmailPayload(payload.UserId, payload.AlertType, payload.OccurredAtUtc, payload.IpAddress, payload.UserAgent),
            cancellationToken);

        return OutboxHandlerResult.Processed($"Sent:{payload.AlertType}");
    }
}
