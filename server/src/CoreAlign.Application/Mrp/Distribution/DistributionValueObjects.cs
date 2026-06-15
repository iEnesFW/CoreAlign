using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Mrp.Distribution;

public sealed record WarehouseStockSnapshot(
    Guid ProductId,
    Guid WarehouseId,
    decimal OnHand,
    decimal Reserved,
    decimal Demand);

public sealed record DistributionWarehouseSnapshot(
    Guid WarehouseId,
    bool IsDefault,
    WarehouseType Type);

public sealed record DistributionProductSnapshot(
    Guid ProductId);

public sealed record DistributionInput(
    IReadOnlyList<DistributionProductSnapshot> Products,
    IReadOnlyList<DistributionWarehouseSnapshot> Warehouses,
    IReadOnlyList<WarehouseStockSnapshot> Stock);

public sealed record WarehouseNetPosition(
    Guid ProductId,
    Guid WarehouseId,
    decimal Available,
    decimal Demand,
    decimal Net);

public sealed record TransferSuggestion(
    Guid ProductId,
    Guid FromWarehouseId,
    Guid ToWarehouseId,
    decimal Quantity);

public sealed record ExternalReplenishmentNeed(
    Guid ProductId,
    Guid WarehouseId,
    decimal Quantity);

public sealed record DistributionPlan(
    IReadOnlyList<WarehouseNetPosition> NetPositions,
    IReadOnlyList<TransferSuggestion> Transfers,
    IReadOnlyList<ExternalReplenishmentNeed> ExternalReplenishment);
