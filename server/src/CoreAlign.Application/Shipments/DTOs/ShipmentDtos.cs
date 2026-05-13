using CoreAlign.Application.Orders.DTOs;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Shipments.DTOs;

public class ShipmentLineDto
{
    public Guid Id { get; set; }
    public Guid OrderLineId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public Guid? LotId { get; set; }
    public string? LotNumber { get; set; }
    public string? SerialNumber { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCostSnapshot { get; set; }
    public string? Notes { get; set; }
}

public class ShipmentDto
{
    public Guid Id { get; set; }
    public string ShipmentNumber { get; set; } = string.Empty;
    public Guid OrderId { get; set; }
    public string? OrderNumber { get; set; }
    public Guid CustomerId { get; set; }
    public Guid WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
    public ShipmentStatus Status { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? PickedAtUtc { get; set; }
    public DateTime? PackedAtUtc { get; set; }
    public DateTime? DispatchedAtUtc { get; set; }
    public DateTime? DeliveredAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public string? CarrierName { get; set; }
    public string? TrackingNumber { get; set; }
    public string? TrackingUrl { get; set; }
    public decimal? ShippingCost { get; set; }
    public string? ReceivedBy { get; set; }
    public AddressSnapshotDto? ShippingAddressSnapshot { get; set; }
    public string? Notes { get; set; }
    public string? CancelReason { get; set; }
    public List<ShipmentLineDto> Lines { get; set; } = new();
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
