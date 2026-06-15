using System.Text.Json;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Common.Outbox;

public sealed record SecurityAlertEmailMessage(
    Guid UserId,
    string AlertType,
    DateTime OccurredAtUtc,
    string? IpAddress,
    string? UserAgent);

public interface ISecurityAlertOutbox
{
    Task EnqueueRefreshTokenReuseAsync(Guid userId, DateTime occurredAtUtc, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default);
}

public sealed class SecurityAlertOutbox : ISecurityAlertOutbox
{
    public const string MessageType = "SecurityAlertEmail";
    public const string AlertTypeRefreshTokenReuse = "RefreshTokenReuse";

    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IOutboxRepository _outbox;
    private readonly IOutboxSignal _signal;

    public SecurityAlertOutbox(IOutboxRepository outbox, IOutboxSignal signal)
    {
        _outbox = outbox;
        _signal = signal;
    }

    public async Task EnqueueRefreshTokenReuseAsync(Guid userId, DateTime occurredAtUtc, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default)
    {
        var payload = new SecurityAlertEmailMessage(userId, AlertTypeRefreshTokenReuse, occurredAtUtc, ipAddress, userAgent);
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        await _outbox.AddAsync(new OutboxMessage(MessageType, json), cancellationToken);
        _signal.MarkPending();
    }

    internal static T? Deserialize<T>(string payloadJson) where T : class =>
        JsonSerializer.Deserialize<T>(payloadJson, JsonOptions);
}
