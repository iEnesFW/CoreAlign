using System.Text.Json;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Billing;

/// <summary>
/// Payload persisted onto the outbox after a subscription order is marked Paid.
/// Drained post-commit to provision <see cref="TenantModule"/> rows and fan
/// admin notifications out, so payment marking is never blocked by side effects.
/// </summary>
public sealed record SubscriptionActivatedPayload(Guid OrderId, Guid TenantId);

public interface ISubscriptionActivatedOutbox
{
    Task EnqueueAsync(SubscriptionActivatedPayload payload, CancellationToken cancellationToken = default);
}

public sealed class SubscriptionActivatedOutbox : ISubscriptionActivatedOutbox
{
    public const string MessageType = "SubscriptionActivated";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IOutboxRepository _outbox;
    private readonly IOutboxSignal _signal;

    public SubscriptionActivatedOutbox(IOutboxRepository outbox, IOutboxSignal signal)
    {
        _outbox = outbox;
        _signal = signal;
    }

    public async Task EnqueueAsync(SubscriptionActivatedPayload payload, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        await _outbox.AddAsync(new OutboxMessage(MessageType, json), cancellationToken);
        _signal.MarkPending();
    }

    internal static SubscriptionActivatedPayload? Deserialize(string json) =>
        JsonSerializer.Deserialize<SubscriptionActivatedPayload>(json, JsonOptions);
}
