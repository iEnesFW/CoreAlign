using CoreAlign.Application.BI;
using CoreAlign.Application.BI.DataSources;
using CoreAlign.Domain.Entities.Reporting;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.BI.DataSources;

public sealed class ARDataSource : IBIDataSourceAggregator
{
    private readonly CoreAlignDbContext _db;

    public ARDataSource(CoreAlignDbContext db)
    {
        _db = db;
    }

    public BIDataSource Source => BIDataSource.AR;

    public async Task<BIResultDto> ExecuteAsync(BIQueryConfigDto config, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(config);
        var today = DateTime.UtcNow.Date;
        var thirtyDays = today.AddDays(-30);
        var sixtyDays = today.AddDays(-60);
        var ninetyDays = today.AddDays(-90);

        var openInvoices = _db.Invoices.AsNoTracking()
            .Where(i => i.PaidAtUtc == null && i.CancelledAtUtc == null && i.VoidedAtUtc == null && (i.Total - i.AmountPaid) > 0);

        var aggregated = await openInvoices
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Bucket0_30 = g.Where(i => i.DueDate >= thirtyDays).Sum(i => (decimal?)(i.Total - i.AmountPaid)) ?? 0m,
                Bucket31_60 = g.Where(i => i.DueDate < thirtyDays && i.DueDate >= sixtyDays).Sum(i => (decimal?)(i.Total - i.AmountPaid)) ?? 0m,
                Bucket61_90 = g.Where(i => i.DueDate < sixtyDays && i.DueDate >= ninetyDays).Sum(i => (decimal?)(i.Total - i.AmountPaid)) ?? 0m,
                Bucket90Plus = g.Where(i => i.DueDate < ninetyDays).Sum(i => (decimal?)(i.Total - i.AmountPaid)) ?? 0m,
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
            .Select(b => (IDictionary<string, object?>)new Dictionary<string, object?>
            {
                ["bucket"] = b,
                ["amount"] = totals[b],
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
