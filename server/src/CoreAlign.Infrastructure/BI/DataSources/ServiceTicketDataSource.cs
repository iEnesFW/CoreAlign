using CoreAlign.Application.BI;
using CoreAlign.Application.BI.DataSources;
using CoreAlign.Domain.Entities.Reporting;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.BI.DataSources;

public sealed class ServiceTicketDataSource : IBIDataSourceAggregator
{
    private readonly CoreAlignDbContext _db;

    public ServiceTicketDataSource(CoreAlignDbContext db)
    {
        _db = db;
    }

    public BIDataSource Source => BIDataSource.Service;

    public async Task<BIResultDto> ExecuteAsync(BIQueryConfigDto config, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(config);

        // Single server-side aggregation: COUNT(*) over all tickets per status, and the
        // average resolution time as a conditional AVG over resolved tickets only
        // (avg(CASE WHEN resolved THEN epoch_hours END) — AVG ignores the NULLs of
        // unresolved rows). Never materializes the full ticket set into memory; the DB
        // returns one row per status. Verified translatable on Npgsql via ToQueryString.
        var grouped = await _db.ServiceTickets.AsNoTracking()
            .GroupBy(t => t.Status)
            .Select(g => new
            {
                Status = g.Key,
                Count = g.Count(),
                AvgResolutionHours = g.Average(t => t.ResolvedAtUtc.HasValue
                    ? (double?)(t.ResolvedAtUtc.Value - t.ReportedAtUtc).TotalHours
                    : null),
            })
            .ToListAsync(cancellationToken);

        var rows = grouped
            .Select(s => (IDictionary<string, object?>)new Dictionary<string, object?>
            {
                ["status"] = s.Status.ToString(),
                ["count"] = s.Count,
                ["avgResolutionHours"] = s.AvgResolutionHours ?? 0d,
            })
            .ToList();

        var columns = new List<BIResultColumnDto>
        {
            new("status", "Status", "string"),
            new("count", "Tickets", "integer"),
            new("avgResolutionHours", "Avg Resolution (hr)", "decimal"),
        };
        return new BIResultDto(columns, rows, rows.Count);
    }
}
