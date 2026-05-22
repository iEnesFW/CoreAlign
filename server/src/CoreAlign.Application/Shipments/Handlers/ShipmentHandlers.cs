using CoreAlign.Application.Common;
using CoreAlign.Application.Shipments.Commands;
using CoreAlign.Application.Shipments.DTOs;
using CoreAlign.Application.Shipments.Mapping;
using CoreAlign.Application.Shipments.Queries;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Shipments.Handlers;

public class CreateShipmentHandler : IRequestHandler<CreateShipmentCommand, ShipmentDto>
{
    private readonly IOrderRepository _orders;
    private readonly IShipmentRepository _shipments;
    private readonly IDocumentSequenceRepository _sequences;
    private readonly IUnitOfWork _uow;

    public CreateShipmentHandler(IOrderRepository orders, IShipmentRepository shipments, IDocumentSequenceRepository sequences, IUnitOfWork uow)
    {
        _orders = orders;
        _shipments = shipments;
        _sequences = sequences;
        _uow = uow;
    }

    public async Task<ShipmentDto> Handle(CreateShipmentCommand c, CancellationToken ct)
    {
        var order = await _orders.GetWithLinesAndShipmentsAsync(c.OrderId, ct)
            ?? throw new OrderNotFoundException();

        if (order.Status is OrderStatus.Draft or OrderStatus.Submitted or OrderStatus.Cancelled or OrderStatus.Closed)
        {
            throw new InvalidShipmentStateException($"Cannot create shipment while order is in {order.Status}.");
        }

        if (c.Lines.Count == 0)
        {
            throw new InvalidShipmentStateException("Shipment must include at least one line.");
        }

        foreach (var input in c.Lines)
        {
            var line = order.Lines.FirstOrDefault(l => l.Id == input.OrderLineId)
                ?? throw new InvalidShipmentStateException($"Order line {input.OrderLineId} not found.");
            if (input.Quantity > line.QuantityRemainingToShip)
            {
                throw new ShipmentLineQuantityExceededException(line.ProductSku, line.QuantityRemainingToShip, input.Quantity);
            }
        }

        var shipmentNumber = await _sequences.ConsumeAsync(DocumentSequenceType.ShipmentNumber, DateTime.UtcNow, ct);

        var shipment = new Shipment(shipmentNumber, order.Id, order.CustomerId, c.WarehouseId, order.ShippingAddressSnapshot);
        foreach (var input in c.Lines)
        {
            var orderLine = order.Lines.First(l => l.Id == input.OrderLineId);
            var shipLine = new ShipmentLine(
                orderLine.Id,
                orderLine.ProductId,
                orderLine.ProductSku,
                orderLine.ProductName,
                input.Quantity,
                orderLine.UnitCostSnapshot,
                input.LotId,
                input.SerialNumber,
                input.Notes);
            shipment.AddLine(shipLine);
        }
        shipment.UpdateMeta(c.Notes, order.ShippingAddressSnapshot);

        await _shipments.AddAsync(shipment, ct);
        await _uow.SaveChangesAsync(ct);
        shipment.Order = order;
        return ShipmentMapper.ToDto(shipment);
    }
}

public class PickShipmentHandler : IRequestHandler<PickShipmentCommand, ShipmentDto>
{
    private readonly IShipmentRepository _shipments;
    private readonly IUnitOfWork _uow;
    public PickShipmentHandler(IShipmentRepository shipments, IUnitOfWork uow) { _shipments = shipments; _uow = uow; }

    public async Task<ShipmentDto> Handle(PickShipmentCommand c, CancellationToken ct)
    {
        var shipment = await _shipments.GetWithLinesAsync(c.Id, ct) ?? throw new ShipmentNotFoundException();
        shipment.MarkPicked(c.PostedByUserId);
        _shipments.Update(shipment);
        await _uow.SaveChangesAsync(ct);
        return ShipmentMapper.ToDto(shipment);
    }
}

public class PackShipmentHandler : IRequestHandler<PackShipmentCommand, ShipmentDto>
{
    private readonly IShipmentRepository _shipments;
    private readonly IUnitOfWork _uow;
    public PackShipmentHandler(IShipmentRepository shipments, IUnitOfWork uow) { _shipments = shipments; _uow = uow; }

    public async Task<ShipmentDto> Handle(PackShipmentCommand c, CancellationToken ct)
    {
        var shipment = await _shipments.GetWithLinesAsync(c.Id, ct) ?? throw new ShipmentNotFoundException();
        shipment.MarkPacked();
        _shipments.Update(shipment);
        await _uow.SaveChangesAsync(ct);
        return ShipmentMapper.ToDto(shipment);
    }
}

public class DispatchShipmentHandler : IRequestHandler<DispatchShipmentCommand, ShipmentDto>
{
    private readonly IShipmentRepository _shipments;
    private readonly IOrderRepository _orders;
    private readonly IAllocationService _allocator;
    private readonly IUnitOfWork _uow;

    public DispatchShipmentHandler(IShipmentRepository shipments, IOrderRepository orders, IAllocationService allocator, IUnitOfWork uow)
    {
        _shipments = shipments;
        _orders = orders;
        _allocator = allocator;
        _uow = uow;
    }

