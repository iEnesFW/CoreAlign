using CoreAlign.Domain.Entities.Privacy;

namespace CoreAlign.Domain.Interfaces;

public interface IRetentionPolicyRepository
{
    Task<RetentionPolicy?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<RetentionPolicy?> GetByEntityTypeAsync(Guid tenantId, string entityType, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RetentionPolicy>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RetentionPolicy>> ListAllEnabledAcrossTenantsAsync(CancellationToken cancellationToken = default);

    Task AddAsync(RetentionPolicy entity, CancellationToken cancellationToken = default);

    void Update(RetentionPolicy entity);
}
