using System.Globalization;
using CoreAlign.Application.BI;
using CoreAlign.Application.BI.DataSources;
using CoreAlign.Domain.Entities.Reporting;
using CoreAlign.Domain.Enums;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.BI.DataSources;

public sealed class CashDataSource : IBIDataSourceAggregator
{
    private readonly CoreAlignDbContext _db;

    public CashDataSource(CoreAlignDbContext db)
    {
        _db = db;
    }

    public BIDataSource Source => BIDataSource.Cash;

    public async Task<BIResultDto> ExecuteAsync(BIQueryConfigDto config, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(config);
        var query = _db.Payments.AsNoTracking().AsQueryable();
        if (config.FromUtc.HasValue)
        {
            var from = DateTime.SpecifyKind(config.FromUtc.Value, DateTimeKind.Utc);
            query = query.Where(p => p.PaymentDate >= from);
        }
        if (config.ToUtc.HasValue)
        {
            var to = DateTime.SpecifyKind(config.ToUtc.Value, DateTimeKind.Utc);
            query = query.Where(p => p.PaymentDate <= to);
        }

        var bucketed = await query
            .GroupBy(p => new { p.PaymentDate.Year, p.PaymentDate.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Inflow = g.Where(x => x.Direction == PaymentDirection.CustomerReceipt).Sum(x => (decimal?)x.Amount) ?? 0m,
                Outflow = g.Where(x => x.Direction != PaymentDirection.CustomerReceipt).Sum(x => (decimal?)x.Amount) ?? 0m,
            })
            .OrderBy(b => b.Year).ThenBy(b => b.Month)
            .ToListAsync(cancellationToken);

        var rows = bucketed
            .Select(b => (IDictionary<string, object?>)new Dictionary<string, object?>
            {
                ["bucket"] = string.Create(CultureInfo.InvariantCulture, $"{b.Year:0000}-{b.Month:00}"),
                ["inflow"] = b.Inflow,
                ["outflow"] = b.Outflow,
            })
            .ToList();

        var columns = new List<BIResultColumnDto>
        {
            new("bucket", "Period", "string"),
            new("inflow", "Inflow", "decimal"),
            new("outflow", "Outflow", "decimal"),
        };
        return new BIResultDto(columns, rows, rows.Count);
    }
}
