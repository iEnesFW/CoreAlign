using CoreAlign.Domain.Entities.Manufacturing;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public sealed class ProductionJobRepository : IProductionJobRepository
{
    private readonly CoreAlignDbContext _context;

    public ProductionJobRepository(CoreAlignDbContext context) => _context = context;

    private DbSet<ProductionJob> Jobs => _context.Set<ProductionJob>();
    private DbSet<Product> Products => _context.Set<Product>();

    public async Task AddAsync(ProductionJob job, CancellationToken ct = default)
        => await Jobs.AddAsync(job, ct);

    public Task<ProductionJob?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken ct = default)
        => Jobs.Include(j => j.Steps)
            .FirstOrDefaultAsync(j => j.TenantId == tenantId && j.Id == id, ct);

    public Task<bool> JobNumberExistsAsync(Guid tenantId, string jobNumber, CancellationToken ct = default)
        => Jobs.AnyAsync(j => j.TenantId == tenantId && j.JobNumber == jobNumber, ct);

    public async Task<IReadOnlyList<ProductionJobListRow>> ListAsync(
        Guid tenantId,
        ProductionJobStatus? status,
        Guid? productId,
        int take,
        CancellationToken ct = default)
    {
        var query = from j in Jobs.AsNoTracking()
                    join p in Products.AsNoTracking() on j.ProductId equals p.Id
                    where j.TenantId == tenantId
                        && p.TenantId == tenantId
                        && (status == null || j.Status == status)
                        && (productId == null || j.ProductId == productId)
                    orderby j.CreatedAtUtc descending
                    select new ProductionJobListRow(
                        j.Id,
                        j.JobNumber,
                        j.ProductId,
                        p.Name,
                        j.Status,
                        j.PlannedQuantity,
                        j.CompletedQuantity,
                        j.ScrappedQuantity,
                        j.UnitOfMeasure,
                        j.CurrentStepNumber,
                        j.Steps.Count,
                        j.DueDateUtc,
                        j.CreatedAtUtc);

        return await query.Take(take).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ProductionJob>> GetByStatusAsync(Guid tenantId, ProductionJobStatus[] statuses, CancellationToken ct = default)
    {
        return await Jobs
            .Include(j => j.Steps)
            .Where(j => j.TenantId == tenantId && statuses.Contains(j.Status))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ProductionJob>> GetCompletedJobsInRangeAsync(Guid tenantId, DateTime start, DateTime end, CancellationToken ct = default)
    {
        return await Jobs
            .Include(j => j.Steps)
            .Where(j => j.TenantId == tenantId 
                     && j.Status == ProductionJobStatus.Completed 
                     && j.CompletedAtUtc >= start 
                     && j.CompletedAtUtc <= end)
            .ToListAsync(ct);
    }
}
