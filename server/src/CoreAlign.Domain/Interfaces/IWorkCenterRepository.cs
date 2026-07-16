using CoreAlign.Domain.Entities.Manufacturing;

namespace CoreAlign.Domain.Interfaces;

public interface IWorkCenterRepository
{
    Task AddAsync(WorkCenter workCenter, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkCenter>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    Task<WorkCenter?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkCenter>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> GetActiveIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);

    Task<WorkCenter?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkCenter>> ListAsync(Guid tenantId, bool includeInactive, CancellationToken cancellationToken = default);

    Task<bool> CodeExistsAsync(Guid tenantId, string code, Guid? excludeId, CancellationToken cancellationToken = default);
}
