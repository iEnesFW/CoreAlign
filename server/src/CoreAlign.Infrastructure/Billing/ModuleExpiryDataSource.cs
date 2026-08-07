using CoreAlign.Application.Billing.Expiry;
using CoreAlign.Domain.Entities;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Billing;

public sealed class ModuleExpiryDataSource : IModuleExpiryDataSource
{
    private readonly CoreAlignDbContext _context;

    public ModuleExpiryDataSource(CoreAlignDbContext context) => _context = context;

    public async Task<IReadOnlyList<ExpiringModuleSnapshot>> GetExpiringAsync(
        DateTime nowUtc,
        int withinDays,
        int max,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);
        var horizon = now.AddDays(withinDays);

        // IgnoreQueryFilters with no tenant predicate on purpose: the job runs without an HTTP
        // context, so the global filter would compare against Guid.Empty and return nothing.
        // A perpetual grant (EndUtc == null) can never lapse and is excluded here, not later.
        // WHY: order and page over the joined rows, never over the projected record — EF cannot
        // translate a member read off a constructor call, so projecting first breaks the query.
        return await _context.Set<TenantModule>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(t => t.EndUtc != null && t.EndUtc > now && t.EndUtc <= horizon && t.StartUtc <= now)
            .Join(
                _context.Set<Module>().AsNoTracking(),
                t => t.ModuleId,
                m => m.Id,
                (t, m) => new { Grant = t, Module = m })
            .OrderBy(x => x.Grant.EndUtc)
            .ThenBy(x => x.Grant.Id)
            .Take(max)
            .Select(x => new ExpiringModuleSnapshot(
                x.Grant.TenantId,
                x.Grant.Id,
                x.Module.Id,
                x.Module.Code,
                x.Module.Name,
                x.Grant.EndUtc!.Value))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Guid>> GetTenantAdminUserIdsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(u => u.TenantId == tenantId && u.IsActive)
            .Where(u => u.UserRoles.Any(ur => ur.Role.Name == "TenantAdmin"))
            .Select(u => u.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
