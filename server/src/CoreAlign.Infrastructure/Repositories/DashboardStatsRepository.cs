using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class DashboardStatsRepository : IDashboardStatsRepository
{
    private readonly IDbContextFactory<CoreAlignDbContext> _contextFactory;

    public DashboardStatsRepository(IDbContextFactory<CoreAlignDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<int> GetCustomerCountAsync(CancellationToken cancellationToken = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await ctx.Customers.AsNoTracking().CountAsync(cancellationToken);
    }

    public async Task<int> GetActiveProductCountAsync(CancellationToken cancellationToken = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await ctx.Products
            .AsNoTracking()
            .CountAsync(
                p => p.Status == ProductStatus.Active || p.Status == ProductStatus.New,
                cancellationToken);
    }

    public async Task<Dictionary<string, int>> GetOrderCountByStatusAsync(CancellationToken cancellationToken = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var grouped = await ctx.Orders
            .AsNoTracking()
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var result = Enum.GetNames(typeof(OrderStatus)).ToDictionary(name => name, _ => 0);
        foreach (var item in grouped)
        {
            result[item.Status.ToString()] = item.Count;
        }
        return result;
    }

    public async Task<decimal> GetTotalSalesAsync(CancellationToken cancellationToken = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await ctx.Orders
            .AsNoTracking()
            .Where(o => o.Status == OrderStatus.Closed || o.Status == OrderStatus.Shipped)
            .SumAsync(o => (decimal?)o.Total, cancellationToken) ?? 0m;
    }

    public async Task<List<Product>> GetLowStockProductsAsync(int take, CancellationToken cancellationToken = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await ctx.Products
            .AsNoTracking()
            .Where(p => p.Status == ProductStatus.Active || p.Status == ProductStatus.New)
            .OrderBy(p => p.StockQuantity)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Order>> GetRecentOrdersAsync(int take, CancellationToken cancellationToken = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await ctx.Orders
            .AsNoTracking()
            .Include(o => o.Customer)
            .OrderByDescending(o => o.OrderDate)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<(decimal Outstanding, decimal CollectedThisMonth, int OpenCount)> GetInvoiceStatsAsync(CancellationToken cancellationToken = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var outstandingAgg = await ctx.Invoices
            .AsNoTracking()
            .Where(i => i.Status == InvoiceStatus.Issued)
            .GroupBy(_ => 1)
            .Select(g => new { Total = (decimal?)g.Sum(i => i.Total), Count = g.Count() })
            .FirstOrDefaultAsync(cancellationToken);

        var outstanding = outstandingAgg?.Total ?? 0m;
        var openCount = outstandingAgg?.Count ?? 0;

        var collected = await ctx.Invoices
            .AsNoTracking()
            .Where(i => i.Status == InvoiceStatus.Paid && i.PaidAtUtc >= monthStart)
            .SumAsync(i => (decimal?)i.Total, cancellationToken) ?? 0m;

        return (outstanding, collected, openCount);
    }

    public async Task<List<(DateTime Date, decimal Total)>> GetSalesTrendAsync(int days, CancellationToken cancellationToken = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var totalDays = Math.Clamp(days, 1, 365);
        var since = DateTime.UtcNow.Date.AddDays(-totalDays + 1);

        var raw = await ctx.Orders
            .AsNoTracking()
            .Where(o => (o.Status == OrderStatus.Shipped || o.Status == OrderStatus.Closed) && o.OrderDate >= since)
            .GroupBy(o => o.OrderDate.Date)
            .Select(g => new { Date = g.Key, Total = g.Sum(o => o.Total) })
            .ToListAsync(cancellationToken);

        var byDate = raw.ToDictionary(r => r.Date, r => r.Total);
        var points = new List<(DateTime, decimal)>(totalDays);
        for (var i = 0; i < totalDays; i++)
        {
            var date = since.AddDays(i);
            points.Add((date, byDate.TryGetValue(date, out var total) ? total : 0m));
        }
        return points;
    }
}
