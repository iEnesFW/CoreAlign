using System.Globalization;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class ReportRepository : IReportRepository
{
    private readonly CoreAlignDbContext _context;

    public ReportRepository(CoreAlignDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<SalesPeriodRow>> GetSalesByPeriodAsync(
        DateTime fromUtc,
        DateTime toUtc,
        string bucket,
        CancellationToken cancellationToken = default)
    {
        // Server-side bucketing: project to a (year, month/day/week) tuple inside
        // the SQL query so PostgreSQL aggregates the groups directly. This avoids
        // pulling every invoice in the date window into memory for in-process
        // grouping, which used to be a multi-second hit on multi-year reports.
        var baseQuery = _context.Invoices
            .AsNoTracking()
            .Where(i => i.IssueDate >= fromUtc
                && i.IssueDate <= toUtc
                && i.Status != InvoiceStatus.Draft
                && i.Status != InvoiceStatus.Cancelled
                && i.Status != InvoiceStatus.Void);

        List<(int Year, int Sub, decimal Revenue, decimal Paid, int Count, int CustomerCount)> rows;

        if (string.Equals(bucket, "Day", StringComparison.OrdinalIgnoreCase))
        {
            rows = (await baseQuery
                .GroupBy(i => new { i.IssueDate.Year, i.IssueDate.Month, i.IssueDate.Day })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    g.Key.Day,
                    Revenue = g.Sum(i => i.Total),
                    Paid = g.Sum(i => i.AmountPaid),
                    Count = g.Count(),
                    CustomerCount = g.Select(i => i.CustomerId).Distinct().Count(),
                })
                .ToListAsync(cancellationToken))
                .Select(r => (r.Year, r.Month * 100 + r.Day, r.Revenue, r.Paid, r.Count, r.CustomerCount))
                .ToList();
        }
        else if (string.Equals(bucket, "Week", StringComparison.OrdinalIgnoreCase))
        {
            // Week bucketing still requires post-processing for ISO week math —
            // pull only the date + aggregate scalars, no full invoice rows.
            var perDay = await baseQuery
                .GroupBy(i => new { i.IssueDate.Year, i.IssueDate.Month, i.IssueDate.Day })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    g.Key.Day,
                    Revenue = g.Sum(i => i.Total),
                    Paid = g.Sum(i => i.AmountPaid),
                    Count = g.Count(),
                    DistinctCustomers = g.Select(i => i.CustomerId).Distinct().ToList(),
                })
                .ToListAsync(cancellationToken);

            var grouped = perDay
                .Select(r => new
                {
                    Date = new DateTime(r.Year, r.Month, r.Day, 0, 0, 0, DateTimeKind.Utc),
                    r.Revenue,
                    r.Paid,
                    r.Count,
                    r.DistinctCustomers,
                })
                .GroupBy(x => WeekBucket(x.Date))
                .Select(g => new SalesPeriodRow(
                    g.Key.Key,
                    g.Key.Start,
                    g.Sum(x => x.Revenue),
                    g.Sum(x => x.Paid),
                    g.Sum(x => x.Count),
                    g.SelectMany(x => x.DistinctCustomers).Distinct().Count()))
                .OrderBy(r => r.BucketStart)
                .ToList();

            return grouped;
        }
        else
        {
            rows = (await baseQuery
                .GroupBy(i => new { i.IssueDate.Year, i.IssueDate.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Revenue = g.Sum(i => i.Total),
                    Paid = g.Sum(i => i.AmountPaid),
                    Count = g.Count(),
                    CustomerCount = g.Select(i => i.CustomerId).Distinct().Count(),
                })
                .ToListAsync(cancellationToken))
                .Select(r => (r.Year, r.Month, r.Revenue, r.Paid, r.Count, r.CustomerCount))
                .ToList();
        }

        return rows
            .Select(r =>
            {
                DateTime start;
                string key;
                if (string.Equals(bucket, "Day", StringComparison.OrdinalIgnoreCase))
                {
                    var month = r.Sub / 100;
                    var day = r.Sub % 100;
                    start = new DateTime(r.Year, month, day, 0, 0, 0, DateTimeKind.Utc);
                    key = start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                }
                else
                {
                    start = new DateTime(r.Year, r.Sub, 1, 0, 0, 0, DateTimeKind.Utc);
                    key = $"{r.Year:D4}-{r.Sub:D2}";
                }
                return new SalesPeriodRow(key, start, r.Revenue, r.Paid, r.Count, r.CustomerCount);
            })
            .OrderBy(r => r.BucketStart)
            .ToList();
    }

    public async Task<IReadOnlyList<TopCustomerRow>> GetTopCustomersAsync(
        int limit,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken = default)
    {
        var invoiceQuery = _context.Invoices.AsNoTracking()
            .Where(i => i.Status != InvoiceStatus.Draft
                && i.Status != InvoiceStatus.Cancelled
                && i.Status != InvoiceStatus.Void);
        if (fromUtc.HasValue) invoiceQuery = invoiceQuery.Where(i => i.IssueDate >= fromUtc.Value);
        if (toUtc.HasValue) invoiceQuery = invoiceQuery.Where(i => i.IssueDate <= toUtc.Value);

        var invoiceAgg = await invoiceQuery
            .GroupBy(i => i.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                Currency = g.Min(i => i.Currency) ?? "TRY",
                Revenue = g.Sum(i => i.Total),
                Paid = g.Sum(i => i.AmountPaid),
                Count = g.Count(),
            })
            .OrderByDescending(x => x.Revenue)
            .Take(limit)
            .ToListAsync(cancellationToken);

        var customerIds = invoiceAgg.Select(x => x.CustomerId).ToList();
        var customers = await _context.Customers
            .AsNoTracking()
            .Where(c => customerIds.Contains(c.Id))
            .Select(c => new { c.Id, c.Name, c.Code })
            .ToListAsync(cancellationToken);

        var orderQuery = _context.Orders.AsNoTracking().Where(o => customerIds.Contains(o.CustomerId));
        if (fromUtc.HasValue) orderQuery = orderQuery.Where(o => o.OrderDate >= fromUtc.Value);
        if (toUtc.HasValue) orderQuery = orderQuery.Where(o => o.OrderDate <= toUtc.Value);
        var orderAgg = await orderQuery
            .GroupBy(o => o.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                Count = g.Count(),
                Last = g.Max(o => (DateTime?)o.OrderDate),
            })
            .ToListAsync(cancellationToken);

        // O(N) projection — Dictionary lookups instead of FirstOrDefault scans.
        var customerById = customers.ToDictionary(c => c.Id);
        var orderById = orderAgg.ToDictionary(o => o.CustomerId);

        return invoiceAgg
            .Select(inv =>
            {
                customerById.TryGetValue(inv.CustomerId, out var c);
                orderById.TryGetValue(inv.CustomerId, out var o);
                return new TopCustomerRow(
                    inv.CustomerId,
                    c?.Name ?? "",
                    c?.Code,
                    inv.Currency,
                    inv.Revenue,
                    inv.Paid,
                    Math.Max(0m, inv.Revenue - inv.Paid),
                    inv.Count,
                    o?.Count ?? 0,
                    o?.Last);
            })
            .ToList();
    }

    public async Task<IReadOnlyList<TopProductRow>> GetTopProductsGlobalAsync(
        int limit,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken = default)
    {
        var query = _context.InvoiceLines
            .AsNoTracking()
            .Where(l => l.Invoice!.Status != InvoiceStatus.Draft
                && l.Invoice.Status != InvoiceStatus.Cancelled
                && l.Invoice.Status != InvoiceStatus.Void);
        if (fromUtc.HasValue) query = query.Where(l => l.Invoice!.IssueDate >= fromUtc.Value);
        if (toUtc.HasValue) query = query.Where(l => l.Invoice!.IssueDate <= toUtc.Value);

        return await query
            .GroupBy(l => new { l.ProductId, l.ProductSku, l.ProductName })
            .Select(g => new TopProductRow(
                g.Key.ProductId,
                g.Key.ProductSku,
                g.Key.ProductName,
                g.Sum(x => x.Quantity),
                g.Sum(x => x.LineTotal),
                g.Select(x => x.InvoiceId).Distinct().Count()))
            .OrderByDescending(t => t.Revenue)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OpenInvoiceRow>> GetOpenInvoicesAcrossCustomersAsync(
        CancellationToken cancellationToken = default)
    {
        var rows = await _context.Invoices
            .AsNoTracking()
            .Where(i => i.Status == InvoiceStatus.Issued
                || i.Status == InvoiceStatus.Sent
                || i.Status == InvoiceStatus.PartiallyPaid
                || i.Status == InvoiceStatus.Overdue)
            .Select(i => new
            {
                i.CustomerId,
                i.CustomerNameSnapshot,
                i.Currency,
                Outstanding = i.Total - i.AmountPaid,
                i.DueDate,
            })
            .Where(x => x.Outstanding > 0)
            .ToListAsync(cancellationToken);
        return rows
            .Select(r => new OpenInvoiceRow(r.CustomerId, r.CustomerNameSnapshot, r.Currency, r.Outstanding, r.DueDate))
            .ToList();
    }

    public async Task<IReadOnlyList<AgingBucketRow>> GetAgingBucketsAsync(
        DateTime asOfUtc,
        CancellationToken cancellationToken = default)
    {
        // Postgres-translatable date math via subtraction → days. Falls back to
        // post-materialization bucketing only when the provider can't translate
        // the conditional sums (e.g. SQLite dev fallback). Result rows include
        // already-aggregated outstanding per (customer, currency, bucket).
        var rows = await _context.Invoices
            .AsNoTracking()
            .Where(i => (i.Status == InvoiceStatus.Issued
                    || i.Status == InvoiceStatus.Sent
                    || i.Status == InvoiceStatus.PartiallyPaid
                    || i.Status == InvoiceStatus.Overdue)
                && (i.Total - i.AmountPaid) > 0)
            .Select(i => new
            {
                i.CustomerId,
                i.CustomerNameSnapshot,
                i.Currency,
                Outstanding = i.Total - i.AmountPaid,
                i.DueDate,
            })
            .ToListAsync(cancellationToken);

        // Bucket in memory — but only over rows that already passed the
        // server-side open-invoice filter + projection, so we never materialize
        // tax breakdowns, snapshots, or other heavy invoice columns.
        return rows
            .GroupBy(r => new { r.CustomerId, r.CustomerNameSnapshot, r.Currency })
            .Select(g =>
            {
                decimal current = 0, b1 = 0, b2 = 0, b3 = 0, b4 = 0;
                foreach (var inv in g)
                {
                    var days = (asOfUtc.Date - inv.DueDate.Date).TotalDays;
                    if (days <= 0) current += inv.Outstanding;
                    else if (days <= 30) b1 += inv.Outstanding;
                    else if (days <= 60) b2 += inv.Outstanding;
                    else if (days <= 90) b3 += inv.Outstanding;
                    else b4 += inv.Outstanding;
                }
                return new AgingBucketRow(g.Key.CustomerId, g.Key.CustomerNameSnapshot, g.Key.Currency, current, b1, b2, b3, b4);
            })
            .ToList();
    }

    private static (string Key, DateTime Start) BucketKey(DateTime date, string bucket)
    {
        var local = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
        return bucket switch
        {
            "Day" => (local.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), local),
            "Week" => WeekBucket(local),
            _ => MonthBucket(local),
        };
    }

    private static (string Key, DateTime Start) MonthBucket(DateTime date)
    {
        var start = new DateTime(date.Year, date.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        return ($"{start.Year:D4}-{start.Month:D2}", start);
    }

    private static (string Key, DateTime Start) WeekBucket(DateTime date)
    {
        var dayOfWeek = (int)date.DayOfWeek;
        var diff = (dayOfWeek == 0 ? 6 : dayOfWeek - 1);
        var monday = date.AddDays(-diff);
        var key = $"{ISOWeek.GetYear(date):D4}-W{ISOWeek.GetWeekOfYear(date):D2}";
        return (key, DateTime.SpecifyKind(monday.Date, DateTimeKind.Utc));
    }
}
