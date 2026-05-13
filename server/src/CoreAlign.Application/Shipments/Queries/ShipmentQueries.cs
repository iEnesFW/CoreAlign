using CoreAlign.Application.Common;
using CoreAlign.Application.Shipments.DTOs;
using MediatR;

namespace CoreAlign.Application.Shipments.Queries;

public record GetShipmentByIdQuery(Guid Id) : IRequest<ShipmentDto?>;
public record GetShipmentsByOrderQuery(Guid OrderId) : IRequest<IReadOnlyList<ShipmentDto>>;
public record SearchShipmentsQuery(
    string? Search = null,
    Guid? CustomerId = null,
    Guid? OrderId = null,
    int Page = 1,
    int PageSize = 25) : IRequest<PagedResult<ShipmentDto>>;
