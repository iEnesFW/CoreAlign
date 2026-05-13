using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities;

public class ShipmentLine : TenantEntity
{
    public Guid ShipmentId { get; internal set; }
    public Guid OrderLineId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductSku { get; private set; } = string.Empty;
    public string ProductName { get; private set; } = string.Empty;
    public Guid? LotId { get; private set; }
    public string? SerialNumber { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitCostSnapshot { get; private set; }
    public string? Notes { get; private set; }

    public Shipment Shipment { get; set; } = null!;
    public OrderLine OrderLine { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public Lot? Lot { get; set; }

    protected ShipmentLine() { }

    public ShipmentLine(
        Guid orderLineId,
        Guid productId,
        string productSku,
        string productName,
        decimal quantity,
        decimal unitCostSnapshot,
        Guid? lotId = null,
        string? serialNumber = null,
        string? notes = null)
    {
        OrderLineId = orderLineId;
        ProductId = productId;
        ProductSku = productSku;
        ProductName = productName;
        Quantity = quantity;
        UnitCostSnapshot = unitCostSnapshot;
        LotId = lotId;
        SerialNumber = serialNumber;
        Notes = notes;
    }

    public void Update(decimal quantity, Guid? lotId, string? serialNumber, string? notes)
    {
        Quantity = quantity;
        LotId = lotId;
        SerialNumber = serialNumber;
        Notes = notes;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
