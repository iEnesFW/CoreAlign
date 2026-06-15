using CoreAlign.Domain.Entities.Sso;

namespace CoreAlign.Domain.Interfaces;

public interface ITenantIdentityProviderRepository
{
    Task<TenantIdentityProvider?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TenantIdentityProvider?> GetByTenantAndNameAsync(Guid tenantId, string name, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TenantIdentityProvider>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TenantIdentityProvider>> ListActiveByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task AddAsync(TenantIdentityProvider entity, CancellationToken cancellationToken = default);
    void Update(TenantIdentityProvider entity);
}

public interface IExternalUserBindingRepository
{
    Task<ExternalUserBinding?> GetByExternalIdAsync(Guid identityProviderId, string externalUserId, CancellationToken cancellationToken = default);
    Task<ExternalUserBinding?> GetByLocalUserAsync(Guid identityProviderId, Guid localUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExternalUserBinding>> ListByUserAsync(Guid localUserId, CancellationToken cancellationToken = default);
    Task AddAsync(ExternalUserBinding entity, CancellationToken cancellationToken = default);
    void Update(ExternalUserBinding entity);
}
