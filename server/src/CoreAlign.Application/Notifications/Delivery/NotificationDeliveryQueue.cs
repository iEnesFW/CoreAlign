using System.Text.Json;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace CoreAlign.Application.Notifications.Delivery;

public interface INotificationDeliveryQueue
{
    Task EnqueueChannelSendAsync(NotificationChannelSendPayload payload, CancellationToken cancellationToken = default);
}

public sealed class NotificationDeliveryQueue : INotificationDeliveryQueue
{
    public const string MessageType = "NotificationChannelSend";

    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IOutboxRepository _outbox;
    private readonly IOutboxSignal _signal;
    private readonly NotificationDeliveryOptions _options;

    public NotificationDeliveryQueue(
        IOutboxRepository outbox,
        IOutboxSignal signal,
        IOptions<NotificationDeliveryOptions> options)
    {
        _outbox = outbox;
        _signal = signal;
        _options = options.Value;
    }

    public async Task EnqueueChannelSendAsync(NotificationChannelSendPayload payload, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        await _outbox.AddAsync(new OutboxMessage(MessageType, json, _options.MaxAttempts), cancellationToken).ConfigureAwait(false);
        _signal.MarkPending();
    }

    internal static NotificationChannelSendPayload? Deserialize(string payloadJson) =>
        JsonSerializer.Deserialize<NotificationChannelSendPayload>(payloadJson, JsonOptions);
}
