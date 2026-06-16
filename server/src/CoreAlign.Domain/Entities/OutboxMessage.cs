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
    public DateTime? NextAttemptUtc { get; private set; }
    public int MaxAttempts { get; private set; } = DefaultMaxAttempts;

    public const int DefaultMaxAttempts = 8;

    public bool HasExhaustedAttempts => Attempts + 1 >= MaxAttempts;

    protected OutboxMessage() { }

    public OutboxMessage(string type, string payloadJson, int maxAttempts = DefaultMaxAttempts)
    {
        if (string.IsNullOrWhiteSpace(type)) throw new ArgumentException("Type is required.", nameof(type));
        if (string.IsNullOrWhiteSpace(payloadJson)) throw new ArgumentException("Payload is required.", nameof(payloadJson));
        Type = type.Trim();
        PayloadJson = payloadJson;
        MaxAttempts = maxAttempts < 1 ? DefaultMaxAttempts : maxAttempts;
    }

    public void MarkProcessed(string result)
    {
        Status = OutboxStatus.Processed;
        Result = result;
        LastError = null;
        NextAttemptUtc = null;
        ProcessedAtUtc = DateTime.UtcNow;
        Attempts++;
        UpdatedAtUtc = ProcessedAtUtc.Value;
    }

    public void ScheduleRetry(DateTime nextAttemptUtc, string lastError)
    {
        Status = OutboxStatus.Pending;
        LastError = lastError;
        NextAttemptUtc = nextAttemptUtc;
        Attempts++;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void DeferUntil(DateTime nextAttemptUtc, string reason)
    {
        Status = OutboxStatus.Pending;
        LastError = reason;
        NextAttemptUtc = nextAttemptUtc;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkDeadLetter(string error)
    {
        Status = OutboxStatus.DeadLetter;
        LastError = error;
        NextAttemptUtc = null;
        Attempts++;
        UpdatedAtUtc = DateTime.UtcNow;
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
        NextAttemptUtc = null;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
