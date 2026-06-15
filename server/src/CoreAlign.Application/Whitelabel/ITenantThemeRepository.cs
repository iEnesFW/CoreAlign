using CoreAlign.Domain.Entities.Whitelabel;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Whitelabel;

public interface ITenantThemeRepository
{
    Task<TenantTheme?> GetByTenantIdAsync(Guid tenantId, CancellationToken ct);
    Task<TenantTheme?> GetBySubdomainAsync(string subdomain, CancellationToken ct);
    Task<TenantTheme?> GetByCustomDomainAsync(string domain, CancellationToken ct);
    Task AddAsync(TenantTheme entity, CancellationToken ct);
    Task<bool> SubdomainExistsAsync(string subdomain, Guid excludingTenantId, CancellationToken ct);
    Task AddAssetAsync(TenantThemeAsset asset, CancellationToken ct);
    Task<IReadOnlyList<TenantThemeAsset>> ListAssetsAsync(Guid tenantId, CancellationToken ct);
    Task<TenantThemeAsset?> GetLatestAssetAsync(Guid tenantId, TenantThemeAssetKind kind, CancellationToken ct);
}
