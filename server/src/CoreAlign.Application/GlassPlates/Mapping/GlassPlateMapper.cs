using CoreAlign.Application.GlassPlates.DTOs;
using CoreAlign.Domain.Entities.GlassPlates;

namespace CoreAlign.Application.GlassPlates.Mapping;

public static class GlassPlateMapper
{
    public static StorageLocationDto ToDto(StorageLocation l) =>
        new(l.Id, l.WarehouseId, l.ParentLocationId, l.Code, l.Name, l.Kind.ToString(), l.IsActive, l.Notes);

    public static GlassPlateDto ToDto(GlassPlate p) =>
        new(
            p.Id,
            p.ProductId,
            p.WarehouseId,
            p.Warehouse?.Name ?? string.Empty,
            p.StorageLocationId,
            p.StorageLocation?.Code,
            p.StorageLocation?.Name,
            p.LotId,
            p.PlateNumber,
            p.Kind.ToString(),
            p.Status.ToString(),
            p.WidthMm,
            p.HeightMm,
            p.ThicknessMm,
            p.OriginalAreaMm2,
            p.RemainingAreaMm2,
            p.OriginalAreaMm2 <= 0m
                ? 0m
                : Math.Round((p.OriginalAreaMm2 - p.RemainingAreaMm2) * 100m / p.OriginalAreaMm2, 2),
            p.ParentPlateId,
            p.Condition.ToString(),
            p.ReceivedAtUtc,
            p.ConsumedAtUtc);

    public static GlassPlateConsumptionDto ToDto(GlassPlateConsumption c) =>
        new(
            c.Id,
            c.GlassPlateId,
            c.ProductId,
            c.WarehouseId,
            c.OrderLineId,
            c.JobId,
            c.CutAreaMm2,
            c.Pieces,
            c.ScrappedAreaMm2,
            c.ResultingRemnantPlateId,
            c.OccurredAtUtc);
}
