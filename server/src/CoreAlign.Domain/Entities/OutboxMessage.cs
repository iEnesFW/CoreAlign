using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Entities;

/// <summary>
/// Transactional-outbox row. Producers append one within the same unit of work
/// as the business change (atomic commit); a post-commit drainer processes it
/// exactly once. Decouples side effects (GL posting) from the originating
/// transaction so a posting failure never rolls back the business action.
/// </summary>
public class OutboxMessage : TenantEntity
{
    public string Type { get; private set; } = string.Empty;
    public string PayloadJson { get; private set; } = string.Empty;
    public OutboxStatus Status { get; private set; } = OutboxStatus.Pending;
    public int Attempts { get; private set; }
    public string? Result { get; private set; }
    public string? LastError { get; private set; }
    public DateTime? ProcessedAtUtc { get; private set; }

    protected OutboxMessage() { }

    public OutboxMessage(string type, string payloadJson)
    {
        if (string.IsNullOrWhiteSpace(type)) throw new ArgumentException("Type is required.", nameof(type));
        if (string.IsNullOrWhiteSpace(payloadJson)) throw new ArgumentException("Payload is required.", nameof(payloadJson));
        Type = type.Trim();
        PayloadJson = payloadJson;
    }

    public void MarkProcessed(string result)
    {
        Status = OutboxStatus.Processed;
        Result = result;
        LastError = null;
        ProcessedAtUtc = DateTime.UtcNow;
        Attempts++;
        UpdatedAtUtc = ProcessedAtUtc.Value;
    }

    /// <summary>Blocked on a fixable condition (closed period / unmapped account) — replayable.</summary>
    public void MarkDeferred(string reason)
    {
        Status = OutboxStatus.Deferred;
        LastError = reason;
        Attempts++;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkFailed(string error)
    {
        Status = OutboxStatus.Failed;
        LastError = error;
        Attempts++;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Return a Deferred/Failed row to the Pending queue for another drain pass.</summary>
    public void Requeue()
    {
        Status = OutboxStatus.Pending;
        LastError = null;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
