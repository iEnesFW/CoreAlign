using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class ModuleRepository : IModuleRepository
{
    private readonly CoreAlignDbContext _context;
    public ModuleRepository(CoreAlignDbContext context) => _context = context;

    public Task<Module?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Modules.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    public Task<Module?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        _context.Modules.FirstOrDefaultAsync(m => m.Code == code, cancellationToken);

    public async Task<IReadOnlyList<Module>> ListAsync(bool activeOnly, CancellationToken cancellationToken = default)
    {
        var query = _context.Modules.AsNoTracking();
        if (activeOnly) query = query.Where(m => m.IsActive);
        return await query
            .OrderBy(m => m.SortOrder)
            .ThenBy(m => m.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Module>> ListByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0) return Array.Empty<Module>();
        return await _context.Modules.AsNoTracking().Where(m => ids.Contains(m.Id)).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Module module, CancellationToken cancellationToken = default) =>
        await _context.Modules.AddAsync(module, cancellationToken);

    public void Update(Module module) => _context.Modules.Update(module);
}

public class ModulePricePlanRepository : IModulePricePlanRepository
{
    private readonly CoreAlignDbContext _context;
    public ModulePricePlanRepository(CoreAlignDbContext context) => _context = context;

    public Task<ModulePricePlan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.ModulePricePlans.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ModulePricePlan>> ListByModuleAsync(Guid moduleId, bool activeOnly, CancellationToken cancellationToken = default)
    {
        var query = _context.ModulePricePlans.AsNoTracking().Where(p => p.ModuleId == moduleId);
        if (activeOnly) query = query.Where(p => p.IsActive);
        return await query.OrderBy(p => p.SortOrder).ThenBy(p => p.DurationDays).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ModulePricePlan>> ListByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0) return Array.Empty<ModulePricePlan>();
        return await _context.ModulePricePlans.AsNoTracking().Where(p => ids.Contains(p.Id)).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ModulePricePlan>> ListAllActiveAsync(CancellationToken cancellationToken = default) =>
        await _context.ModulePricePlans
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.ModuleId)
            .ThenBy(p => p.SortOrder)
            .ToListAsync(cancellationToken);

    public Task<ModulePricePlan?> GetByModuleAndCodeAsync(Guid moduleId, string code, CancellationToken cancellationToken = default) =>
        _context.ModulePricePlans.FirstOrDefaultAsync(p => p.ModuleId == moduleId && p.Code == code, cancellationToken);

    public async Task AddAsync(ModulePricePlan plan, CancellationToken cancellationToken = default) =>
        await _context.ModulePricePlans.AddAsync(plan, cancellationToken);

    public void Update(ModulePricePlan plan) => _context.ModulePricePlans.Update(plan);
}

public class TenantModuleRepository : ITenantModuleRepository
{
    private readonly CoreAlignDbContext _context;
    public TenantModuleRepository(CoreAlignDbContext context) => _context = context;

    public Task<TenantModule?> GetByModuleIdAsync(Guid moduleId, CancellationToken cancellationToken = default) =>
        _context.TenantModules.FirstOrDefaultAsync(t => t.ModuleId == moduleId, cancellationToken);

    public async Task<IReadOnlyList<TenantModule>> ListAsync(CancellationToken cancellationToken = default) =>
        await _context.TenantModules.AsNoTracking().ToListAsync(cancellationToken);

    public async Task AddAsync(TenantModule tenantModule, CancellationToken cancellationToken = default) =>
        await _context.TenantModules.AddAsync(tenantModule, cancellationToken);

    public void Update(TenantModule tenantModule) => _context.TenantModules.Update(tenantModule);
}

public class SubscriptionOrderRepository : ISubscriptionOrderRepository
{
    private readonly CoreAlignDbContext _context;
    public SubscriptionOrderRepository(CoreAlignDbContext context) => _context = context;

    public Task<SubscriptionOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.SubscriptionOrders.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public Task<SubscriptionOrder?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.SubscriptionOrders
            .Include(o => o.Items)
            .Include(o => o.Attempts)
            .AsSplitQuery()
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public Task<SubscriptionOrder?> GetByGatewayIntentAsync(string gatewayName, string intentId, CancellationToken cancellationToken = default) =>
        _context.SubscriptionOrders
            .IgnoreQueryFilters()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.GatewayName == gatewayName && o.GatewayIntentId == intentId, cancellationToken);

    public async Task<(IReadOnlyList<SubscriptionOrder> Items, int Total)> ListAsync(
        SubscriptionOrderStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.SubscriptionOrders.AsNoTracking().AsQueryable();
        if (status.HasValue) query = query.Where(o => o.Status == status.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(o => o.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task AddAsync(SubscriptionOrder order, CancellationToken cancellationToken = default) =>
        await _context.SubscriptionOrders.AddAsync(order, cancellationToken);

    public void Update(SubscriptionOrder order) => _context.SubscriptionOrders.Update(order);
}

public class PaymentAttemptRepository : IPaymentAttemptRepository
{
    private readonly CoreAlignDbContext _context;
    public PaymentAttemptRepository(CoreAlignDbContext context) => _context = context;

    public async Task AddAsync(PaymentAttempt attempt, CancellationToken cancellationToken = default) =>
        await _context.PaymentAttempts.AddAsync(attempt, cancellationToken);

    public async Task<IReadOnlyList<PaymentAttempt>> ListByOrderAsync(Guid subscriptionOrderId, CancellationToken cancellationToken = default) =>
        await _context.PaymentAttempts
            .AsNoTracking()
            .Where(a => a.SubscriptionOrderId == subscriptionOrderId)
            .OrderByDescending(a => a.AttemptedAtUtc)
            .ToListAsync(cancellationToken);
}
