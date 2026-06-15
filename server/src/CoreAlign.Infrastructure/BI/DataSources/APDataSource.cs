using CoreAlign.Application.BI;
using CoreAlign.Application.BI.DataSources;
using CoreAlign.Domain.Entities.Reporting;
using CoreAlign.Domain.Enums;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.BI.DataSources;

public sealed class APDataSource : IBIDataSourceAggregator
{
    private readonly CoreAlignDbContext _db;

    public APDataSource(CoreAlignDbContext db)
    {
        _db = db;
    }

    public BIDataSource Source => BIDataSource.AP;

    public async Task<BIResultDto> ExecuteAsync(BIQueryConfigDto config, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(config);
        var today = DateTime.UtcNow.Date;
        var thirtyDays = today.AddDays(-30);
        var sixtyDays = today.AddDays(-60);
        var ninetyDays = today.AddDays(-90);

        var openBills = _db.VendorBills.AsNoTracking()
            .Where(b => b.Status != VendorBillStatus.Paid && b.Status != VendorBillStatus.Cancelled
                && b.DueDate.HasValue && (b.Total - b.AmountPaid) > 0);

        var aggregated = await openBills
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Bucket0_30 = g.Where(b => b.DueDate!.Value >= thirtyDays).Sum(b => (decimal?)(b.Total - b.AmountPaid)) ?? 0m,
                Bucket31_60 = g.Where(b => b.DueDate!.Value < thirtyDays && b.DueDate!.Value >= sixtyDays).Sum(b => (decimal?)(b.Total - b.AmountPaid)) ?? 0m,
                Bucket61_90 = g.Where(b => b.DueDate!.Value < sixtyDays && b.DueDate!.Value >= ninetyDays).Sum(b => (decimal?)(b.Total - b.AmountPaid)) ?? 0m,
                Bucket90Plus = g.Where(b => b.DueDate!.Value < ninetyDays).Sum(b => (decimal?)(b.Total - b.AmountPaid)) ?? 0m,
            })
            .FirstOrDefaultAsync(cancellationToken);

        var totals = new Dictionary<string, decimal>
        {
            ["0-30"] = aggregated?.Bucket0_30 ?? 0m,
            ["31-60"] = aggregated?.Bucket31_60 ?? 0m,
            ["61-90"] = aggregated?.Bucket61_90 ?? 0m,
            ["90+"] = aggregated?.Bucket90Plus ?? 0m,
        };

        var buckets = new[] { "0-30", "31-60", "61-90", "90+" };
        var rows = buckets
            .Select(name => (IDictionary<string, object?>)new Dictionary<string, object?>
            {
                ["bucket"] = name,
                ["amount"] = totals[name],
            })
            .ToList();
        var columns = new List<BIResultColumnDto>
        {
            new("bucket", "Aging Bucket", "string"),
            new("amount", "Open Amount", "decimal"),
        };
        return new BIResultDto(columns, rows, rows.Count);
    }
}
