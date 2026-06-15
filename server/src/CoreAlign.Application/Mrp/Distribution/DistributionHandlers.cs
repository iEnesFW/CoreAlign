using MediatR;

namespace CoreAlign.Application.Mrp.Distribution;

public class GetMrpTransferSuggestionsHandler
    : IRequestHandler<GetMrpTransferSuggestionsQuery, MrpTransferSuggestionsResultDto>
{
    private readonly IDistributionDataLoader _loader;
    private readonly IDistributionPlanner _planner;

    public GetMrpTransferSuggestionsHandler(IDistributionDataLoader loader, IDistributionPlanner planner)
    {
        _loader = loader;
        _planner = planner;
    }

    public async Task<MrpTransferSuggestionsResultDto> Handle(GetMrpTransferSuggestionsQuery q, CancellationToken ct)
    {
        var context = await _loader.LoadAsync(ct);
        var plan = _planner.Plan(context.Input);

        var transfers = plan.Transfers
            .Select(t => ToTransferDto(t, context))
            .ToList();

        var netPositions = plan.NetPositions
            .Select(n => ToNetPositionDto(n, context))
            .ToList();

        var externalReplenishment = plan.ExternalReplenishment
            .Select(e => ToExternalDto(e, context))
            .ToList();

        return new MrpTransferSuggestionsResultDto(
            context.Input.Products.Count,
            transfers.Count,
            externalReplenishment.Count,
            transfers,
            netPositions,
            externalReplenishment);
    }

    private static MrpTransferSuggestionDto ToTransferDto(TransferSuggestion t, DistributionContext context)
    {
        var product = ResolveProduct(t.ProductId, context);
        var from = ResolveWarehouse(t.FromWarehouseId, context);
        var to = ResolveWarehouse(t.ToWarehouseId, context);
        return new MrpTransferSuggestionDto(
            t.ProductId,
            product.Sku,
            product.Name,
            t.FromWarehouseId,
            from.Code,
            from.Name,
            t.ToWarehouseId,
            to.Code,
            to.Name,
            t.Quantity);
    }

    private static MrpWarehouseNetPositionDto ToNetPositionDto(WarehouseNetPosition n, DistributionContext context)
    {
        var product = ResolveProduct(n.ProductId, context);
        var warehouse = ResolveWarehouse(n.WarehouseId, context);
        return new MrpWarehouseNetPositionDto(
            n.ProductId,
            product.Sku,
            product.Name,
            n.WarehouseId,
            warehouse.Code,
            warehouse.Name,
            n.Available,
            n.Demand,
            n.Net);
    }

    private static MrpExternalReplenishmentDto ToExternalDto(ExternalReplenishmentNeed e, DistributionContext context)
    {
        var product = ResolveProduct(e.ProductId, context);
        var warehouse = ResolveWarehouse(e.WarehouseId, context);
        return new MrpExternalReplenishmentDto(
            e.ProductId,
            product.Sku,
            product.Name,
            e.WarehouseId,
            warehouse.Code,
            warehouse.Name,
            e.Quantity);
    }

    private static DistributionProductInfo ResolveProduct(Guid productId, DistributionContext context) =>
        context.Products.TryGetValue(productId, out var info)
            ? info
            : new DistributionProductInfo(productId, string.Empty, string.Empty);

    private static DistributionWarehouseInfo ResolveWarehouse(Guid warehouseId, DistributionContext context) =>
        context.Warehouses.TryGetValue(warehouseId, out var info)
            ? info
            : new DistributionWarehouseInfo(warehouseId, string.Empty, string.Empty);
}
