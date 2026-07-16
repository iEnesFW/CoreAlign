namespace CoreAlign.Application.GlassPlates.DTOs;

public record StorageLocationDto(
    Guid Id,
    Guid WarehouseId,
    Guid? ParentLocationId,
    string Code,
    string Name,
    string Kind,
    bool IsActive,
    string? Notes);

public record GlassPlateDto(
    Guid Id,
    Guid ProductId,
    Guid WarehouseId,
    string WarehouseName,
    Guid? StorageLocationId,
    string? StorageLocationCode,
    string? StorageLocationName,
    Guid? LotId,
    string PlateNumber,
    string Kind,
    string Status,
    decimal WidthMm,
    decimal HeightMm,
    decimal ThicknessMm,
    decimal OriginalAreaMm2,
    decimal RemainingAreaMm2,
    decimal UtilizationPercent,
    Guid? ParentPlateId,
    string Condition,
    DateTime ReceivedAtUtc,
    DateTime? ConsumedAtUtc);

public record ReceiveGlassPlatesResultDto(Guid MovementId, int PlateCount, decimal TotalAreaM2);

public record GlassScrapResultDto(Guid MovementId, decimal ScrappedAreaMm2, int PlatesScrapped);

public record ConsumeGlassPlateResultDto(
    Guid MovementId,
    decimal ConsumedAreaMm2,
    Guid? RemnantPlateId,
    decimal RemnantAreaMm2,
    decimal ScrappedAreaMm2);

public record LowStockPlateDto(
    Guid ProductId,
    string Sku,
    string ProductName,
    Guid WarehouseId,
    string WarehouseName,
    int AvailableCount,
    int MinPlateCount);

public record GlassPlateConsumptionDto(
    Guid Id,
    Guid GlassPlateId,
    Guid ProductId,
    Guid WarehouseId,
    Guid? OrderLineId,
    Guid? JobId,
    decimal CutAreaMm2,
    int Pieces,
    decimal ScrappedAreaMm2,
    Guid? ResultingRemnantPlateId,
    DateTime OccurredAtUtc);
