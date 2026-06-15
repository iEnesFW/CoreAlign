namespace CoreAlign.Application.Common.Audit;

/// <summary>
/// Scoped <see cref="IAuditContext"/> implementation backed by a lock-guarded
/// list. The collection is short-lived (one HTTP request / one MediatR pipeline)
/// so a plain lock is cheaper than concurrent collections and gives us a stable
/// snapshot semantics for <see cref="PendingEntries"/>.
/// </summary>
public sealed class AuditContext : IAuditContext
{
    private readonly List<AuditEntry> _entries = new();
    private readonly Lock _gate = new();
    private readonly IAuditFieldRedactor _redactor;

    public AuditContext(IAuditFieldRedactor redactor)
    {
        _redactor = redactor;
    }

    public IReadOnlyList<AuditEntry> PendingEntries
    {
        get
        {
            lock (_gate)
            {
                return _entries.ToArray();
            }
        }
    }

    public void Capture(Guid aggregateId, string aggregateType, string field, string? oldValue, string? newValue)
    {
        if (aggregateId == Guid.Empty) throw new ArgumentException("AggregateId is required", nameof(aggregateId));
        if (string.IsNullOrWhiteSpace(aggregateType)) throw new ArgumentException("AggregateType is required", nameof(aggregateType));
        if (string.IsNullOrWhiteSpace(field)) throw new ArgumentException("Field is required", nameof(field));

        var redactedOld = _redactor.Redact(field, oldValue);
        var redactedNew = _redactor.Redact(field, newValue);

        if (string.Equals(redactedOld, redactedNew, StringComparison.Ordinal))
        {
            return;
        }

        var entry = new AuditEntry(
            aggregateId,
            aggregateType,
            ChangeKind: "FieldUpdate",
            Field: field,
            OldValue: redactedOld,
            NewValue: redactedNew,
            CapturedAtUtc: DateTime.UtcNow);

        lock (_gate)
        {
            _entries.Add(entry);
        }
    }

    public void CaptureCustom(Guid aggregateId, string aggregateType, string changeKind, string details)
    {
        if (aggregateId == Guid.Empty) throw new ArgumentException("AggregateId is required", nameof(aggregateId));
        if (string.IsNullOrWhiteSpace(aggregateType)) throw new ArgumentException("AggregateType is required", nameof(aggregateType));
        if (string.IsNullOrWhiteSpace(changeKind)) throw new ArgumentException("ChangeKind is required", nameof(changeKind));

        var entry = new AuditEntry(
            aggregateId,
            aggregateType,
            ChangeKind: changeKind,
            Field: null,
            OldValue: null,
            NewValue: details,
            CapturedAtUtc: DateTime.UtcNow);

        lock (_gate)
        {
            _entries.Add(entry);
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            _entries.Clear();
        }
    }
}
