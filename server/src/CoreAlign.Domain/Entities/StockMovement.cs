using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Entities;

public class StockMovement : TenantEntity
{
    public Guid ProductId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid? LotId { get; private set; }
    public string? SerialNumber { get; private set; }
    public StockMovementType Type { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitCost { get; private set; }
    public decimal TotalCost { get; private set; }
    public decimal OnHandAfter { get; private set; }
    public decimal AvgCostAfter { get; private set; }
    public DateTime OccurredAtUtc { get; private set; } = DateTime.UtcNow;
    public StockSourceDocumentType SourceDocumentType { get; private set; }
    public Guid? SourceDocumentId { get; private set; }
    public Guid? SourceLineId { get; private set; }
    public string? SourceReference { get; private set; }
    public Guid? ReasonCodeId { get; private set; }
    public Guid? PostedByUserId { get; private set; }
    public string? Notes { get; private set; }

    public Product Product { get; set; } = null!;
    public Warehouse Warehouse { get; set; } = null!;
    public Lot? Lot { get; set; }
    public StockReasonCode? ReasonCode { get; set; }

    protected StockMovement() { }

    public StockMovement(
        Guid productId,
        Guid warehouseId,
        StockMovementType type,
        decimal quantity,
        decimal unitCost,
        decimal onHandAfter,
        decimal avgCostAfter,
        DateTime occurredAtUtc,
        StockSourceDocumentType sourceDocumentType,
        Guid? sourceDocumentId = null,
        Guid? sourceLineId = null,
        string? sourceReference = null,
        Guid? lotId = null,
        string? serialNumber = null,
        Guid? reasonCodeId = null,
        Guid? postedByUserId = null,
        string? notes = null)
    {
        ProductId = productId;
        WarehouseId = warehouseId;
        Type = type;
        Quantity = quantity;
        UnitCost = unitCost;
        TotalCost = Math.Round(quantity * unitCost, 4);
        OnHandAfter = onHandAfter;
        AvgCostAfter = avgCostAfter;
        OccurredAtUtc = occurredAtUtc;
        SourceDocumentType = sourceDocumentType;
        SourceDocumentId = sourceDocumentId;
        SourceLineId = sourceLineId;
        SourceReference = sourceReference;
        LotId = lotId;
        SerialNumber = serialNumber;
        ReasonCodeId = reasonCodeId;
        PostedByUserId = postedByUserId;
        Notes = notes;
    }
}
