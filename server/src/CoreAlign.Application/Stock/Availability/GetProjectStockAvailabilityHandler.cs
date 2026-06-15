using MediatR;

namespace CoreAlign.Application.Stock.Availability;

public class GetProjectStockAvailabilityHandler
    : IRequestHandler<GetProjectStockAvailabilityQuery, IReadOnlyList<StockAvailabilityRow>>
{
    private readonly IStockAvailabilityService _availability;

    public GetProjectStockAvailabilityHandler(IStockAvailabilityService availability)
    {
        _availability = availability;
    }

    public Task<IReadOnlyList<StockAvailabilityRow>> Handle(
        GetProjectStockAvailabilityQuery request,
        CancellationToken cancellationToken)
        => _availability.CheckAsync(request.ProjectId, request.WarehouseId, cancellationToken);
}
