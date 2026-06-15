using CoreAlign.Application.Mrp.Distribution;
using CoreAlign.Domain.Enums;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Mrp.Distribution;

public sealed class DistributionDataLoader : IDistributionDataLoader
{
    private readonly CoreAlignDbContext _db;

    public DistributionDataLoader(CoreAlignDbContext db) => _db = db;

    public async Task<DistributionContext> LoadAsync(CancellationToken cancellationToken = default)
    {
        var products = await _db.Products.AsNoTracking()
            .Where(p => p.IsStockTracked && p.Status == ProductStatus.Active)
            .Select(p => new { p.Id, p.Sku, p.Name })
            .ToListAsync(cancellationToken);

        var productInfo = products.ToDictionary(
            p => p.Id,
            p => new DistributionProductInfo(p.Id, p.Sku, p.Name));

        var warehouses = await _db.Warehouses.AsNoTracking()
            .Where(w => w.IsActive)
            .Select(w => new { w.Id, w.Code, w.Name, w.Type, w.IsDefault })
            .ToListAsync(cancellationToken);

        var warehouseInfo = warehouses.ToDictionary(
            w => w.Id,
            w => new DistributionWarehouseInfo(w.Id, w.Code, w.Name));

        var warehouseSnapshots = warehouses
            .Select(w => new DistributionWarehouseSnapshot(w.Id, w.IsDefault, w.Type))
            .ToList();

        if (productInfo.Count == 0 || warehouseSnapshots.Count == 0)
        {
            return new DistributionContext(
                new DistributionInput(
                    Array.Empty<DistributionProductSnapshot>(),
                    warehouseSnapshots,
                    Array.Empty<WarehouseStockSnapshot>()),
                productInfo,
                warehouseInfo);
        }

        var productIds = productInfo.Keys.ToHashSet();

        var stockRows = await _db.StockItems.AsNoTracking()
            .Where(s => productIds.Contains(s.ProductId))
            .GroupBy(s => new { s.ProductId, s.WarehouseId })
            .Select(g => new StockRollupRow(
                g.Key.ProductId,
                g.Key.WarehouseId,
                g.Sum(x => x.OnHand),
                g.Sum(x => x.Reserved)))
            .ToListAsync(cancellationToken);

        var demandRows = await _db.OrderLines.AsNoTracking()
            .Where(l => productIds.Contains(l.ProductId)
                && l.Status != OrderLineStatus.Cancelled
                && l.Status != OrderLineStatus.Shipped
                && l.Status != OrderLineStatus.Invoiced
                && l.QuantityAllocated > l.QuantityShipped)
            .GroupBy(l => new { l.ProductId, l.WarehouseId })
            .Select(g => new DemandRollupRow(
                g.Key.ProductId,
                g.Key.WarehouseId,
                g.Sum(x => x.QuantityAllocated - x.QuantityShipped)))
            .ToListAsync(cancellationToken);

        var stock = BuildStockSnapshots(stockRows, demandRows);

        var input = new DistributionInput(
            productInfo.Keys.Select(id => new DistributionProductSnapshot(id)).ToList(),
            warehouseSnapshots,
            stock);

        return new DistributionContext(input, productInfo, warehouseInfo);
    }

    private static List<WarehouseStockSnapshot> BuildStockSnapshots(
        IReadOnlyList<StockRollupRow> stockRows,
        IReadOnlyList<DemandRollupRow> demandRows)
    {
        var map = new Dictionary<(Guid ProductId, Guid? WarehouseId), (decimal OnHand, decimal Reserved, decimal Demand)>();

        foreach (var row in stockRows)
        {
            var key = (row.ProductId, (Guid?)row.WarehouseId);
            var current = map.GetValueOrDefault(key);
            map[key] = (current.OnHand + row.OnHand, current.Reserved + row.Reserved, current.Demand);
        }

        foreach (var row in demandRows)
        {
            var key = (row.ProductId, row.WarehouseId);
            var current = map.GetValueOrDefault(key);
            map[key] = (current.OnHand, current.Reserved, current.Demand + row.Demand);
        }

        return map
            .Select(kvp => new WarehouseStockSnapshot(
                kvp.Key.ProductId,
                kvp.Key.WarehouseId ?? Guid.Empty,
                kvp.Value.OnHand,
                kvp.Value.Reserved,
                kvp.Value.Demand))
            .ToList();
    }

    private sealed record StockRollupRow(Guid ProductId, Guid WarehouseId, decimal OnHand, decimal Reserved);

    private sealed record DemandRollupRow(Guid ProductId, Guid? WarehouseId, decimal Demand);
}
