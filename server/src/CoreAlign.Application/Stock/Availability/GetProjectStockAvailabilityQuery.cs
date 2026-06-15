using MediatR;

namespace CoreAlign.Application.Stock.Availability;

public record GetProjectStockAvailabilityQuery(
    Guid ProjectId,
    Guid? WarehouseId = null) : IRequest<IReadOnlyList<StockAvailabilityRow>>;
