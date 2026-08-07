using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Common;

public interface IFiscalYearResolver
{
    Task<int> GetStartMonthAsync(CancellationToken cancellationToken = default);
    Task<FiscalYearRange?> ResolveAsync(int? fiscalYear, CancellationToken cancellationToken = default);
}

/// <summary>
/// Turns a caller-supplied fiscal year into a date range using the CURRENT tenant's start month.
/// A list endpoint takes the year, never the range: a client that could send its own boundaries
/// could quietly widen a "2026" filter, and the tenant's start month would stop being the one
/// definition of a year.
/// </summary>
public sealed class FiscalYearResolver : IFiscalYearResolver
{
    private readonly ITenantRepository _tenants;
    private readonly ITenantContext _tenantContext;
    private int? _cachedStartMonth;

    public FiscalYearResolver(ITenantRepository tenants, ITenantContext tenantContext)
    {
        _tenants = tenants;
        _tenantContext = tenantContext;
    }

    public async Task<int> GetStartMonthAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedStartMonth.HasValue)
        {
            return _cachedStartMonth.Value;
        }
        var tenantId = _tenantContext.CurrentTenantId;
        if (!tenantId.HasValue || tenantId.Value == Guid.Empty)
        {
            _cachedStartMonth = FiscalYear.CalendarStartMonth;
            return _cachedStartMonth.Value;
        }
        var tenant = await _tenants.GetByIdAsync(tenantId.Value, cancellationToken).ConfigureAwait(false);
        _cachedStartMonth = FiscalYear.NormalizeStartMonth(tenant?.FiscalYearStartMonth ?? FiscalYear.CalendarStartMonth);
        return _cachedStartMonth.Value;
    }

    public async Task<FiscalYearRange?> ResolveAsync(int? fiscalYear, CancellationToken cancellationToken = default)
    {
        if (!fiscalYear.HasValue)
        {
            return null;
        }
        var startMonth = await GetStartMonthAsync(cancellationToken).ConfigureAwait(false);
        return FiscalYear.For(fiscalYear.Value, startMonth);
    }
}
