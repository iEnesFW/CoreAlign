namespace CoreAlign.Application.Common.Outbox;

/// <summary>
/// Type-specific handler for an outbox message. Implementations are looked up by
/// <see cref="MessageType"/> and dispatched by <see cref="OutboxProcessor"/>.
/// The processor handles persistence, status transitions and retries; the
/// handler only needs to deserialize the payload, do its side-effect work, and
/// return a structured result.
/// </summary>
public interface IOutboxMessageHandler
{
    /// <summary>The <see cref="Domain.Entities.OutboxMessage.Type"/> this handler accepts.</summary>
    string MessageType { get; }

    Task<OutboxHandlerResult> HandleAsync(string payloadJson, CancellationToken cancellationToken);
}

public enum OutboxHandlerOutcome
{
    Processed,
    Deferred,
    Failed,
}

public sealed record OutboxHandlerResult(OutboxHandlerOutcome Outcome, string ResultOrError)
{
    public static OutboxHandlerResult Processed(string result) => new(OutboxHandlerOutcome.Processed, result);
    public static OutboxHandlerResult Deferred(string reason) => new(OutboxHandlerOutcome.Deferred, reason);
    public static OutboxHandlerResult Failed(string error) => new(OutboxHandlerOutcome.Failed, error);
}
