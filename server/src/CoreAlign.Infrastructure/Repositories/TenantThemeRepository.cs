using CoreAlign.Application.Whitelabel;
using CoreAlign.Domain.Entities.Whitelabel;
using CoreAlign.Domain.Enums;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public sealed class TenantThemeRepository : ITenantThemeRepository
{
    private readonly CoreAlignDbContext _context;

    public TenantThemeRepository(CoreAlignDbContext context) => _context = context;

    public Task<TenantTheme?> GetByTenantIdAsync(Guid tenantId, CancellationToken ct) =>
        _context.TenantThemes
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.TenantId == tenantId, ct);

    public Task<TenantTheme?> GetBySubdomainAsync(string subdomain, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(subdomain)) return Task.FromResult<TenantTheme?>(null);
        var normalized = subdomain.Trim().ToLowerInvariant();
        return _context.TenantThemes
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.CustomSubdomain == normalized, ct);
    }

    public Task<TenantTheme?> GetByCustomDomainAsync(string domain, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(domain)) return Task.FromResult<TenantTheme?>(null);
        var normalized = domain.Trim().ToLowerInvariant();
        return _context.TenantThemes
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.CustomDomain == normalized, ct);
    }

    public async Task AddAsync(TenantTheme entity, CancellationToken ct) =>
        await _context.TenantThemes.AddAsync(entity, ct).ConfigureAwait(false);

    public Task<bool> SubdomainExistsAsync(string subdomain, Guid excludingTenantId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(subdomain)) return Task.FromResult(false);
        var normalized = subdomain.Trim().ToLowerInvariant();
        return _context.TenantThemes
            .IgnoreQueryFilters()
            .AnyAsync(t => t.CustomSubdomain == normalized && t.TenantId != excludingTenantId, ct);
    }

    public async Task AddAssetAsync(TenantThemeAsset asset, CancellationToken ct) =>
        await _context.TenantThemeAssets.AddAsync(asset, ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<TenantThemeAsset>> ListAssetsAsync(Guid tenantId, CancellationToken ct) =>
        await _context.TenantThemeAssets
            .IgnoreQueryFilters()
            .Where(a => a.TenantId == tenantId)
            .OrderByDescending(a => a.CreatedAtUtc)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public Task<TenantThemeAsset?> GetLatestAssetAsync(Guid tenantId, TenantThemeAssetKind kind, CancellationToken ct) =>
        _context.TenantThemeAssets
            .IgnoreQueryFilters()
            .Where(a => a.TenantId == tenantId && a.AssetKind == kind)
            .OrderByDescending(a => a.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);
}
