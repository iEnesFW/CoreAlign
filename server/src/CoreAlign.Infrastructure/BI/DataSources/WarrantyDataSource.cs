using CoreAlign.Application.BI;
using CoreAlign.Application.BI.DataSources;
using CoreAlign.Domain.Entities.Reporting;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.BI.DataSources;

public sealed class WarrantyDataSource : IBIDataSourceAggregator
{
    private readonly CoreAlignDbContext _db;

    public WarrantyDataSource(CoreAlignDbContext db)
    {
        _db = db;
    }

    public BIDataSource Source => BIDataSource.Warranty;

    public async Task<BIResultDto> ExecuteAsync(BIQueryConfigDto config, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(config);
        var grouped = await _db.WarrantyContracts.AsNoTracking()
            .GroupBy(w => w.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var rows = grouped
            .Select(g => (IDictionary<string, object?>)new Dictionary<string, object?>
            {
                ["status"] = g.Status.ToString(),
                ["count"] = g.Count,
            })
            .ToList();
        var columns = new List<BIResultColumnDto>
        {
            new("status", "Status", "string"),
            new("count", "Contracts", "integer"),
        };
        return new BIResultDto(columns, rows, rows.Count);
    }
}
