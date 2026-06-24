using CoreAlign.Domain.Entities.Payroll;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public sealed class PayrollRunRepository : IPayrollRunRepository
{
    private readonly CoreAlignDbContext _context;
    public PayrollRunRepository(CoreAlignDbContext context) => _context = context;

    public Task<PayrollRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.PayrollRuns.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public Task<PayrollRun?> GetWithPayslipsAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.PayrollRuns
            .AsSplitQuery()
            .Include(r => r.Parameters)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public Task<bool> ExistsForPeriodAsync(int periodYear, int periodMonth, PayrollRunType runType, CancellationToken cancellationToken = default) =>
        _context.PayrollRuns.AnyAsync(
            r => r.PeriodYear == periodYear && r.PeriodMonth == periodMonth && r.RunType == runType,
            cancellationToken);

    public async Task<(IReadOnlyList<PayrollRun> Items, int Total)> GetPagedAsync(
        int? periodYear,
        PayrollRunStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.PayrollRuns.AsNoTracking().AsQueryable();
        if (periodYear.HasValue) query = query.Where(r => r.PeriodYear == periodYear.Value);
        if (status.HasValue) query = query.Where(r => r.Status == status.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(r => r.PeriodYear)
            .ThenByDescending(r => r.PeriodMonth)
            .ThenByDescending(r => r.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task AddAsync(PayrollRun run, CancellationToken cancellationToken = default) =>
        await _context.PayrollRuns.AddAsync(run, cancellationToken);

    public void Update(PayrollRun run) => _context.PayrollRuns.Update(run);
}
