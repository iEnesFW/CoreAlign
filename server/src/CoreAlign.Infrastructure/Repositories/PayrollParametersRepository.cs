using CoreAlign.Domain.Entities.Payroll;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public sealed class PayrollParametersRepository : IPayrollParametersRepository
{
    private readonly CoreAlignDbContext _context;
    public PayrollParametersRepository(CoreAlignDbContext context) => _context = context;

    public async Task<PayrollParameters?> ResolveAsync(int year, DateOnly period, CancellationToken cancellationToken = default)
    {
        var tenantId = _context.CurrentTenantIdOrEmpty;
        var candidates = await _context.PayrollParameters
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(p => p.TaxBrackets)
            .Where(p => p.TenantId == tenantId || p.TenantId == Guid.Empty)
            .Where(p => p.EffectiveYear == year)
            .ToListAsync(cancellationToken);

        return candidates
            .Where(p => p.IsCurrentlyValid(period))
            .OrderByDescending(p => p.TenantId != Guid.Empty)
            .ThenByDescending(p => p.EffectiveFrom)
            .FirstOrDefault();
    }

    public async Task<IReadOnlyList<PayrollParameters>> ListAsync(int? year, CancellationToken cancellationToken = default)
    {
        var tenantId = _context.CurrentTenantIdOrEmpty;
        var query = _context.PayrollParameters
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(p => p.TaxBrackets)
            .Where(p => p.TenantId == tenantId || p.TenantId == Guid.Empty);

        if (year.HasValue)
        {
            query = query.Where(p => p.EffectiveYear == year.Value);
        }

        return await query
            .OrderByDescending(p => p.EffectiveYear)
            .ThenByDescending(p => p.EffectiveFrom)
            .ThenByDescending(p => p.TenantId != Guid.Empty)
            .ToListAsync(cancellationToken);
    }

    public async Task<PayrollParameters?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = _context.CurrentTenantIdOrEmpty;
        return await _context.PayrollParameters
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(p => p.TaxBrackets)
            .Where(p => p.TenantId == tenantId || p.TenantId == Guid.Empty)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public Task<PayrollParameters?> GetOwnedByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.PayrollParameters
            .Include(p => p.TaxBrackets)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task AddAsync(PayrollParameters parameters, CancellationToken cancellationToken = default) =>
        _context.PayrollParameters.AddAsync(parameters, cancellationToken).AsTask();

    public void Update(PayrollParameters parameters) => _context.PayrollParameters.Update(parameters);
}
