namespace CoreAlign.Domain.Enums;

/// <summary>
/// Lifecycle of a transactional-outbox message. Pending rows are drained after
/// the producing transaction commits; Deferred rows are blocked on a fixable
/// condition (closed period, unmapped account) and can be replayed; Failed rows
/// hit an unexpected error and need attention.
/// </summary>
public enum OutboxStatus
{
    Pending = 0,
    Processed = 1,
    Deferred = 2,
    Failed = 3,
}
