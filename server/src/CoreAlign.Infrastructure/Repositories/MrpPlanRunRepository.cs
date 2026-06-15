using CoreAlign.Domain.Entities.Mrp;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class MrpPlanRunRepository : IMrpPlanRunRepository
{
    private readonly CoreAlignDbContext _context;
    public MrpPlanRunRepository(CoreAlignDbContext context) => _context = context;

    private DbSet<MrpPlanRun> PlanRuns => _context.Set<MrpPlanRun>();
    private DbSet<MrpPlannedOrder> PlannedOrders => _context.Set<MrpPlannedOrder>();
    private DbSet<MrpActionMessage> ActionMessages => _context.Set<MrpActionMessage>();
    private DbSet<MrpPegging> Peggings => _context.Set<MrpPegging>();

    public async Task AddAsync(MrpPlanRun planRun, CancellationToken cancellationToken = default) =>
        await PlanRuns.AddAsync(planRun, cancellationToken);

    public void Update(MrpPlanRun planRun) => PlanRuns.Update(planRun);

    public Task<MrpPlanRun?> GetByIdAsync(Guid id, bool includeChildren, CancellationToken cancellationToken = default)
    {
        var query = PlanRuns.AsQueryable();
        if (includeChildren)
        {
            query = query
                .Include(r => r.PlannedOrders)
                .Include(r => r.ActionMessages)
                .AsSplitQuery();
        }
        return query.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public Task<MrpPlanRun?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default) =>
        PlanRuns.FirstOrDefaultAsync(r => r.IdempotencyKey == idempotencyKey, cancellationToken);

    public async Task<(IReadOnlyList<MrpPlanRun> Items, int Total)> SearchPlanRunsAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = PlanRuns.AsNoTracking();
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(r => r.AsOfDateUtc)
            .ThenByDescending(r => r.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<(IReadOnlyList<MrpActionMessage> Items, int Total)> SearchActionMessagesAsync(
        Guid? planRunId,
        MrpActionType? actionType,
        MrpActionSeverity? severity,
        Guid? supplierId,
        bool includeDismissed,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = ActionMessages.AsNoTracking();

        if (planRunId.HasValue) query = query.Where(m => m.PlanRunId == planRunId.Value);
        if (actionType.HasValue) query = query.Where(m => m.ActionType == actionType.Value);
        if (severity.HasValue) query = query.Where(m => m.Severity == severity.Value);
        if (!includeDismissed) query = query.Where(m => !m.IsDismissed);

        if (supplierId.HasValue)
        {
            var supplierProductIds = PlannedOrders
                .Where(o => o.PreferredSupplierId == supplierId.Value)
                .Select(o => o.ProductId);
            query = query.Where(m => supplierProductIds.Contains(m.ProductId));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(m => m.DaysUntilStockOut)
            .ThenByDescending(m => m.Severity)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, total);
    }

    public Task<MrpActionMessage?> GetActionMessageByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        ActionMessages.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    public Task<MrpPlannedOrder?> GetPlannedOrderByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        PlannedOrders.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public async Task<IReadOnlyList<MrpPlannedOrder>> GetPlannedOrdersAsync(
        Guid planRunId,
        IReadOnlyList<Guid> plannedOrderIds,
        CancellationToken cancellationToken = default)
    {
        var query = PlannedOrders.Where(o => o.PlanRunId == planRunId);
        if (plannedOrderIds.Count > 0)
        {
            query = query.Where(o => plannedOrderIds.Contains(o.Id));
        }
        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MrpPegging>> GetPeggingAsync(
        Guid planRunId,
        Guid componentProductId,
        CancellationToken cancellationToken = default) =>
        await Peggings.AsNoTracking()
            .Where(p => p.PlanRunId == planRunId && p.ComponentProductId == componentProductId)
            .OrderBy(p => p.DueDateUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<MrpPegging>> GetAllPeggingAsync(
        Guid planRunId,
        CancellationToken cancellationToken = default) =>
        await Peggings.AsNoTracking()
            .Where(p => p.PlanRunId == planRunId)
            .OrderBy(p => p.DueDateUtc)
            .ToListAsync(cancellationToken);
}
