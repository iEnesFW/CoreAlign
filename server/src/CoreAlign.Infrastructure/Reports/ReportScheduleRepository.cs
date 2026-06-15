using CoreAlign.Domain.Entities.Reporting;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Reports;

public sealed class ReportScheduleRepository : IReportScheduleRepository
{
    private readonly CoreAlignDbContext _context;

    public ReportScheduleRepository(CoreAlignDbContext context)
    {
        _context = context;
    }

    public Task<ReportSchedule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Set<ReportSchedule>().FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ReportSchedule>> ListAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _context.Set<ReportSchedule>()
            .AsNoTracking()
            .OrderByDescending(s => s.UpdatedAtUtc)
            .ToListAsync(cancellationToken);
        return rows;
    }

    public async Task<IReadOnlyList<ReportSchedule>> GetDueAsync(DateTime asOfUtc, CancellationToken cancellationToken = default)
    {
        var bound = DateTime.SpecifyKind(asOfUtc, DateTimeKind.Utc);
        var rows = await _context.Set<ReportSchedule>()
            .IgnoreQueryFilters()
            .Where(s => s.IsActive && s.NextRunAtUtc <= bound)
            .OrderBy(s => s.NextRunAtUtc)
            .Take(200)
            .ToListAsync(cancellationToken);
        return rows;
    }

    public async Task AddAsync(ReportSchedule schedule, CancellationToken cancellationToken = default)
    {
        await _context.Set<ReportSchedule>().AddAsync(schedule, cancellationToken);
    }

    public void Update(ReportSchedule schedule) => _context.Set<ReportSchedule>().Update(schedule);

    public void Remove(ReportSchedule schedule) => _context.Set<ReportSchedule>().Remove(schedule);
}
