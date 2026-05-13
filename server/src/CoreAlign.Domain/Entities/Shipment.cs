using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Domain.Entities;

public class Shipment : TenantEntity
{
    public string ShipmentNumber { get; private set; } = string.Empty;
    public Guid OrderId { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public ShipmentStatus Status { get; private set; } = ShipmentStatus.Draft;
    public DateTime CreatedDate { get; private set; } = DateTime.UtcNow;
    public DateTime? PickedAtUtc { get; private set; }
    public DateTime? PackedAtUtc { get; private set; }
    public DateTime? DispatchedAtUtc { get; private set; }
    public DateTime? DeliveredAtUtc { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }

    public string? CarrierName { get; private set; }
    public string? TrackingNumber { get; private set; }
    public string? TrackingUrl { get; private set; }
    public decimal? ShippingCost { get; private set; }
    public string? ReceivedBy { get; private set; }
    public AddressSnapshot? ShippingAddressSnapshot { get; private set; }
    public string? Notes { get; private set; }
    public Guid? PostedByUserId { get; private set; }
    public string? CancelReason { get; private set; }

    public Order Order { get; set; } = null!;
    public Warehouse Warehouse { get; set; } = null!;
    public ICollection<ShipmentLine> Lines { get; private set; } = new List<ShipmentLine>();

    protected Shipment() { }

    public Shipment(
        string shipmentNumber,
        Guid orderId,
        Guid customerId,
        Guid warehouseId,
        AddressSnapshot? shippingAddressSnapshot)
    {
        ShipmentNumber = shipmentNumber;
        OrderId = orderId;
        CustomerId = customerId;
        WarehouseId = warehouseId;
        ShippingAddressSnapshot = shippingAddressSnapshot;
    }

    public void AddLine(ShipmentLine line)
    {
        if (Status != ShipmentStatus.Draft)
        {
            throw new InvalidShipmentStateException("Shipment lines can only be added while in Draft.");
        }
        Lines.Add(line);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkPicked(Guid? postedByUserId)
    {
        EnsureTransitionAllowed(Status, ShipmentStatus.Picked);
        Status = ShipmentStatus.Picked;
        PickedAtUtc = DateTime.UtcNow;
        PostedByUserId = postedByUserId;
        UpdatedAtUtc = PickedAtUtc.Value;
    }

    public void MarkPacked()
    {
        EnsureTransitionAllowed(Status, ShipmentStatus.Packed);
        Status = ShipmentStatus.Packed;
        PackedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = PackedAtUtc.Value;
    }

    public void Dispatch(string? carrierName, string? trackingNumber, string? trackingUrl, decimal? shippingCost)
    {
        EnsureTransitionAllowed(Status, ShipmentStatus.Dispatched);
        Status = ShipmentStatus.Dispatched;
        DispatchedAtUtc = DateTime.UtcNow;
        CarrierName = carrierName;
        TrackingNumber = trackingNumber;
        TrackingUrl = trackingUrl;
        ShippingCost = shippingCost;
        UpdatedAtUtc = DispatchedAtUtc.Value;
    }

    public void MarkDelivered(string? receivedBy, DateTime? deliveredAtUtc)
    {
        EnsureTransitionAllowed(Status, ShipmentStatus.Delivered);
        Status = ShipmentStatus.Delivered;
        DeliveredAtUtc = deliveredAtUtc ?? DateTime.UtcNow;
        ReceivedBy = receivedBy;
        UpdatedAtUtc = DeliveredAtUtc.Value;
    }

    public void Cancel(string? reason)
    {
        if (Status == ShipmentStatus.Delivered)
        {
            throw new InvalidShipmentStateException("Delivered shipments cannot be cancelled (use a return).");
        }
        Status = ShipmentStatus.Cancelled;
        CancelReason = reason;
        CancelledAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = CancelledAtUtc.Value;
    }

    public void UpdateMeta(string? notes, AddressSnapshot? shippingAddressSnapshot)
    {
        Notes = notes;
        if (shippingAddressSnapshot != null) ShippingAddressSnapshot = shippingAddressSnapshot;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private static void EnsureTransitionAllowed(ShipmentStatus from, ShipmentStatus to)
    {
        var allowed = from switch
        {
            ShipmentStatus.Draft => to is ShipmentStatus.Picked or ShipmentStatus.Cancelled,
            ShipmentStatus.Picked => to is ShipmentStatus.Packed or ShipmentStatus.Cancelled,
            ShipmentStatus.Packed => to is ShipmentStatus.Dispatched or ShipmentStatus.Cancelled,
            ShipmentStatus.Dispatched => to is ShipmentStatus.Delivered or ShipmentStatus.Returned,
            _ => false
        };
        if (!allowed)
        {
            throw new InvalidShipmentStateException($"Shipment cannot transition from {from} to {to}.");
        }
    }
}
