namespace CoreAlign.Application.Common.Outbox;

/// <summary>
/// Per-request flag set when an outbox message is enqueued, so the drain
/// behavior can skip the no-op case (most requests enqueue nothing) without a
/// database round-trip.
/// </summary>
public interface IOutboxSignal
{
    bool HasPending { get; }
    void MarkPending();
}

public sealed class OutboxSignal : IOutboxSignal
{
    public bool HasPending { get; private set; }
    public void MarkPending() => HasPending = true;
}
