using CoreAlign.Application.Common.Audit;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Integration.Tests.Providers.TestFixtures;

/// <summary>In-memory <see cref="IOutboxRepository"/> snapshot — captures enqueued messages for assertion.</summary>
public sealed class InMemoryOutboxRepository : IOutboxRepository
{
    public List<OutboxMessage> Messages { get; } = new();

    public Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        Messages.Add(message);
        return Task.CompletedTask;
    }

    public void Update(OutboxMessage message) { }

    public Task<OutboxMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult<OutboxMessage?>(Messages.FirstOrDefault(m => m.Id == id));

    public Task<IReadOnlyList<OutboxMessage>> GetDueForCurrentTenantAsync(int max, DateTime utcNow, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<OutboxMessage>>(
            Messages.Where(m => m.Status == OutboxStatus.Pending && (m.NextAttemptUtc == null || m.NextAttemptUtc <= utcNow))
                .Take(max).ToList());

    public Task<IReadOnlyList<OutboxMessage>> GetDueAcrossTenantsAsync(int max, DateTime utcNow, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<OutboxMessage>>(
            Messages.Where(m => m.Status == OutboxStatus.Pending && (m.NextAttemptUtc == null || m.NextAttemptUtc <= utcNow))
                .Take(max).ToList());

    public Task<IReadOnlyList<OutboxMessage>> ListAsync(OutboxStatus? status, int max, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<OutboxMessage>>(Messages.Take(max).ToList());
}

/// <summary>In-memory <see cref="IAuditContext"/> — captures dispatch / refund audit entries for assertion.</summary>
public sealed class InMemoryAuditContext : IAuditContext
{
    private readonly List<AuditEntry> _entries = new();

    public void Capture(Guid aggregateId, string aggregateType, string field, string? oldValue, string? newValue)
    {
        _entries.Add(new AuditEntry(aggregateId, aggregateType, "FieldChange", field, oldValue, newValue, DateTime.UtcNow));
    }

    public void CaptureCustom(Guid aggregateId, string aggregateType, string changeKind, string details)
    {
        _entries.Add(new AuditEntry(aggregateId, aggregateType, changeKind, null, null, details, DateTime.UtcNow));
    }

    public IReadOnlyList<AuditEntry> PendingEntries => _entries;

    public void Clear() => _entries.Clear();
}
