using CoreAlign.Application.Common;
using CoreAlign.Application.Shipments.DTOs;
using MediatR;

namespace CoreAlign.Application.Shipments.Commands;

public record ShipmentLineInput(
    Guid OrderLineId,
    decimal Quantity,
    Guid? LotId = null,
    string? SerialNumber = null,
    string? Notes = null);

public record CreateShipmentCommand(
    Guid OrderId,
    Guid WarehouseId,
    List<ShipmentLineInput> Lines,
    string? Notes = null
) : IRequest<ShipmentDto>, ITransactionalRequest;

public record PickShipmentCommand(Guid Id, Guid? PostedByUserId = null) : IRequest<ShipmentDto>, ITransactionalRequest;
public record PackShipmentCommand(Guid Id) : IRequest<ShipmentDto>, ITransactionalRequest;

public record DispatchShipmentCommand(
    Guid Id,
    string? CarrierName,
    string? TrackingNumber,
    string? TrackingUrl,
    decimal? ShippingCost) : IRequest<ShipmentDto>, ITransactionalRequest;

public record DeliverShipmentCommand(
    Guid Id,
    string? ReceivedBy,
    DateTime? DeliveredAtUtc) : IRequest<ShipmentDto>, ITransactionalRequest;

public record CancelShipmentCommand(Guid Id, string? Reason) : IRequest<ShipmentDto>, ITransactionalRequest;
