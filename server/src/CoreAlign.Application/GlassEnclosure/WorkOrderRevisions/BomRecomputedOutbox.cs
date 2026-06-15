using System.Text.Json;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.GlassEnclosure.WorkOrderRevisions;

/// <summary>
/// Payload enqueued after a project BOM recompute so each downstream work-order revision is
/// processed in its own transaction by <see cref="BomRecomputedOutboxHandler"/>. Splitting fan-out
/// across outbox rows keeps RecomputeBOM within the single-aggregate-per-transaction boundary and
/// lets per-revision failures retry independently instead of rolling back the project recompute.
/// </summary>
public sealed record BomRecomputedOutboxPayload(
    Guid WorkOrderId,
    string SnapshotJson,
    decimal NewTotal,
    string Reason);

public interface IBomRecomputedOutbox
{
    Task EnqueueAsync(BomRecomputedOutboxPayload payload, CancellationToken cancellationToken = default);
}

public sealed class BomRecomputedOutbox : IBomRecomputedOutbox
{
    public const string MessageType = "GlassEnclosure.BomRecomputed";

    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IOutboxRepository _outbox;
    private readonly IOutboxSignal _signal;

    public BomRecomputedOutbox(IOutboxRepository outbox, IOutboxSignal signal)
    {
        _outbox = outbox;
        _signal = signal;
    }

    public async Task EnqueueAsync(BomRecomputedOutboxPayload payload, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        await _outbox.AddAsync(new OutboxMessage(MessageType, json), cancellationToken);
        _signal.MarkPending();
    }

    internal static BomRecomputedOutboxPayload? Deserialize(string payloadJson) =>
        JsonSerializer.Deserialize<BomRecomputedOutboxPayload>(payloadJson, JsonOptions);
}

public sealed class BomRecomputedOutboxHandler : IOutboxMessageHandler
{
    public string MessageType => BomRecomputedOutbox.MessageType;

    private readonly IWorkOrderRevisionService _revisionService;

    public BomRecomputedOutboxHandler(IWorkOrderRevisionService revisionService)
    {
        _revisionService = revisionService;
    }

    public async Task<OutboxHandlerResult> HandleAsync(string payloadJson, CancellationToken cancellationToken)
    {
        var payload = BomRecomputedOutbox.Deserialize(payloadJson);
        if (payload is null)
        {
            return OutboxHandlerResult.Failed("Payload deserialized to null.");
        }

        var decision = await _revisionService.CreateRevisionAsync(
            payload.WorkOrderId,
            payload.SnapshotJson,
            payload.NewTotal,
            payload.Reason,
            cancellationToken);

        return decision is null
            ? OutboxHandlerResult.Processed($"WorkOrder:{payload.WorkOrderId}:NoOp")
            : OutboxHandlerResult.Processed($"WorkOrder:{payload.WorkOrderId}:{decision.Status}");
    }
}
