using CoreAlign.Application.Inventory.DTOs;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Inventory.Mapping;

public static class InventoryMapper
{
    public static StockItemDto ToDto(StockItemSearchRow r) => new()
    {
        Id = r.Id,
        ProductId = r.ProductId,
        ProductSku = r.ProductSku,
        ProductName = r.ProductName,
        WarehouseId = r.WarehouseId,
        WarehouseCode = r.WarehouseCode,
        WarehouseName = r.WarehouseName,
        LotId = r.LotId,
        LotNumber = r.LotNumber,
        LotExpiryDate = r.LotExpiryDate,
        BinLocation = r.BinLocation,
        OnHand = r.OnHand,
        Reserved = r.Reserved,
        AvailableToPromise = r.OnHand - r.Reserved,
        AvgCost = r.AvgCost,
        ReorderPoint = r.ProductReorderPoint,
        MinStock = r.ProductMinStock,
        Currency = r.ProductCurrency,
        LastMovementAtUtc = r.LastMovementAtUtc,
    };

    public static StockItemDto ToDto(StockItem item) => new()
    {
        Id = item.Id,
        ProductId = item.ProductId,
        ProductSku = item.Product?.Sku ?? string.Empty,
        ProductName = item.Product?.Name ?? string.Empty,
        WarehouseId = item.WarehouseId,
        WarehouseCode = item.Warehouse?.Code ?? string.Empty,
        WarehouseName = item.Warehouse?.Name ?? string.Empty,
        LotId = item.LotId,
        LotNumber = item.Lot?.LotNumber,
        LotExpiryDate = item.Lot?.ExpiryDate,
        BinLocation = item.BinLocation,
        OnHand = item.OnHand,
        Reserved = item.Reserved,
        AvailableToPromise = item.AvailableToPromise,
        AvgCost = item.AvgCost,
        ReorderPoint = item.Product?.ReorderPoint,
        MinStock = item.Product?.MinStock,
        Currency = item.Product?.Currency ?? "TRY",
        LastMovementAtUtc = item.LastMovementAtUtc,
    };

    public static StockMovementDto ToDto(StockMovement m) => new()
    {
        Id = m.Id,
        ProductId = m.ProductId,
        ProductSku = m.Product?.Sku ?? string.Empty,
        ProductName = m.Product?.Name ?? string.Empty,
        WarehouseId = m.WarehouseId,
        WarehouseCode = m.Warehouse?.Code ?? string.Empty,
        WarehouseName = m.Warehouse?.Name ?? string.Empty,
        LotId = m.LotId,
        LotNumber = m.Lot?.LotNumber,
        SerialNumber = m.SerialNumber,
        Type = m.Type,
        Quantity = m.Quantity,
        UnitCost = m.UnitCost,
        TotalCost = m.TotalCost,
        OnHandAfter = m.OnHandAfter,
        AvgCostAfter = m.AvgCostAfter,
        OccurredAtUtc = m.OccurredAtUtc,
        SourceDocumentType = m.SourceDocumentType,
        SourceDocumentId = m.SourceDocumentId,
        SourceReference = m.SourceReference,
        ReasonCodeId = m.ReasonCodeId,
        ReasonCodeName = m.ReasonCode?.Name,
        PostedByUserId = m.PostedByUserId,
        Notes = m.Notes,
    };

    public static StockTransferResultDto ToDto(StockTransferResult r) => new()
    {
        ProductId = r.ProductId,
        FromWarehouseId = r.FromWarehouseId,
        ToWarehouseId = r.ToWarehouseId,
        Quantity = r.Quantity,
        UnitCost = r.UnitCost,
        FromOnHandAfter = r.FromOnHandAfter,
        ToOnHandAfter = r.ToOnHandAfter,
        SourceDocumentId = r.SourceDocumentId,
        MovementsCreated = r.MovementsCreated,
        TransferOut = ToDto(r.TransferOut),
        TransferIn = ToDto(r.TransferIn),
    };

    public static StockAllocationDto ToDto(StockAllocation a) => new()
    {
        Id = a.Id,
        OrderId = a.OrderId,
        OrderLineId = a.OrderLineId,
        ProductId = a.ProductId,
        ProductSku = a.Product?.Sku ?? string.Empty,
        ProductName = a.Product?.Name ?? string.Empty,
        WarehouseId = a.WarehouseId,
        WarehouseName = a.Warehouse?.Name ?? string.Empty,
        LotId = a.LotId,
        LotNumber = a.Lot?.LotNumber,
        Quantity = a.Quantity,
        QuantityConsumed = a.QuantityConsumed,
        Remaining = a.Remaining,
        Status = a.Status,
        AllocatedAtUtc = a.AllocatedAtUtc,
        ReleasedAtUtc = a.ReleasedAtUtc,
    };

    public static LotDto ToDto(Lot lot)
    {
        var nowUtc = DateTime.UtcNow;
        int? days = lot.ExpiryDate.HasValue
            ? Math.Max(0, (int)Math.Floor((lot.ExpiryDate.Value - nowUtc).TotalDays))
            : (int?)null;
        return new LotDto
        {
            Id = lot.Id,
            ProductId = lot.ProductId,
            LotNumber = lot.LotNumber,
            ManufactureDate = lot.ManufactureDate,
            ExpiryDate = lot.ExpiryDate,
            SupplierLotRef = lot.SupplierLotRef,
            CountryOfOrigin = lot.CountryOfOrigin,
            Notes = lot.Notes,
            IsBlocked = lot.IsBlocked,
            BlockReason = lot.BlockReason,
            IsExpired = lot.IsExpired(nowUtc),
            DaysUntilExpiry = days,
        };
    }

    public static StockReasonCodeDto ToDto(StockReasonCode r) => new()
    {
        Id = r.Id,
        Code = r.Code,
        Name = r.Name,
        Category = r.Category,
        AffectsCost = r.AffectsCost,
        Description = r.Description,
        IsActive = r.IsActive,
    };
}
