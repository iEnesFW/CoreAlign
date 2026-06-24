using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Interfaces;

public interface IOutboxRepository
{
    Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default);
    void Update(OutboxMessage message);
    Task<OutboxMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Due rows for the current tenant (relies on the tenant query filter), oldest first, capped to <paramref name="max"/>. Used by the inline post-commit drain.</summary>
    Task<IReadOnlyList<OutboxMessage>> GetDueForCurrentTenantAsync(int max, DateTime utcNow, CancellationToken cancellationToken = default);

    /// <summary>Due rows across all tenants (bypasses the query filter), oldest first. Used by the Hangfire background drain.</summary>
    Task<IReadOnlyList<OutboxMessage>> GetDueAcrossTenantsAsync(int max, DateTime utcNow, CancellationToken cancellationToken = default);

    /// <summary>Recent rows for an admin/ops view, optionally filtered by status.</summary>
    Task<IReadOnlyList<OutboxMessage>> ListAsync(OutboxStatus? status, int max, CancellationToken cancellationToken = default);
}
