using CoreAlign.Application.Mrp;
using CoreAlign.Domain.Enums;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Mrp;

public sealed class AbcUsageDataLoader : IAbcUsageDataLoader
{
    public const int UsageWindowDays = 365;

    private readonly CoreAlignDbContext _db;

    public AbcUsageDataLoader(CoreAlignDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<AbcProductUsage>> LoadAsync(DateTime asOfUtc, CancellationToken cancellationToken = default)
    {
        var anchorUtc = DateTime.SpecifyKind(asOfUtc, DateTimeKind.Utc);

        var products = await _db.Products
            .Where(p => p.IsStockTracked && p.Status == ProductStatus.Active)
            .ToListAsync(cancellationToken);

        if (products.Count == 0)
        {
            return Array.Empty<AbcProductUsage>();
        }

        var productIds = products.Select(p => p.Id).ToList();
        var fromUtc = anchorUtc.AddDays(-UsageWindowDays);

        var shippedByProduct = await _db.OrderLines.AsNoTracking()
            .Where(l => productIds.Contains(l.ProductId)
                && l.QuantityShipped > 0m
                && l.Order.OrderDate >= fromUtc
                && l.Order.OrderDate <= anchorUtc)
            .GroupBy(l => l.ProductId)
            .Select(g => new { ProductId = g.Key, ShippedQty = g.Sum(x => x.QuantityShipped) })
            .ToDictionaryAsync(x => x.ProductId, x => x.ShippedQty, cancellationToken);

        var result = new List<AbcProductUsage>(products.Count);
        foreach (var product in products)
        {
            var shippedQty = shippedByProduct.TryGetValue(product.Id, out var qty) ? qty : 0m;
            var avgDailyDemand = shippedQty / UsageWindowDays;
            var unitCost = product.LastPurchaseCost > 0m ? product.LastPurchaseCost : product.StandardCost;
            var annualUsageValue = avgDailyDemand * unitCost * UsageWindowDays;
            result.Add(new AbcProductUsage(product, annualUsageValue));
        }

        return result;
    }
}
