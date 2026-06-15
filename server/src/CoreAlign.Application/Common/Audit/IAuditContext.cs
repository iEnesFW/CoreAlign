namespace CoreAlign.Application.Common.Audit;

/// <summary>
/// Per-request audit collector. Handlers and the EF interceptor push
/// field-level changes here; <see cref="Behaviors.AuditBehavior{TRequest,TResponse}"/>
/// drains the buffer after a successful handler invocation so the entries
/// can be persisted via the outbox alongside the transaction commit.
/// </summary>
public interface IAuditContext
{
    /// <summary>
    /// Records a single field change. Sensitive fields are redacted by
    /// <see cref="IAuditFieldRedactor"/> before the entry reaches the buffer.
    /// </summary>
    void Capture(Guid aggregateId, string aggregateType, string field, string? oldValue, string? newValue);

    /// <summary>
    /// Records a non-field-shaped change (e.g. <c>StatusTransition</c>,
    /// <c>BulkImport</c>) where the diff is described by free-form details.
    /// </summary>
    void CaptureCustom(Guid aggregateId, string aggregateType, string changeKind, string details);

    /// <summary>Snapshot of entries pending flush; safe to enumerate.</summary>
    IReadOnlyList<AuditEntry> PendingEntries { get; }

    /// <summary>Clears the buffer — invoked on handler failure to avoid leaking partial state.</summary>
    void Clear();
}

public sealed record AuditEntry(
    Guid AggregateId,
    string AggregateType,
    string ChangeKind,
    string? Field,
    string? OldValue,
    string? NewValue,
    DateTime CapturedAtUtc);
