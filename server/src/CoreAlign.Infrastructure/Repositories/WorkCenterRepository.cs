using CoreAlign.Domain.Entities.Manufacturing;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class WorkCenterRepository : IWorkCenterRepository
{
    private readonly CoreAlignDbContext _context;
    public WorkCenterRepository(CoreAlignDbContext context) => _context = context;

    private DbSet<WorkCenter> WorkCenters => _context.Set<WorkCenter>();

    public async Task AddAsync(WorkCenter workCenter, CancellationToken cancellationToken = default) =>
        await WorkCenters.AddAsync(workCenter, cancellationToken);

    public async Task<IReadOnlyList<WorkCenter>> GetAllActiveAsync(CancellationToken cancellationToken = default) =>
        await WorkCenters.AsNoTracking()
            .Where(w => w.IsActive)
            .OrderBy(w => w.Code)
            .ToListAsync(cancellationToken);

    public Task<WorkCenter?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        WorkCenters.FirstOrDefaultAsync(w => w.Code == code, cancellationToken);

    public async Task<IReadOnlyList<WorkCenter>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default) =>
        await WorkCenters.AsNoTracking()
            .Where(w => ids.Contains(w.Id))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Guid>> GetActiveIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default) =>
        await WorkCenters.AsNoTracking()
            .Where(w => ids.Contains(w.Id) && w.IsActive)
            .Select(w => w.Id)
            .ToListAsync(cancellationToken);

    public Task<WorkCenter?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default) =>
        WorkCenters.FirstOrDefaultAsync(w => w.TenantId == tenantId && w.Id == id, cancellationToken);

    public async Task<IReadOnlyList<WorkCenter>> ListAsync(Guid tenantId, bool includeInactive, CancellationToken cancellationToken = default) =>
        await WorkCenters.AsNoTracking()
            .Where(w => w.TenantId == tenantId && (includeInactive || w.IsActive))
            .OrderBy(w => w.Code)
            .ToListAsync(cancellationToken);

    public Task<bool> CodeExistsAsync(Guid tenantId, string code, Guid? excludeId, CancellationToken cancellationToken = default) =>
        WorkCenters.AnyAsync(
            w => w.TenantId == tenantId && w.Code == code && (excludeId == null || w.Id != excludeId),
            cancellationToken);
}
