using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class GlassProjectRepository : IGlassProjectRepository
{
    private readonly CoreAlignDbContext _context;
    public GlassProjectRepository(CoreAlignDbContext context) => _context = context;

    public Task<GlassProject?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.GlassProjects.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<GlassProject?> GetByIdWithRunsAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.GlassProjects
            // AsSplitQuery: Runs.Panels and Connections are sibling collections — a
            // single JOIN cross-joins them (panels × connections rows). Split into
            // separate queries so a large project doesn't materialize the product.
            .AsSplitQuery()
            .Include(p => p.Runs).ThenInclude(r => r.Panels).ThenInclude(pl => pl.Hardware)
            .Include(p => p.Connections)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<GlassProject?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        _context.GlassProjects.FirstOrDefaultAsync(p => p.Code == code, cancellationToken);

    public async Task<(IReadOnlyList<GlassProjectListItem> Items, int Total)> SearchAsync(
        string? search,
        GlassProjectStatus? status,
        Guid? customerId,
        Guid? assignedDesignerUserId,
        Guid? assignedSalespersonUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.GlassProjects.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var like = $"%{search.Trim()}%";
            query = query.Where(p => EF.Functions.ILike(p.Code, like) || EF.Functions.ILike(p.ProjectName, like));
        }
        if (status.HasValue) query = query.Where(p => p.Status == status.Value);
        if (customerId.HasValue) query = query.Where(p => p.CustomerId == customerId.Value);
        if (assignedDesignerUserId.HasValue) query = query.Where(p => p.AssignedDesignerUserId == assignedDesignerUserId.Value);
        if (assignedSalespersonUserId.HasValue) query = query.Where(p => p.AssignedSalespersonUserId == assignedSalespersonUserId.Value);

        var total = await query.CountAsync(cancellationToken);

        var rows = await query
            .OrderByDescending(p => p.UpdatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Join(
                _context.Customers,
                p => p.CustomerId,
                c => c.Id,
                (p, c) => new GlassProjectListItem(
                    p.Id, p.Code, p.ProjectName, p.CustomerId, c.Name, p.Status,
                    p.GrandTotal, p.Currency, p.TotalPanels, p.TotalAreaM2, p.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return (rows, total);
    }

    public async Task AddAsync(GlassProject project, CancellationToken cancellationToken = default) =>
        await _context.GlassProjects.AddAsync(project, cancellationToken);
    public void Update(GlassProject project) => _context.GlassProjects.Update(project);
    public void Remove(GlassProject project) => _context.GlassProjects.Remove(project);
}

public class GlassProjectRunRepository : IGlassProjectRunRepository
{
    private readonly CoreAlignDbContext _context;
    public GlassProjectRunRepository(CoreAlignDbContext context) => _context = context;

    public Task<GlassProjectRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.GlassProjectRuns.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public Task<GlassProjectRun?> GetByIdWithPanelsAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.GlassProjectRuns.Include(r => r.Panels).FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<GlassProjectRun>> ListByProjectAsync(Guid projectId, CancellationToken cancellationToken = default) =>
        await _context.GlassProjectRuns
            .Include(r => r.Panels)
            .Where(r => r.ProjectId == projectId)
            .OrderBy(r => r.OrderIndex)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(GlassProjectRun run, CancellationToken cancellationToken = default) =>
        await _context.GlassProjectRuns.AddAsync(run, cancellationToken);
    public void Update(GlassProjectRun run) => _context.GlassProjectRuns.Update(run);
    public void Remove(GlassProjectRun run) => _context.GlassProjectRuns.Remove(run);
}

public class RunConnectionRepository : IRunConnectionRepository
{
    private readonly CoreAlignDbContext _context;
    public RunConnectionRepository(CoreAlignDbContext context) => _context = context;

    public Task<RunConnection?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.GlassRunConnections.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<RunConnection>> ListByProjectAsync(Guid projectId, CancellationToken cancellationToken = default) =>
        await _context.GlassRunConnections.AsNoTracking().Where(c => c.ProjectId == projectId).ToListAsync(cancellationToken);

    public async Task AddAsync(RunConnection connection, CancellationToken cancellationToken = default) =>
        await _context.GlassRunConnections.AddAsync(connection, cancellationToken);
    public void Update(RunConnection connection) => _context.GlassRunConnections.Update(connection);
    public void Remove(RunConnection connection) => _context.GlassRunConnections.Remove(connection);
}

public class GlassProjectPanelRepository : IGlassProjectPanelRepository
{
    private readonly CoreAlignDbContext _context;
    public GlassProjectPanelRepository(CoreAlignDbContext context) => _context = context;

    public Task<GlassProjectPanel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.GlassProjectPanels.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<GlassProjectPanel>> ListByRunAsync(Guid runId, CancellationToken cancellationToken = default) =>
        await _context.GlassProjectPanels.AsNoTracking().Where(p => p.RunId == runId).OrderBy(p => p.PanelIndex).ToListAsync(cancellationToken);

    public async Task AddAsync(GlassProjectPanel panel, CancellationToken cancellationToken = default) =>
        await _context.GlassProjectPanels.AddAsync(panel, cancellationToken);
    public void Update(GlassProjectPanel panel) => _context.GlassProjectPanels.Update(panel);
    public void Remove(GlassProjectPanel panel) => _context.GlassProjectPanels.Remove(panel);
}

public class GlassProjectSceneRepository : IGlassProjectSceneRepository
{
    private readonly CoreAlignDbContext _context;
    public GlassProjectSceneRepository(CoreAlignDbContext context) => _context = context;

    public Task<GlassProjectScene?> GetByVersionAsync(Guid projectId, int version, CancellationToken cancellationToken = default) =>
        _context.GlassProjectScenes.FirstOrDefaultAsync(s => s.ProjectId == projectId && s.Version == version, cancellationToken);

    public Task<GlassProjectScene?> GetLatestAsync(Guid projectId, CancellationToken cancellationToken = default) =>
        _context.GlassProjectScenes
            .Where(s => s.ProjectId == projectId)
            .OrderByDescending(s => s.Version)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<GlassProjectScene>> ListVersionsAsync(Guid projectId, int limit, CancellationToken cancellationToken = default) =>
        await _context.GlassProjectScenes.AsNoTracking()
            .Where(s => s.ProjectId == projectId)
            .OrderByDescending(s => s.Version)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public async Task<int> GetMaxVersionAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var max = await _context.GlassProjectScenes
            .Where(s => s.ProjectId == projectId)
            .MaxAsync(s => (int?)s.Version, cancellationToken);
        return max ?? 0;
    }

    public async Task AddAsync(GlassProjectScene scene, CancellationToken cancellationToken = default) =>
        await _context.GlassProjectScenes.AddAsync(scene, cancellationToken);
    public void Update(GlassProjectScene scene) => _context.GlassProjectScenes.Update(scene);
}

public class GlassProjectChangeLogRepository : IGlassProjectChangeLogRepository
{
    private readonly CoreAlignDbContext _context;
    public GlassProjectChangeLogRepository(CoreAlignDbContext context) => _context = context;

    public async Task<IReadOnlyList<GlassProjectChangeLog>> ListByProjectAsync(Guid projectId, int limit, CancellationToken cancellationToken = default) =>
        await _context.GlassProjectChangeLogs.AsNoTracking()
            .Where(c => c.ProjectId == projectId)
            .OrderByDescending(c => c.CreatedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(GlassProjectChangeLog entry, CancellationToken cancellationToken = default) =>
        await _context.GlassProjectChangeLogs.AddAsync(entry, cancellationToken);
}

public class GlassProjectAttachmentRepository : IGlassProjectAttachmentRepository
{
    private readonly CoreAlignDbContext _context;
    public GlassProjectAttachmentRepository(CoreAlignDbContext context) => _context = context;

    public Task<GlassProjectAttachment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.GlassProjectAttachments.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<IReadOnlyList<GlassProjectAttachment>> ListByProjectAsync(Guid projectId, CancellationToken cancellationToken = default) =>
        await _context.GlassProjectAttachments.AsNoTracking().Where(a => a.ProjectId == projectId).ToListAsync(cancellationToken);

    public async Task AddAsync(GlassProjectAttachment attachment, CancellationToken cancellationToken = default) =>
        await _context.GlassProjectAttachments.AddAsync(attachment, cancellationToken);
    public void Remove(GlassProjectAttachment attachment) => _context.GlassProjectAttachments.Remove(attachment);
}

public class GlassProjectBOMLineRepository : IGlassProjectBOMLineRepository
{
    private readonly CoreAlignDbContext _context;
    public GlassProjectBOMLineRepository(CoreAlignDbContext context) => _context = context;

    public async Task<IReadOnlyList<GlassProjectBOMLine>> ListByProjectAsync(Guid projectId, CancellationToken cancellationToken = default) =>
        await _context.GlassProjectBOMLines.AsNoTracking()
            .Where(l => l.ProjectId == projectId)
            .OrderBy(l => l.Kind).ThenBy(l => l.SortOrder)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<GlassProjectBOMLine>> ListByProjectForUpdateAsync(Guid projectId, CancellationToken cancellationToken = default) =>
        await _context.GlassProjectBOMLines
            .Where(l => l.ProjectId == projectId)
            .OrderBy(l => l.Kind).ThenBy(l => l.SortOrder)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<GlassProjectBOMLine>> ListUnlinkedAsync(CancellationToken cancellationToken = default) =>
        await _context.GlassProjectBOMLines
            .Where(l => l.ProductId == null && !l.IsService && !l.IsManual)
            .ToListAsync(cancellationToken);

    public async Task ReplaceAllForProjectAsync(Guid projectId, IEnumerable<GlassProjectBOMLine> lines, CancellationToken cancellationToken = default)
    {
        var existing = await _context.GlassProjectBOMLines.Where(l => l.ProjectId == projectId).ToListAsync(cancellationToken);
        _context.GlassProjectBOMLines.RemoveRange(existing);
        await _context.GlassProjectBOMLines.AddRangeAsync(lines, cancellationToken);
    }

    public async Task AddAsync(GlassProjectBOMLine line, CancellationToken cancellationToken = default) =>
        await _context.GlassProjectBOMLines.AddAsync(line, cancellationToken);

    public void Update(GlassProjectBOMLine line) => _context.GlassProjectBOMLines.Update(line);

    public void Remove(GlassProjectBOMLine line) => _context.GlassProjectBOMLines.Remove(line);
}

public class GlassProjectCuttingPlanRepository : IGlassProjectCuttingPlanRepository
{
    private readonly CoreAlignDbContext _context;
    public GlassProjectCuttingPlanRepository(CoreAlignDbContext context) => _context = context;

    public Task<GlassProjectCuttingPlan?> GetLatestAsync(Guid projectId, GlassCuttingPlanType planType, CancellationToken cancellationToken = default) =>
        _context.GlassProjectCuttingPlans
            .Where(p => p.ProjectId == projectId && p.PlanType == planType)
            .OrderByDescending(p => p.GeneratedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task AddAsync(GlassProjectCuttingPlan plan, CancellationToken cancellationToken = default) =>
        await _context.GlassProjectCuttingPlans.AddAsync(plan, cancellationToken);
}

public class GlassProjectQuoteSnapshotRepository : IGlassProjectQuoteSnapshotRepository
{
    private readonly CoreAlignDbContext _context;
    public GlassProjectQuoteSnapshotRepository(CoreAlignDbContext context) => _context = context;

    public Task<GlassProjectQuoteSnapshot?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.GlassProjectQuoteSnapshots.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<IReadOnlyList<GlassProjectQuoteSnapshot>> ListByProjectAsync(Guid projectId, CancellationToken cancellationToken = default) =>
        await _context.GlassProjectQuoteSnapshots.AsNoTracking()
            .Where(s => s.ProjectId == projectId)
            .OrderByDescending(s => s.IssuedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(GlassProjectQuoteSnapshot snapshot, CancellationToken cancellationToken = default) =>
        await _context.GlassProjectQuoteSnapshots.AddAsync(snapshot, cancellationToken);
}

public class GlassProjectShareTokenRepository : IGlassProjectShareTokenRepository
{
    private readonly CoreAlignDbContext _context;
    public GlassProjectShareTokenRepository(CoreAlignDbContext context) => _context = context;

    public Task<GlassProjectShareToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default) =>
        _context.GlassProjectShareTokens.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Token == token, cancellationToken);

    public async Task<IReadOnlyList<GlassProjectShareToken>> ListByProjectAsync(Guid projectId, CancellationToken cancellationToken = default) =>
        await _context.GlassProjectShareTokens.AsNoTracking().Where(t => t.ProjectId == projectId).ToListAsync(cancellationToken);

    public async Task AddAsync(GlassProjectShareToken token, CancellationToken cancellationToken = default) =>
        await _context.GlassProjectShareTokens.AddAsync(token, cancellationToken);
    public void Update(GlassProjectShareToken token) => _context.GlassProjectShareTokens.Update(token);
}

public class FieldSurveyRepository : IFieldSurveyRepository
{
    private readonly CoreAlignDbContext _context;
    public FieldSurveyRepository(CoreAlignDbContext context) => _context = context;

    public Task<FieldSurvey?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.GlassFieldSurveys.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<IReadOnlyList<FieldSurvey>> ListByProjectAsync(Guid projectId, CancellationToken cancellationToken = default) =>
        await _context.GlassFieldSurveys.AsNoTracking()
            .Where(s => s.ProjectId == projectId)
            .OrderByDescending(s => s.SurveyedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(FieldSurvey survey, CancellationToken cancellationToken = default) =>
        await _context.GlassFieldSurveys.AddAsync(survey, cancellationToken);
    public void Update(FieldSurvey survey) => _context.GlassFieldSurveys.Update(survey);
    public void Remove(FieldSurvey survey) => _context.GlassFieldSurveys.Remove(survey);
}

public class GlassWorkOrderRepository : IGlassWorkOrderRepository
{
    private readonly CoreAlignDbContext _context;
    public GlassWorkOrderRepository(CoreAlignDbContext context) => _context = context;

    public Task<GlassWorkOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.GlassWorkOrders.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

    public async Task<IReadOnlyList<GlassWorkOrder>> ListByProjectAsync(Guid projectId, CancellationToken cancellationToken = default) =>
        await _context.GlassWorkOrders.AsNoTracking().Where(w => w.ProjectId == projectId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<GlassWorkOrder>> ListReleasableByProjectAsync(Guid projectId, CancellationToken cancellationToken = default) =>
        await _context.GlassWorkOrders
            .Where(w => w.ProjectId == projectId
                && w.BomSnapshotJson != null
                && w.Status != GlassWorkOrderStatus.Installed
                && !w.IsDeleted)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<GlassWorkOrder>> ListInRangeAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default) =>
        await _context.GlassWorkOrders.AsNoTracking()
            .Where(w => w.ScheduledStartDate < toUtc && w.ScheduledEndDate >= fromUtc)
            .OrderBy(w => w.ScheduledStartDate)
            .ToListAsync(cancellationToken);

    public async Task<decimal> GetWorkloadM2ForDayAsync(DateTime dayUtc, CancellationToken cancellationToken = default)
    {
        var dayStart = new DateTime(dayUtc.Year, dayUtc.Month, dayUtc.Day, 0, 0, 0, DateTimeKind.Utc);
        var dayEnd = dayStart.AddDays(1);
        return await _context.GlassWorkOrders.AsNoTracking()
            .Where(w => w.ScheduledStartDate < dayEnd && w.ScheduledEndDate >= dayStart)
            .SumAsync(w => (decimal?)w.WorkloadM2, cancellationToken) ?? 0m;
    }

    public async Task AddAsync(GlassWorkOrder workOrder, CancellationToken cancellationToken = default) =>
        await _context.GlassWorkOrders.AddAsync(workOrder, cancellationToken);
    public void Update(GlassWorkOrder workOrder) => _context.GlassWorkOrders.Update(workOrder);
}

public class GlassProjectOrderLinkRepository : IGlassProjectOrderLinkRepository
{
    private readonly CoreAlignDbContext _context;
    public GlassProjectOrderLinkRepository(CoreAlignDbContext context) => _context = context;

    public Task<GlassProjectOrderLink?> GetByProjectAsync(Guid projectId, CancellationToken cancellationToken = default) =>
        _context.GlassProjectOrderLinks.FirstOrDefaultAsync(l => l.ProjectId == projectId, cancellationToken);

    public async Task AddAsync(GlassProjectOrderLink link, CancellationToken cancellationToken = default) =>
        await _context.GlassProjectOrderLinks.AddAsync(link, cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, decimal>> SumPendingOrderDemandByProductsAsync(
        IReadOnlyCollection<Guid> productIds,
        Guid excludeProjectId,
        IReadOnlyCollection<OrderStatus> pendingStatuses,
        CancellationToken cancellationToken = default)
    {
        if (productIds.Count == 0 || pendingStatuses.Count == 0)
        {
            return new Dictionary<Guid, decimal>();
        }

        var productIdList = productIds.Distinct().ToList();
        var statusList = pendingStatuses.Distinct().ToList();

        // Tenant isolation comes for free: links, orders and lines are all tenant-scoped, so the
        // global query filter confines the join to the current tenant.
        var rows = await (
            from link in _context.GlassProjectOrderLinks
            where link.ProjectId != excludeProjectId
            join order in _context.Orders on link.OrderId equals order.Id
            where statusList.Contains(order.Status)
            join orderLine in _context.OrderLines on order.Id equals orderLine.OrderId
            where !orderLine.IsService && productIdList.Contains(orderLine.ProductId)
            group orderLine.Quantity by orderLine.ProductId into grouped
            select new { ProductId = grouped.Key, Quantity = grouped.Sum() }).ToListAsync(cancellationToken);

        return rows.ToDictionary(r => r.ProductId, r => r.Quantity);
    }
}

public class GlassNotificationLogRepository : IGlassNotificationLogRepository
{
    private readonly CoreAlignDbContext _context;
    public GlassNotificationLogRepository(CoreAlignDbContext context) => _context = context;

    public Task<GlassNotificationLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.GlassNotificationLogs.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

    public async Task<IReadOnlyList<GlassNotificationLog>> ListByProjectAsync(Guid projectId, CancellationToken cancellationToken = default) =>
        await _context.GlassNotificationLogs.AsNoTracking()
            .Where(l => l.ProjectId == projectId)
            .OrderByDescending(l => l.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<GlassNotificationLog>> ListFailedForRetryAsync(int maxRetries, int batchSize, CancellationToken cancellationToken = default) =>
        await _context.GlassNotificationLogs
            .Where(l => l.Status == Domain.Enums.GlassNotificationStatus.Failed && l.RetryCount < maxRetries)
            .OrderBy(l => l.UpdatedAtUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(GlassNotificationLog log, CancellationToken cancellationToken = default) =>
        await _context.GlassNotificationLogs.AddAsync(log, cancellationToken);
    public void Update(GlassNotificationLog log) => _context.GlassNotificationLogs.Update(log);
}
