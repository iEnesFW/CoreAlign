using CoreAlign.Application.Billing;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Services;

/// <summary>
/// Per-request cache of the current tenant's active module codes. Registered as
/// Scoped so the underlying DB lookup happens at most once per HTTP request.
/// </summary>
public sealed class ActiveModulesService : IActiveModulesService
{
    private readonly CoreAlignDbContext _context;
    private readonly ITenantContext _tenant;
    private IReadOnlySet<string>? _cache;

    public ActiveModulesService(CoreAlignDbContext context, ITenantContext tenant)
    {
        _context = context;
        _tenant = tenant;
    }

    public async Task<bool> IsActiveAsync(string moduleCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(moduleCode)) return false;
        var set = await GetActiveCodesAsync(cancellationToken);
        return set.Contains(moduleCode);
    }

    public async Task<IReadOnlySet<string>> GetActiveCodesAsync(CancellationToken cancellationToken = default)
    {
        if (_cache is not null) return _cache;

        var tenantId = _tenant.CurrentTenantId;
        if (tenantId is null)
        {
            _cache = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            return _cache;
        }

        var now = DateTime.UtcNow;
        var codes = await _context.TenantModules
            .AsNoTracking()
            .Where(t => t.EndUtc == null || t.EndUtc > now)
            .Join(
                _context.Modules.AsNoTracking().Where(m => m.IsActive),
                tm => tm.ModuleId,
                m => m.Id,
                (tm, m) => m.Code)
            .ToListAsync(cancellationToken);

        _cache = new HashSet<string>(codes, StringComparer.OrdinalIgnoreCase);
        return _cache;
    }
}
