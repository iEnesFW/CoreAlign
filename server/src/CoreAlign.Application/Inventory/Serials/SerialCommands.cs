using CoreAlign.Application.Common;
using MediatR;

namespace CoreAlign.Application.Inventory.Serials;

// Registers serialized units for a serial-tracked product (typically at goods receipt). Duplicate
// serials (already registered for the product) are rejected.
public record RegisterSerialUnitsCommand(
    Guid ProductId,
    IReadOnlyList<string> SerialNumbers,
    Guid? WarehouseId = null,
    Guid? LotId = null,
    decimal UnitCost = 0m,
    Guid? SourceReceiptMovementId = null,
    Guid? ParentSerialUnitId = null) : IRequest<int>, ITransactionalRequest;

// Marks units shipped to a customer and stamps the where-used links (order / shipment / owner).
public record ShipSerialUnitsCommand(
    Guid ProductId,
    IReadOnlyList<string> SerialNumbers,
    Guid OrderId,
    Guid? ShipmentId = null,
    Guid? CustomerId = null) : IRequest<int>, ITransactionalRequest;

public record SerialWhereUsedDto(
    Guid Id,
    Guid ProductId,
    string SerialNumber,
    string Status,
    Guid? WarehouseId,
    Guid? LotId,
    decimal UnitCost,
    DateTime ReceivedAtUtc,
    Guid? OrderId,
    Guid? ShipmentId,
    Guid? CurrentOwnerCustomerId,
    Guid? ParentSerialUnitId,
    IReadOnlyList<SerialComponentDto> Components);

public record SerialComponentDto(Guid Id, Guid ProductId, string SerialNumber, string Status);

public record GetSerialWhereUsedQuery(string SerialNumber) : IRequest<IReadOnlyList<SerialWhereUsedDto>>;
