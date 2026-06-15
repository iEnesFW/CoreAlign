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
}
