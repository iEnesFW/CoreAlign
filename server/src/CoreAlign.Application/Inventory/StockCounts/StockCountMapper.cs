using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Inventory.StockCounts;

internal static class StockCountMapper
{
    // List rows carry no lines (loaded only in the detail view); totals are already
    // aggregated server-side on the row, so Lines is intentionally empty here.
    public static StockCountDto ToDto(StockCountSearchRow r) => new(
        r.Id,
        r.CountNumber,
        r.WarehouseId,
        r.WarehouseCode,
        r.WarehouseName,
        r.Status,
        r.PlannedAtUtc,
        r.CountingStartedAtUtc,
        r.ReconciledAtUtc,
        r.PostedAtUtc,
        r.PlannedByUserId,
        r.PostedByUserId,
        r.Notes,
        r.TotalVarianceQuantity,
        r.TotalVarianceCost,
        System.Array.Empty<StockCountLineDto>(),
        r.CreatedAtUtc);

    public static StockCountLineDto ToDto(StockCountLine l) => new(
        l.Id,
        l.ProductId,
        l.ProductSku,
        l.ProductName,
        l.LotId,
        l.LotNumber,
        l.BinLocation,
        l.ExpectedQuantity,
        l.CountedQuantity,
        l.VarianceQuantity,
        l.SnapshotUnitCost,
        l.VarianceCost,
        l.CountedAtUtc,
        l.CountedByUserId,
        l.LineNotes);

    public static StockCountDto ToDto(StockCount c) => new(
        c.Id,
        c.CountNumber,
        c.WarehouseId,
        c.WarehouseCode,
        c.WarehouseName,
        c.Status,
        c.PlannedAtUtc,
        c.CountingStartedAtUtc,
        c.ReconciledAtUtc,
        c.PostedAtUtc,
        c.PlannedByUserId,
        c.PostedByUserId,
        c.Notes,
        c.TotalVarianceQuantity,
        c.TotalVarianceCost,
        c.Lines.OrderBy(l => l.ProductSku).Select(ToDto).ToList(),
        c.CreatedAtUtc);
}
