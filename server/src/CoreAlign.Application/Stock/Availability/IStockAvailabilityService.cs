namespace CoreAlign.Application.Stock.Availability;

public interface IStockAvailabilityService
{
    Task<IReadOnlyList<StockAvailabilityRow>> CheckAsync(
        Guid projectId,
        Guid? warehouseId,
        bool accountForPendingDemand = false,
        CancellationToken cancellationToken = default);
}

public sealed record StockAvailabilityRow(
    Guid BomLineId,
    Guid? ProductId,
    string ProductSku,
    string ProductName,
    decimal RequiredQty,
    decimal AvailableQty,
    decimal ShortageQty,
    bool HasShortage,
    bool IsService,
    Guid? WarehouseId,
    IReadOnlyList<StockAvailabilitySubstitute> Substitutes);

public sealed record StockAvailabilitySubstitute(
    Guid ProductId,
    string ProductSku,
    string ProductName,
    decimal AvailableQty,
    decimal ConversionRate,
    int Depth);
