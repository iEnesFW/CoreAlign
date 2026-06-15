using CoreAlign.Domain.Entities.Privacy;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class RetentionPolicyRepository : IRetentionPolicyRepository
{
    private readonly CoreAlignDbContext _context;

    public RetentionPolicyRepository(CoreAlignDbContext context) => _context = context;

    public Task<RetentionPolicy?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.RetentionPolicies.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<RetentionPolicy?> GetByEntityTypeAsync(Guid tenantId, string entityType, CancellationToken cancellationToken = default) =>
        _context.RetentionPolicies
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.EntityType == entityType, cancellationToken);

    public async Task<IReadOnlyList<RetentionPolicy>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
        await _context.RetentionPolicies
            .AsNoTracking()
            .OrderBy(p => p.EntityType)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<RetentionPolicy>> ListAllEnabledAcrossTenantsAsync(CancellationToken cancellationToken = default) =>
        await _context.RetentionPolicies
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(p => p.IsEnabled && !p.IsDeleted)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(RetentionPolicy entity, CancellationToken cancellationToken = default) =>
        await _context.RetentionPolicies.AddAsync(entity, cancellationToken);

    public void Update(RetentionPolicy entity) => _context.RetentionPolicies.Update(entity);
}
