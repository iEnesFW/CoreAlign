using CoreAlign.Domain.Entities.Sso;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class TenantIdentityProviderRepository : ITenantIdentityProviderRepository
{
    private readonly CoreAlignDbContext _context;

    public TenantIdentityProviderRepository(CoreAlignDbContext context) => _context = context;

    public Task<TenantIdentityProvider?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.TenantIdentityProviders.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<TenantIdentityProvider?> GetByTenantAndNameAsync(Guid tenantId, string name, CancellationToken cancellationToken = default) =>
        _context.TenantIdentityProviders
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Name == name && !p.IsDeleted, cancellationToken);

    public async Task<IReadOnlyList<TenantIdentityProvider>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
        await _context.TenantIdentityProviders
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<TenantIdentityProvider>> ListActiveByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
        await _context.TenantIdentityProviders
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId && p.IsActive && !p.IsDeleted)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(TenantIdentityProvider entity, CancellationToken cancellationToken = default) =>
        await _context.TenantIdentityProviders.AddAsync(entity, cancellationToken);

    public void Update(TenantIdentityProvider entity) => _context.TenantIdentityProviders.Update(entity);
}
