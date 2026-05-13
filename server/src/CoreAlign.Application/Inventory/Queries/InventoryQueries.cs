using CoreAlign.Application.Common;
using CoreAlign.Application.Inventory.DTOs;
using CoreAlign.Domain.Enums;
using MediatR;

namespace CoreAlign.Application.Inventory.Queries;

public record GetStockItemsQuery(
    Guid? ProductId = null,
    Guid? WarehouseId = null,
    bool OnlyBelowReorder = false,
    int Page = 1,
    int PageSize = 50) : IRequest<PagedResult<StockItemDto>>;

public record GetStockByProductQuery(Guid ProductId) : IRequest<IReadOnlyList<StockItemDto>>;

public record GetStockSummaryQuery(Guid ProductId) : IRequest<StockSummaryDto>;

public record GetStockMovementsQuery(
    Guid? ProductId = null,
    Guid? WarehouseId = null,
    StockMovementType? Type = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    int Page = 1,
    int PageSize = 50) : IRequest<PagedResult<StockMovementDto>>;

public record GetStockAllocationsByOrderQuery(Guid OrderId) : IRequest<IReadOnlyList<StockAllocationDto>>;

public record GetLotsByProductQuery(Guid ProductId) : IRequest<IReadOnlyList<LotDto>>;

public record ListStockReasonCodesQuery(StockReasonCategory? Category = null, bool? IsActive = null)
    : IRequest<IReadOnlyList<StockReasonCodeDto>>;
