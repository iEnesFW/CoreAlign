using CoreAlign.Application.Orders.DTOs;
using CoreAlign.Application.Shipments.DTOs;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Shipments.Mapping;

public static class ShipmentMapper
{
    public static ShipmentDto ToDto(ShipmentSearchRow r) => new()
    {
        Id = r.Id,
        ShipmentNumber = r.ShipmentNumber,
        OrderId = r.OrderId,
        OrderNumber = null,
        CustomerId = r.CustomerId,
        WarehouseId = r.WarehouseId,
        WarehouseName = r.WarehouseName,
        Status = r.Status,
        CreatedDate = r.CreatedDate,
        PickedAtUtc = r.PickedAtUtc,
        PackedAtUtc = r.PackedAtUtc,
        DispatchedAtUtc = r.DispatchedAtUtc,
        DeliveredAtUtc = r.DeliveredAtUtc,
        CancelledAtUtc = r.CancelledAtUtc,
        CarrierName = r.CarrierName,
        TrackingNumber = r.TrackingNumber,
        TrackingUrl = r.TrackingUrl,
        ShippingCost = r.ShippingCost,
        ReceivedBy = r.ReceivedBy,
        ShippingAddressSnapshot = null,
        Notes = r.Notes,
        CancelReason = r.CancelReason,
        Lines = new List<ShipmentLineDto>(),
        CreatedAtUtc = r.CreatedAtUtc,
        UpdatedAtUtc = r.UpdatedAtUtc,
    };


    public static ShipmentDto ToDto(Shipment s) => new()
    {
        Id = s.Id,
        ShipmentNumber = s.ShipmentNumber,
        OrderId = s.OrderId,
        OrderNumber = s.Order?.OrderNumber,
        CustomerId = s.CustomerId,
        WarehouseId = s.WarehouseId,
        WarehouseName = s.Warehouse?.Name,
        Status = s.Status,
        CreatedDate = s.CreatedDate,
        PickedAtUtc = s.PickedAtUtc,
        PackedAtUtc = s.PackedAtUtc,
        DispatchedAtUtc = s.DispatchedAtUtc,
        DeliveredAtUtc = s.DeliveredAtUtc,
        CancelledAtUtc = s.CancelledAtUtc,
        CarrierName = s.CarrierName,
        TrackingNumber = s.TrackingNumber,
        TrackingUrl = s.TrackingUrl,
        ShippingCost = s.ShippingCost,
        ReceivedBy = s.ReceivedBy,
        ShippingAddressSnapshot = s.ShippingAddressSnapshot == null
            ? null
            : new AddressSnapshotDto
            {
                Label = s.ShippingAddressSnapshot.Label,
                RecipientName = s.ShippingAddressSnapshot.RecipientName,
                Phone = s.ShippingAddressSnapshot.Phone,
                Line1 = s.ShippingAddressSnapshot.Line1,
                Line2 = s.ShippingAddressSnapshot.Line2,
                City = s.ShippingAddressSnapshot.City,
                State = s.ShippingAddressSnapshot.State,
                PostalCode = s.ShippingAddressSnapshot.PostalCode,
                Country = s.ShippingAddressSnapshot.Country,
            },
        Notes = s.Notes,
        CancelReason = s.CancelReason,
        CarrierVkn = s.CarrierVkn,
        VehiclePlate = s.VehiclePlate,
        DriverName = s.DriverName,
        DriverTckn = s.DriverTckn,
        EDespatchUuid = s.EDespatchUuid,
        EDespatchStatus = s.EDespatchStatus,
        EDespatchProfile = s.EDespatchProfile,
        Lines = s.Lines.Select(ToLineDto).ToList(),
        CreatedAtUtc = s.CreatedAtUtc,
        UpdatedAtUtc = s.UpdatedAtUtc,
    };

    public static ShipmentLineDto ToLineDto(ShipmentLine l) => new()
    {
        Id = l.Id,
        OrderLineId = l.OrderLineId,
        ProductId = l.ProductId,
        ProductSku = l.ProductSku,
        ProductName = l.ProductName,
        LotId = l.LotId,
        LotNumber = l.Lot?.LotNumber,
        SerialNumber = l.SerialNumber,
        Quantity = l.Quantity,
        UnitCostSnapshot = l.UnitCostSnapshot,
        Notes = l.Notes,
    };
}
