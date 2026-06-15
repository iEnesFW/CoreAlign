namespace CoreAlign.Application.Mrp.Distribution;

public sealed record DistributionProductInfo(Guid ProductId, string Sku, string Name);

public sealed record DistributionWarehouseInfo(Guid WarehouseId, string Code, string Name);

public sealed record DistributionContext(
    DistributionInput Input,
    IReadOnlyDictionary<Guid, DistributionProductInfo> Products,
    IReadOnlyDictionary<Guid, DistributionWarehouseInfo> Warehouses);

public interface IDistributionDataLoader
{
    Task<DistributionContext> LoadAsync(CancellationToken cancellationToken = default);
}
