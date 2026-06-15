using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Interfaces;

public interface IOutboxRepository
{
    Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default);
    void Update(OutboxMessage message);
    Task<OutboxMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Pending rows for the current tenant, oldest first, capped to <paramref name="max"/>.</summary>
    Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int max, CancellationToken cancellationToken = default);

    /// <summary>Recent rows for an admin/ops view, optionally filtered by status.</summary>
    Task<IReadOnlyList<OutboxMessage>> ListAsync(OutboxStatus? status, int max, CancellationToken cancellationToken = default);
}
