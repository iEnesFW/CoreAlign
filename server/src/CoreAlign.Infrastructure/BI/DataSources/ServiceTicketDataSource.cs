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

        var statusCounts = await _db.ServiceTickets.AsNoTracking()
            .GroupBy(t => t.Status)
            .Select(g => new
            {
                Status = g.Key,
                Count = g.Count(),
            })
            .ToListAsync(cancellationToken);

        var resolved = await _db.ServiceTickets.AsNoTracking()
            .Where(t => t.ResolvedAtUtc.HasValue)
            .Select(t => new
            {
                t.Status,
                t.ReportedAtUtc,
                ResolvedAtUtc = t.ResolvedAtUtc!.Value,
            })
            .ToListAsync(cancellationToken);

        var avgByStatus = resolved
            .GroupBy(r => r.Status)
            .ToDictionary(
                g => g.Key,
                g => g.Average(r => (r.ResolvedAtUtc - r.ReportedAtUtc).TotalHours));

        var rows = statusCounts
            .Select(s => (IDictionary<string, object?>)new Dictionary<string, object?>
            {
                ["status"] = s.Status.ToString(),
                ["count"] = s.Count,
                ["avgResolutionHours"] = avgByStatus.TryGetValue(s.Status, out var avg) ? avg : 0d,
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
