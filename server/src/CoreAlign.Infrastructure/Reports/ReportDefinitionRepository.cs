using CoreAlign.Domain.Entities.Reporting;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Reports;

public sealed class ReportDefinitionRepository : IReportDefinitionRepository
{
    private readonly CoreAlignDbContext _context;

    public ReportDefinitionRepository(CoreAlignDbContext context)
    {
        _context = context;
    }

    public Task<ReportDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Set<ReportDefinition>().FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ReportDefinition>> ListAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _context.Set<ReportDefinition>()
            .AsNoTracking()
            .OrderByDescending(r => r.UpdatedAtUtc)
            .ToListAsync(cancellationToken);
        return rows;
    }

    public async Task AddAsync(ReportDefinition definition, CancellationToken cancellationToken = default)
    {
        await _context.Set<ReportDefinition>().AddAsync(definition, cancellationToken);
    }

    public void Update(ReportDefinition definition) => _context.Set<ReportDefinition>().Update(definition);

    public void Remove(ReportDefinition definition) => _context.Set<ReportDefinition>().Remove(definition);
}
