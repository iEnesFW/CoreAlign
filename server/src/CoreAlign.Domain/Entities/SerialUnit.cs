using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Domain.Entities;

// A single serialized unit — the per-unit traceability ledger for serial-tracked products. This is
// METADATA ONLY: UnitCost is informational and NON-authoritative for GL; cost of record stays on the
// StockMovement/COGS chain. Tracks where-used (order/shipment/owner) and production genealogy
// (ParentSerialUnitId links a consumed component unit to the assembly unit it went into).
public class SerialUnit : TenantEntity, IHasConcurrencyToken
{
    public Guid ProductId { get; private set; }
    public string SerialNumber { get; private set; } = string.Empty;
    public Guid? LotId { get; private set; }
    public Guid? WarehouseId { get; private set; }
    public SerialStatus Status { get; private set; } = SerialStatus.InStock;
    public decimal UnitCost { get; private set; }
    public DateTime ReceivedAtUtc { get; private set; }
    public Guid? SourceReceiptMovementId { get; private set; }

    // Where-used: populated when the unit is shipped to a customer.
    public Guid? OrderId { get; private set; }
    public Guid? ShipmentId { get; private set; }
    public Guid? CurrentOwnerCustomerId { get; private set; }

    // Production genealogy: the assembly serial this component unit was consumed into.
    public Guid? ParentSerialUnitId { get; private set; }

    public long ConcurrencyToken { get; private set; }
    void IHasConcurrencyToken.BumpConcurrencyToken() => ConcurrencyToken++;

    protected SerialUnit() { }

    public SerialUnit(
        Guid productId,
        string serialNumber,
        DateTime receivedAtUtc,
        Guid? warehouseId = null,
        Guid? lotId = null,
        decimal unitCost = 0m,
        Guid? sourceReceiptMovementId = null,
        Guid? parentSerialUnitId = null)
    {
        if (string.IsNullOrWhiteSpace(serialNumber))
        {
            throw new StockMovementValidationException("Serial number is required.");
        }
        ProductId = productId;
        SerialNumber = serialNumber.Trim();
        ReceivedAtUtc = receivedAtUtc;
        WarehouseId = warehouseId;
        LotId = lotId;
        UnitCost = unitCost;
        SourceReceiptMovementId = sourceReceiptMovementId;
        ParentSerialUnitId = parentSerialUnitId;
    }

    public void Ship(Guid orderId, Guid? shipmentId, Guid? customerId, DateTime occurredAtUtc)
    {
        if (Status is SerialStatus.Shipped or SerialStatus.Scrapped)
        {
            throw new SerialUnitTransitionException(SerialNumber, Status, SerialStatus.Shipped);
        }
        Status = SerialStatus.Shipped;
        OrderId = orderId;
        ShipmentId = shipmentId;
        CurrentOwnerCustomerId = customerId;
        UpdatedAtUtc = occurredAtUtc;
    }

    public void ReturnToStock(Guid? warehouseId, DateTime occurredAtUtc)
    {
        if (Status != SerialStatus.Shipped)
        {
            throw new SerialUnitTransitionException(SerialNumber, Status, SerialStatus.Returned);
        }
        Status = SerialStatus.Returned;
        CurrentOwnerCustomerId = null;
        if (warehouseId.HasValue)
        {
            WarehouseId = warehouseId;
        }
        UpdatedAtUtc = occurredAtUtc;
    }

    public void Scrap(DateTime occurredAtUtc)
    {
        if (Status == SerialStatus.Scrapped)
        {
            return;
        }
        Status = SerialStatus.Scrapped;
        UpdatedAtUtc = occurredAtUtc;
    }

    public void AssignToAssembly(Guid parentSerialUnitId, DateTime occurredAtUtc)
    {
        ParentSerialUnitId = parentSerialUnitId;
        UpdatedAtUtc = occurredAtUtc;
    }
}
