using CoreAlign.Application.Accounting.Services;
using CoreAlign.Application.Common;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Application.Orders.EventHandlers;
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

        // WHY open shipments are subtracted too: QuantityShipped only moves on dispatch, so without
        // this an undispatched shipment leaves the whole quantity claimable a second time and the
        // duplicate is only rejected later, after the warehouse has already picked and packed it.
        var claimedByOpenShipments = order.Shipments
            .Where(s => s.Status is ShipmentStatus.Draft or ShipmentStatus.Picked or ShipmentStatus.Packed)
            .SelectMany(s => s.Lines)
            .GroupBy(l => l.OrderLineId)
            .ToDictionary(g => g.Key, g => g.Sum(l => l.Quantity));

        foreach (var input in c.Lines)
        {
            var line = order.Lines.FirstOrDefault(l => l.Id == input.OrderLineId)
                ?? throw new InvalidShipmentStateException($"Order line {input.OrderLineId} not found.");
            var claimed = claimedByOpenShipments.GetValueOrDefault(line.Id);
            var available = Math.Max(0m, line.QuantityRemainingToShip - claimed);
            if (input.Quantity > available)
            {
                throw new ShipmentLineQuantityExceededException(line.ProductSku, available, input.Quantity);
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
    private readonly IGLPostingOutbox _glOutbox;
    private readonly IUnitOfWork _uow;

    public DispatchShipmentHandler(IShipmentRepository shipments, IOrderRepository orders, IAllocationService allocator, IGLPostingOutbox glOutbox, IUnitOfWork uow)
    {
        _shipments = shipments;
        _orders = orders;
        _allocator = allocator;
        _glOutbox = glOutbox;
        _uow = uow;
    }

    public async Task<ShipmentDto> Handle(DispatchShipmentCommand c, CancellationToken ct)
    {
        var shipment = await _shipments.GetWithLinesAsync(c.Id, ct) ?? throw new ShipmentNotFoundException();
        var order = await _orders.GetWithLinesAndShipmentsAsync(shipment.OrderId, ct) ?? throw new OrderNotFoundException();

        // WHY the order status is checked here: Shipment.Dispatch only guards the shipment FSM, so a
        // packed shipment on a terminated order still dispatched — consuming nothing, posting no
        // COGS, and leaving MarkFullyShipped a silent no-op while the goods physically left.
        if (order.Status is OrderStatus.Cancelled or OrderStatus.Closed or OrderStatus.Returned)
        {
            throw new ShipmentOrderNotDispatchableException(shipment.ShipmentNumber, order.Status.ToString());
        }

        shipment.Dispatch(c.CarrierName, c.TrackingNumber, c.TrackingUrl, c.ShippingCost);

        // Σ issue cost across the consumed reservations; relieved to COGS below.
        var cogsCost = 0m;
        foreach (var line in shipment.Lines)
        {
            var orderLine = order.Lines.FirstOrDefault(l => l.Id == line.OrderLineId);
            if (orderLine is null) continue;
            orderLine.RecordShipment(line.Quantity);
            var consumption = await _allocator.ConsumeForOrderLineAsync(order.Id, orderLine.Id, line.Quantity, postedByUserId: null, ct);
            cogsCost += consumption.Cost;
        }

        // COGS recognition for the reserve→ship flow (stock relieved at dispatch,
        // not at confirm). Keyed by (CostOfGoodsSold, ShipmentId) so it is
        // idempotent per shipment and independent of any confirm-time posting.
        if (cogsCost > 0m)
        {
            await _glOutbox.EnqueueAsync(new GLPostingRequest(
                JournalSourceType.CostOfGoodsSold,
                shipment.Id,
                shipment.ShipmentNumber,
                DateTime.UtcNow.Date,
                JournalEntryType.Mahsup,
                $"Satış maliyeti ({shipment.ShipmentNumber})",
                CogsGLLines.Build(cogsCost, reverse: false)), ct);
        }

        var allLinesShipped = order.Lines.All(l => l.IsFullyShipped);
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
            var allLinesShipped = order.Lines.All(l => l.IsFullyShipped);
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
        var s = await _shipments.GetWithLinesAsync(q.Id, ct) ?? throw new ShipmentNotFoundException();
        return ShipmentMapper.ToDto(s);
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
