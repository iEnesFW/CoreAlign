using CoreAlign.Application.Stock.Substitute;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Stock.Availability;

public class StockAvailabilityService : IStockAvailabilityService
{
    private const int MaxSubstituteDepth = 3;

    private readonly IGlassProjectBOMLineRepository _bomLines;
    private readonly IStockItemRepository _stockItems;
    private readonly IProductRepository _products;
    private readonly IProductSubstituteResolver _resolver;

    public StockAvailabilityService(
        IGlassProjectBOMLineRepository bomLines,
        IStockItemRepository stockItems,
        IProductRepository products,
        IProductSubstituteResolver resolver)
    {
        _bomLines = bomLines;
        _stockItems = stockItems;
        _products = products;
        _resolver = resolver;
    }

    public async Task<IReadOnlyList<StockAvailabilityRow>> CheckAsync(
        Guid projectId,
        Guid? warehouseId,
        CancellationToken cancellationToken = default)
    {
        var lines = await _bomLines.ListByProjectAsync(projectId, cancellationToken);
        if (lines.Count == 0)
        {
            return Array.Empty<StockAvailabilityRow>();
        }

        var productIds = lines
            .Where(l => !l.IsService && l.ProductId.HasValue)
            .Select(l => l.ProductId!.Value)
            .Distinct()
            .ToList();

        var products = productIds.Count == 0
            ? new Dictionary<Guid, Product>()
            : await _products.GetByIdsAsync(productIds, cancellationToken);

        var stockMap = await BuildAvailabilityMapAsync(productIds, warehouseId, cancellationToken);

        var rows = new List<StockAvailabilityRow>(lines.Count);
        foreach (var line in lines)
        {
            if (line.IsService || !line.ProductId.HasValue)
            {
                rows.Add(new StockAvailabilityRow(
                    BomLineId: line.Id,
                    ProductId: line.ProductId,
                    ProductSku: line.IsService ? "SERVICE" : "UNLINKED",
                    ProductName: line.Description,
                    RequiredQty: line.Quantity,
                    AvailableQty: 0m,
                    ShortageQty: 0m,
                    HasShortage: false,
                    IsService: line.IsService,
                    WarehouseId: warehouseId,
                    Substitutes: Array.Empty<StockAvailabilitySubstitute>()));
                continue;
            }

            var productId = line.ProductId.Value;
            products.TryGetValue(productId, out var product);
            var available = stockMap.TryGetValue(productId, out var availableQty) ? availableQty : 0m;
            var shortage = Math.Max(0m, line.Quantity - available);
            var hasShortage = shortage > 0m;

            IReadOnlyList<StockAvailabilitySubstitute> substitutes = Array.Empty<StockAvailabilitySubstitute>();
            if (hasShortage)
            {
                substitutes = await BuildSubstitutesAsync(productId, line.Quantity, warehouseId, cancellationToken);
            }

            rows.Add(new StockAvailabilityRow(
                BomLineId: line.Id,
                ProductId: productId,
                ProductSku: product?.Sku ?? string.Empty,
                ProductName: product?.Name ?? line.Description,
                RequiredQty: line.Quantity,
                AvailableQty: available,
                ShortageQty: shortage,
                HasShortage: hasShortage,
                IsService: false,
                WarehouseId: warehouseId,
                Substitutes: substitutes));
        }

        return rows;
    }

    private async Task<IReadOnlyList<StockAvailabilitySubstitute>> BuildSubstitutesAsync(
        Guid productId,
        decimal requiredQty,
        Guid? warehouseId,
        CancellationToken cancellationToken)
    {
        var suggestions = await _resolver.ResolveAsync(productId, requiredQty, MaxSubstituteDepth, cancellationToken);
        if (suggestions.Count == 0)
        {
            return Array.Empty<StockAvailabilitySubstitute>();
        }

        var subIds = suggestions.Select(s => s.ProductId).Distinct().ToList();
        var subStock = await BuildAvailabilityMapAsync(subIds, warehouseId, cancellationToken);

        var result = new List<StockAvailabilitySubstitute>(suggestions.Count);
        foreach (var suggestion in suggestions)
        {
            var subAvail = subStock.TryGetValue(suggestion.ProductId, out var qty) ? qty : 0m;
            if (subAvail <= 0m) continue;
            result.Add(new StockAvailabilitySubstitute(
                ProductId: suggestion.ProductId,
                ProductSku: suggestion.ProductSku,
                ProductName: suggestion.ProductName,
                AvailableQty: subAvail,
                ConversionRate: suggestion.ConversionRate,
                Depth: suggestion.Depth));
        }

        return result;
    }

    private async Task<IReadOnlyDictionary<Guid, decimal>> BuildAvailabilityMapAsync(
        IReadOnlyCollection<Guid> productIds,
        Guid? warehouseId,
        CancellationToken cancellationToken)
    {
        if (productIds.Count == 0)
        {
            return new Dictionary<Guid, decimal>();
        }

        var distinct = productIds.Distinct().ToList();
        var totals = await _stockItems.SumOnHandAndReservedByProductsAsync(distinct, warehouseId, cancellationToken);

        var map = new Dictionary<Guid, decimal>(distinct.Count);
        foreach (var productId in distinct)
        {
            if (totals.TryGetValue(productId, out var pair))
            {
                map[productId] = Math.Max(0m, pair.OnHand - pair.Reserved);
            }
            else
            {
                map[productId] = 0m;
            }
        }
        return map;
    }
}