    public async Task<ShipmentDto> Handle(DispatchShipmentCommand c, CancellationToken ct)
    {
        var shipment = await _shipments.GetWithLinesAsync(c.Id, ct) ?? throw new ShipmentNotFoundException();
        var order = await _orders.GetWithLinesAndShipmentsAsync(shipment.OrderId, ct) ?? throw new OrderNotFoundException();

        shipment.Dispatch(c.CarrierName, c.TrackingNumber, c.TrackingUrl, c.ShippingCost);

        foreach (var line in shipment.Lines)
        {
            var orderLine = order.Lines.FirstOrDefault(l => l.Id == line.OrderLineId);
            if (orderLine is null) continue;
            orderLine.RecordShipment(line.Quantity);
            await _allocator.ConsumeForOrderLineAsync(order.Id, orderLine.Id, line.Quantity, postedByUserId: null, ct);
        }

        var allLinesShipped = order.Lines.All(l => l.QuantityShipped + l.QuantityCancelled >= l.Quantity);
        order.MarkFullyShipped(shipment.Id, shipment.ShipmentNumber, isPartial: !allLinesShipped);

        _shipments.Update(shipment);
        _orders.Update(order);
        await _uow.SaveChangesAsync(ct);
        shipment.Order = order;
        return ShipmentMapper.ToDto(shipment);
    }
}

public class DeliverShipmentHandler : IRequestHandler<DeliverShipmentCommand, ShipmentDto>
{
    private readonly IShipmentRepository _shipments;
    private readonly IOrderRepository _orders;
    private readonly IUnitOfWork _uow;

    public DeliverShipmentHandler(IShipmentRepository shipments, IOrderRepository orders, IUnitOfWork uow)
    {
        _shipments = shipments;
        _orders = orders;
        _uow = uow;
    }

    public async Task<ShipmentDto> Handle(DeliverShipmentCommand c, CancellationToken ct)
    {
        var shipment = await _shipments.GetWithLinesAsync(c.Id, ct) ?? throw new ShipmentNotFoundException();
        shipment.MarkDelivered(c.ReceivedBy, c.DeliveredAtUtc);

        var order = await _orders.GetWithLinesAndShipmentsAsync(shipment.OrderId, ct);
        if (order is not null)
        {
            var allShipmentsDelivered = order.Shipments
                .Where(s => s.Status != ShipmentStatus.Cancelled)
                .All(s => s.Status == ShipmentStatus.Delivered);
            var allLinesShipped = order.Lines.All(l => l.QuantityShipped + l.QuantityCancelled >= l.Quantity);
            if (allShipmentsDelivered && allLinesShipped && order.Status == OrderStatus.Shipped)
            {
                order.ChangeStatus(OrderStatus.Delivered);
                _orders.Update(order);
            }
        }

        _shipments.Update(shipment);
        await _uow.SaveChangesAsync(ct);
        return ShipmentMapper.ToDto(shipment);
    }
}

public class CancelShipmentHandler : IRequestHandler<CancelShipmentCommand, ShipmentDto>
{
    private readonly IShipmentRepository _shipments;
    private readonly IUnitOfWork _uow;
    public CancelShipmentHandler(IShipmentRepository shipments, IUnitOfWork uow) { _shipments = shipments; _uow = uow; }

    public async Task<ShipmentDto> Handle(CancelShipmentCommand c, CancellationToken ct)
    {
        var shipment = await _shipments.GetWithLinesAsync(c.Id, ct) ?? throw new ShipmentNotFoundException();
        shipment.Cancel(c.Reason);
        _shipments.Update(shipment);
        await _uow.SaveChangesAsync(ct);
        return ShipmentMapper.ToDto(shipment);
    }
}

public class GetShipmentByIdHandler : IRequestHandler<GetShipmentByIdQuery, ShipmentDto?>
{
    private readonly IShipmentRepository _shipments;
    public GetShipmentByIdHandler(IShipmentRepository shipments) => _shipments = shipments;
    public async Task<ShipmentDto?> Handle(GetShipmentByIdQuery q, CancellationToken ct)
    {
        var s = await _shipments.GetWithLinesAsync(q.Id, ct);
        return s is null ? null : ShipmentMapper.ToDto(s);
    }
}

public class GetShipmentsByOrderHandler : IRequestHandler<GetShipmentsByOrderQuery, IReadOnlyList<ShipmentDto>>
{
    private readonly IShipmentRepository _shipments;
    public GetShipmentsByOrderHandler(IShipmentRepository shipments) => _shipments = shipments;
    public async Task<IReadOnlyList<ShipmentDto>> Handle(GetShipmentsByOrderQuery q, CancellationToken ct) =>
        (await _shipments.GetByOrderAsync(q.OrderId, ct)).Select(ShipmentMapper.ToDto).ToList();
}

public class SearchShipmentsHandler : IRequestHandler<SearchShipmentsQuery, PagedResult<ShipmentDto>>
{
    private readonly IShipmentRepository _shipments;
    public SearchShipmentsHandler(IShipmentRepository shipments) => _shipments = shipments;
    public async Task<PagedResult<ShipmentDto>> Handle(SearchShipmentsQuery q, CancellationToken ct)
    {
        var (items, total) = await _shipments.SearchAsync(q.Search, q.CustomerId, q.OrderId, q.Page, q.PageSize, ct);
        return new PagedResult<ShipmentDto>
        {
            Items = items.Select(ShipmentMapper.ToDto).ToList(),
            Total = total,
            Page = q.Page,
            PageSize = q.PageSize,
        };
    }
}
