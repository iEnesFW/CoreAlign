using CoreAlign.Application.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Inventory.DTOs;

public class StockItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public Guid? LotId { get; set; }
    public string? LotNumber { get; set; }
    public DateTime? LotExpiryDate { get; set; }
    public string? BinLocation { get; set; }
    public decimal OnHand { get; set; }
    public decimal Reserved { get; set; }
    public decimal AvailableToPromise { get; set; }
    public decimal AvgCost { get; set; }
    public decimal? ReorderPoint { get; set; }
    public decimal? MinStock { get; set; }
    public string Currency { get; set; } = "TRY";
    public DateTime? LastMovementAtUtc { get; set; }
}

public class StockSummaryDto
{
    public Guid ProductId { get; set; }
    public string ProductSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal TotalOnHand { get; set; }
    public decimal TotalReserved { get; set; }
    public decimal TotalAvailable { get; set; }
    public decimal AverageCost { get; set; }
    public string Currency { get; set; } = "TRY";
    public int WarehouseCount { get; set; }
    public bool IsBelowReorder { get; set; }
}

public class StockMovementDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public Guid? LotId { get; set; }
    public string? LotNumber { get; set; }
    public string? SerialNumber { get; set; }
    public StockMovementType Type { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public decimal OnHandAfter { get; set; }
    public decimal AvgCostAfter { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public StockSourceDocumentType SourceDocumentType { get; set; }
    public Guid? SourceDocumentId { get; set; }
    public string? SourceReference { get; set; }
    public Guid? ReasonCodeId { get; set; }
    public string? ReasonCodeName { get; set; }
    public Guid? PostedByUserId { get; set; }
    public string? Notes { get; set; }
}

public class StockAllocationDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid OrderLineId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public Guid? LotId { get; set; }
    public string? LotNumber { get; set; }
    public decimal Quantity { get; set; }
    public decimal QuantityConsumed { get; set; }
    public decimal Remaining { get; set; }
    public AllocationStatus Status { get; set; }
    public DateTime AllocatedAtUtc { get; set; }
    public DateTime? ReleasedAtUtc { get; set; }
}

public class LotDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string LotNumber { get; set; } = string.Empty;
    public DateTime? ManufactureDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? SupplierLotRef { get; set; }
    public string? CountryOfOrigin { get; set; }
    public string? Notes { get; set; }
    public bool IsBlocked { get; set; }
    public string? BlockReason { get; set; }
    public bool IsExpired { get; set; }
    public int? DaysUntilExpiry { get; set; }
}

public class StockReasonCodeDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public StockReasonCategory Category { get; set; }
    public bool AffectsCost { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public class StockMovementListResult
{
    public PagedResult<StockMovementDto> Page { get; set; } = new();
}
