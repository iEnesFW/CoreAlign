using System.Globalization;
using CoreAlign.Application.BI;
using CoreAlign.Application.BI.DataSources;
using CoreAlign.Domain.Entities.Reporting;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.BI.DataSources;

public sealed class SalesDataSource : IBIDataSourceAggregator
{
    private readonly CoreAlignDbContext _db;

    public SalesDataSource(CoreAlignDbContext db)
    {
        _db = db;
    }

    public BIDataSource Source => BIDataSource.Sales;

    public async Task<BIResultDto> ExecuteAsync(BIQueryConfigDto config, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(config);
        var query = _db.Orders.AsNoTracking().AsQueryable();
        if (config.FromUtc.HasValue)
        {
            var from = DateTime.SpecifyKind(config.FromUtc.Value, DateTimeKind.Utc);
            query = query.Where(o => o.OrderDate >= from);
        }
        if (config.ToUtc.HasValue)
        {
            var to = DateTime.SpecifyKind(config.ToUtc.Value, DateTimeKind.Utc);
            query = query.Where(o => o.OrderDate <= to);
        }

        var bucketed = await query
            .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                OrderCount = g.Count(),
                TotalSales = g.Sum(x => x.Total),
                UniqueCustomers = g.Select(x => x.CustomerId).Distinct().Count(),
            })
            .OrderBy(b => b.Year).ThenBy(b => b.Month)
            .ToListAsync(cancellationToken);

        var rows = bucketed
            .Select(b => (IDictionary<string, object?>)new Dictionary<string, object?>
            {
                ["bucket"] = string.Create(CultureInfo.InvariantCulture, $"{b.Year:0000}-{b.Month:00}"),
                ["orderCount"] = b.OrderCount,
                ["totalSales"] = b.TotalSales,
                ["uniqueCustomers"] = b.UniqueCustomers,
            })
            .ToList();

        var columns = new List<BIResultColumnDto>
        {
            new("bucket", "Period", "string"),
            new("orderCount", "Orders", "integer"),
            new("totalSales", "Total Sales", "decimal"),
            new("uniqueCustomers", "Unique Customers", "integer"),
        };
        return new BIResultDto(columns, rows, rows.Count);
    }
}
