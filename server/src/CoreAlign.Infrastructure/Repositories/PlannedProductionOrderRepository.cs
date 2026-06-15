using CoreAlign.Domain.Entities.Manufacturing;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class PlannedProductionOrderRepository : IPlannedProductionOrderRepository
{
    private readonly CoreAlignDbContext _context;
    public PlannedProductionOrderRepository(CoreAlignDbContext context) => _context = context;

    private DbSet<PlannedProductionOrder> Orders => _context.Set<PlannedProductionOrder>();

    public async Task AddRangeAsync(IReadOnlyList<PlannedProductionOrder> orders, CancellationToken cancellationToken = default)
    {
        if (orders.Count == 0)
        {
            return;
        }
        await Orders.AddRangeAsync(orders, cancellationToken);
    }

    public Task<PlannedProductionOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Orders.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<PlannedProductionOrder> Items, int Total)> SearchAsync(
        Guid? planRunId,
        Guid? productId,
        PlannedProductionOrderStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = Orders.AsNoTracking();

        if (planRunId.HasValue) query = query.Where(o => o.SourcePlanRunId == planRunId.Value);
        if (productId.HasValue) query = query.Where(o => o.ProductId == productId.Value);
        if (status.HasValue) query = query.Where(o => o.Status == status.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(o => o.LowLevelCode)
            .ThenBy(o => o.ReleaseDateUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, total);
    }
}
