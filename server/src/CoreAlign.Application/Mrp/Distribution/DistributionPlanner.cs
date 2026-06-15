using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Mrp.Distribution;

public sealed class DistributionPlanner : IDistributionPlanner
{
    public DistributionPlan Plan(DistributionInput input)
    {
        var defaultWarehouseId = ResolveDefaultWarehouseId(input.Warehouses);
        var warehouseIds = input.Warehouses.Select(w => w.WarehouseId).ToHashSet();

        var stockByProduct = input.Stock
            .GroupBy(s => s.ProductId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var netPositions = new List<WarehouseNetPosition>();
        var transfers = new List<TransferSuggestion>();
        var externalNeeds = new List<ExternalReplenishmentNeed>();

        foreach (var product in input.Products)
        {
            if (!stockByProduct.TryGetValue(product.ProductId, out var rows))
            {
                continue;
            }

            var demandByWarehouse = AttributeDemand(rows, warehouseIds, defaultWarehouseId);
            var positions = BuildNetPositions(product.ProductId, rows, demandByWarehouse, warehouseIds);
            netPositions.AddRange(positions);

            var (productTransfers, productExternalNeeds) = SuggestTransfers(product.ProductId, positions);
            transfers.AddRange(productTransfers);
            externalNeeds.AddRange(productExternalNeeds);
        }

        return new DistributionPlan(netPositions, transfers, externalNeeds);
    }

    private static Guid? ResolveDefaultWarehouseId(IReadOnlyList<DistributionWarehouseSnapshot> warehouses)
    {
        var explicitDefault = warehouses
            .Where(w => w.IsDefault)
            .OrderBy(w => w.WarehouseId)
            .FirstOrDefault();
        if (explicitDefault is not null)
        {
            return explicitDefault.WarehouseId;
        }

        var main = warehouses
            .Where(w => w.Type == WarehouseType.Main)
            .OrderBy(w => w.WarehouseId)
            .FirstOrDefault();
        return main?.WarehouseId;
    }

    private static Dictionary<Guid, decimal> AttributeDemand(
        IReadOnlyList<WarehouseStockSnapshot> rows,
        IReadOnlySet<Guid> warehouseIds,
        Guid? defaultWarehouseId)
    {
        var demandByWarehouse = new Dictionary<Guid, decimal>();
        foreach (var row in rows)
        {
            if (row.Demand <= 0m)
            {
                continue;
            }

            var targetWarehouseId = warehouseIds.Contains(row.WarehouseId)
                ? row.WarehouseId
                : defaultWarehouseId;

            if (targetWarehouseId is not { } warehouseId)
            {
                continue;
            }

            demandByWarehouse[warehouseId] = demandByWarehouse.GetValueOrDefault(warehouseId) + row.Demand;
        }
        return demandByWarehouse;
    }

    private static List<WarehouseNetPosition> BuildNetPositions(
        Guid productId,
        IReadOnlyList<WarehouseStockSnapshot> rows,
        IReadOnlyDictionary<Guid, decimal> demandByWarehouse,
        IReadOnlySet<Guid> warehouseIds)
    {
        var availableByWarehouse = new Dictionary<Guid, decimal>();
        foreach (var row in rows)
        {
            if (!warehouseIds.Contains(row.WarehouseId))
            {
                continue;
            }
            // Physical on-hand is the available stock; do NOT subtract Reserved. Reserved already
            // represents the allocated open order lines (StockItem.Reserve on allocation), which is
            // the SAME quantity 'demand' (QuantityAllocated - QuantityShipped) carries. Subtracting
            // both double-counts committed demand (net = OnHand - 2*demand), inventing phantom
            // shortfalls and over-suggesting transfers. Mirrors the planning engine's physical-OnHand
            // + allocated-as-gross invariant (MRP-BUG-6).
            var available = row.OnHand;
            availableByWarehouse[row.WarehouseId] =
                availableByWarehouse.GetValueOrDefault(row.WarehouseId) + available;
        }

        var relevantWarehouseIds = availableByWarehouse.Keys
            .Union(demandByWarehouse.Keys)
            .OrderBy(id => id)
            .ToList();

        var positions = new List<WarehouseNetPosition>(relevantWarehouseIds.Count);
        foreach (var warehouseId in relevantWarehouseIds)
        {
            var available = availableByWarehouse.GetValueOrDefault(warehouseId);
            var demand = demandByWarehouse.GetValueOrDefault(warehouseId);
            positions.Add(new WarehouseNetPosition(
                productId,
                warehouseId,
                available,
                demand,
                available - demand));
        }
        return positions;
    }

    private static (List<TransferSuggestion> Transfers, List<ExternalReplenishmentNeed> ExternalNeeds) SuggestTransfers(
        Guid productId,
        IReadOnlyList<WarehouseNetPosition> positions)
    {
        var surpluses = positions
            .Where(p => p.Net > 0m)
            .Select(p => new MutableBalance(p.WarehouseId, p.Net))
            .OrderByDescending(b => b.Quantity)
            .ThenBy(b => b.WarehouseId)
            .ToList();

        var shortfalls = positions
            .Where(p => p.Net < 0m)
            .Select(p => new MutableBalance(p.WarehouseId, -p.Net))
            .OrderByDescending(b => b.Quantity)
            .ThenBy(b => b.WarehouseId)
            .ToList();

        var transfers = new List<TransferSuggestion>();

        var surplusIndex = 0;
        var shortfallIndex = 0;
        while (surplusIndex < surpluses.Count && shortfallIndex < shortfalls.Count)
        {
            var surplus = surpluses[surplusIndex];
            var shortfall = shortfalls[shortfallIndex];

            if (surplus.Quantity <= 0m)
            {
                surplusIndex++;
                continue;
            }
            if (shortfall.Quantity <= 0m)
            {
                shortfallIndex++;
                continue;
            }

            var moveQty = Math.Min(surplus.Quantity, shortfall.Quantity);
            if (surplus.WarehouseId != shortfall.WarehouseId && moveQty > 0m)
            {
                transfers.Add(new TransferSuggestion(
                    productId,
                    surplus.WarehouseId,
                    shortfall.WarehouseId,
                    moveQty));
            }

            surplus.Quantity -= moveQty;
            shortfall.Quantity -= moveQty;

            if (surplus.Quantity <= 0m)
            {
                surplusIndex++;
            }
            if (shortfall.Quantity <= 0m)
            {
                shortfallIndex++;
            }
        }

        var externalNeeds = shortfalls
            .Where(s => s.Quantity > 0m)
            .OrderBy(s => s.WarehouseId)
            .Select(s => new ExternalReplenishmentNeed(productId, s.WarehouseId, s.Quantity))
            .ToList();

        return (transfers, externalNeeds);
    }

    private sealed class MutableBalance
    {
        public MutableBalance(Guid warehouseId, decimal quantity)
        {
            WarehouseId = warehouseId;
            Quantity = quantity;
        }

        public Guid WarehouseId { get; }
        public decimal Quantity { get; set; }
    }
}
