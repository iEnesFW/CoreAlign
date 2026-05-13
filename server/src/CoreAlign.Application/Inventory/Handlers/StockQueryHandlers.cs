using CoreAlign.Application.Common;
using CoreAlign.Application.Inventory.DTOs;
using CoreAlign.Application.Inventory.Mapping;
using CoreAlign.Application.Inventory.Queries;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Inventory.Handlers;

public class GetStockItemsHandler : IRequestHandler<GetStockItemsQuery, PagedResult<StockItemDto>>
{
    private readonly IStockItemRepository _stockItems;
    public GetStockItemsHandler(IStockItemRepository stockItems) => _stockItems = stockItems;

    public async Task<PagedResult<StockItemDto>> Handle(GetStockItemsQuery q, CancellationToken ct)
    {
        var items = await _stockItems.SearchAsync(q.ProductId, q.WarehouseId, q.OnlyBelowReorder, q.Page, q.PageSize, ct);
        var total = await _stockItems.CountAsync(q.ProductId, q.WarehouseId, q.OnlyBelowReorder, ct);
        return new PagedResult<StockItemDto>
        {
            Items = items.Select(InventoryMapper.ToDto).ToList(),
            Total = total,
            Page = q.Page,
            PageSize = q.PageSize,
        };
    }
}

public class GetStockByProductHandler : IRequestHandler<GetStockByProductQuery, IReadOnlyList<StockItemDto>>
{
    private readonly IStockItemRepository _stockItems;
    public GetStockByProductHandler(IStockItemRepository stockItems) => _stockItems = stockItems;

    public async Task<IReadOnlyList<StockItemDto>> Handle(GetStockByProductQuery q, CancellationToken ct) =>
        (await _stockItems.GetByProductAsync(q.ProductId, ct)).Select(InventoryMapper.ToDto).ToList();
}

public class GetStockSummaryHandler : IRequestHandler<GetStockSummaryQuery, StockSummaryDto>
{
    private readonly IStockItemRepository _stockItems;
    private readonly IProductRepository _products;
    public GetStockSummaryHandler(IStockItemRepository stockItems, IProductRepository products)
    {
        _stockItems = stockItems;
        _products = products;
    }

    public async Task<StockSummaryDto> Handle(GetStockSummaryQuery q, CancellationToken ct)
    {
        var items = await _stockItems.GetByProductAsync(q.ProductId, ct);
        var product = await _products.GetByIdAsync(q.ProductId, ct);

        var totalOnHand = items.Sum(i => i.OnHand);
        var totalReserved = items.Sum(i => i.Reserved);
        var totalAvailable = totalOnHand - totalReserved;
        var totalValue = items.Sum(i => i.OnHand * i.AvgCost);
        var avgCost = totalOnHand > 0m ? Math.Round(totalValue / totalOnHand, 4) : 0m;

        return new StockSummaryDto
        {
            ProductId = q.ProductId,
            ProductSku = product?.Sku ?? string.Empty,
            ProductName = product?.Name ?? string.Empty,
            TotalOnHand = totalOnHand,
            TotalReserved = totalReserved,
            TotalAvailable = totalAvailable,
            AverageCost = avgCost,
            Currency = product?.Currency ?? "TRY",
            WarehouseCount = items.Select(i => i.WarehouseId).Distinct().Count(),
            IsBelowReorder = product is not null && product.ReorderPoint > 0m && totalAvailable < product.ReorderPoint,
        };
    }
}

public class GetStockMovementsHandler : IRequestHandler<GetStockMovementsQuery, PagedResult<StockMovementDto>>
{
    private readonly IStockMovementRepository _movements;
    public GetStockMovementsHandler(IStockMovementRepository movements) => _movements = movements;

    public async Task<PagedResult<StockMovementDto>> Handle(GetStockMovementsQuery q, CancellationToken ct)
    {
        var (items, total) = await _movements.SearchAsync(q.ProductId, q.WarehouseId, q.Type, q.FromUtc, q.ToUtc, q.Page, q.PageSize, ct);
        return new PagedResult<StockMovementDto>
        {
            Items = items.Select(InventoryMapper.ToDto).ToList(),
            Total = total,
            Page = q.Page,
            PageSize = q.PageSize,
        };
    }
}

public class GetStockAllocationsByOrderHandler : IRequestHandler<GetStockAllocationsByOrderQuery, IReadOnlyList<StockAllocationDto>>
{
    private readonly IStockAllocationRepository _allocations;
    public GetStockAllocationsByOrderHandler(IStockAllocationRepository allocations) => _allocations = allocations;

    public async Task<IReadOnlyList<StockAllocationDto>> Handle(GetStockAllocationsByOrderQuery q, CancellationToken ct) =>
        (await _allocations.GetByOrderAsync(q.OrderId, ct)).Select(InventoryMapper.ToDto).ToList();
}

public class GetLotsByProductHandler : IRequestHandler<GetLotsByProductQuery, IReadOnlyList<LotDto>>
{
    private readonly ILotRepository _lots;
    public GetLotsByProductHandler(ILotRepository lots) => _lots = lots;

    public async Task<IReadOnlyList<LotDto>> Handle(GetLotsByProductQuery q, CancellationToken ct) =>
        (await _lots.GetByProductAsync(q.ProductId, ct)).Select(InventoryMapper.ToDto).ToList();
}

public class ListStockReasonCodesHandler : IRequestHandler<ListStockReasonCodesQuery, IReadOnlyList<StockReasonCodeDto>>
{
    private readonly IStockReasonCodeRepository _reasons;
    public ListStockReasonCodesHandler(IStockReasonCodeRepository reasons) => _reasons = reasons;

    public async Task<IReadOnlyList<StockReasonCodeDto>> Handle(ListStockReasonCodesQuery q, CancellationToken ct) =>
        (await _reasons.ListAsync(q.Category, q.IsActive, ct)).Select(InventoryMapper.ToDto).ToList();
}
