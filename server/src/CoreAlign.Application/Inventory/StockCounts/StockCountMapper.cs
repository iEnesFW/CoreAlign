using CoreAlign.Domain.Entities;

namespace CoreAlign.Application.Inventory.StockCounts;

internal static class StockCountMapper
{
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
