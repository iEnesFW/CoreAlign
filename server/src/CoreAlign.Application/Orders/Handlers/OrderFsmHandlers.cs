using CoreAlign.Application.Orders.Commands;
using CoreAlign.Application.Orders.DTOs;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Orders.Handlers;

public class SubmitOrderHandler : IRequestHandler<SubmitOrderCommand, OrderDto>
{
    private readonly IOrderRepository _orders;
    private readonly IUnitOfWork _uow;
    public SubmitOrderHandler(IOrderRepository orders, IUnitOfWork uow) { _orders = orders; _uow = uow; }

    public async Task<OrderDto> Handle(SubmitOrderCommand c, CancellationToken ct)
    {
        var order = await _orders.GetWithLinesAsync(c.Id, ct) ?? throw new OrderNotFoundException();
        order.Submit();
        _orders.Update(order);
        await _uow.SaveChangesAsync(ct);
        return OrderMapper.ToDto(order);
    }
}

public class ApproveOrderHandler : IRequestHandler<ApproveOrderCommand, OrderDto>
{
    private readonly IOrderRepository _orders;
    private readonly IUnitOfWork _uow;
    public ApproveOrderHandler(IOrderRepository orders, IUnitOfWork uow) { _orders = orders; _uow = uow; }

    public async Task<OrderDto> Handle(ApproveOrderCommand c, CancellationToken ct)
    {
        var order = await _orders.GetWithLinesAsync(c.Id, ct) ?? throw new OrderNotFoundException();
        order.Approve(c.ApprovedByUserId ?? Guid.Empty);
        _orders.Update(order);
        await _uow.SaveChangesAsync(ct);
        return OrderMapper.ToDto(order);
    }
}

public class AllocateOrderHandler : IRequestHandler<AllocateOrderCommand, OrderDto>
{
    private readonly IOrderRepository _orders;
    private readonly IWarehouseRepository _warehouses;
    private readonly IAllocationService _allocator;
    private readonly IUnitOfWork _uow;

    public AllocateOrderHandler(IOrderRepository orders, IWarehouseRepository warehouses, IAllocationService allocator, IUnitOfWork uow)
    {
        _orders = orders;
        _warehouses = warehouses;
        _allocator = allocator;
        _uow = uow;
    }

    public async Task<OrderDto> Handle(AllocateOrderCommand c, CancellationToken ct)
    {
        var order = await _orders.GetWithLinesAsync(c.Id, ct) ?? throw new OrderNotFoundException();

        if (order.Status != OrderStatus.Approved)
        {
            throw new InvalidOrderStatusTransitionException(order.Status.ToString(), OrderStatus.Allocated.ToString());
        }

        var defaultWarehouse = c.PreferredWarehouseId.HasValue
            ? await _warehouses.GetByIdAsync(c.PreferredWarehouseId.Value, ct)
            : await _warehouses.GetDefaultAsync(ct);

        if (defaultWarehouse is null)
        {
            throw new InvalidOrderStatusTransitionException("Approved", "Allocated (no default warehouse)");
        }

        foreach (var line in order.Lines.Where(l => l.QuantityAllocated < l.Quantity))
        {
            var qty = line.Quantity - line.QuantityAllocated;
            var warehouseId = line.WarehouseId ?? defaultWarehouse.Id;
            await _allocator.ReserveAsync(new AllocationRequest(order.Id, line.Id, line.ProductId, warehouseId, qty), ct);
            line.RecordAllocation(qty);
        }

        order.MarkAllocated(c.PreferredWarehouseId);
        _orders.Update(order);
        await _uow.SaveChangesAsync(ct);
        return OrderMapper.ToDto(order);
    }
}

public class CancelOrderHandler : IRequestHandler<CancelOrderCommand, OrderDto>
{
    private readonly IOrderRepository _orders;
    private readonly IAllocationService _allocator;
    private readonly IUnitOfWork _uow;

    public CancelOrderHandler(IOrderRepository orders, IAllocationService allocator, IUnitOfWork uow)
    {
        _orders = orders;
        _allocator = allocator;
        _uow = uow;
    }

    public async Task<OrderDto> Handle(CancelOrderCommand c, CancellationToken ct)
    {
        var order = await _orders.GetWithLinesAsync(c.Id, ct) ?? throw new OrderNotFoundException();
        await _allocator.ReleaseByOrderAsync(order.Id, ct);
        order.Cancel(c.Reason);
        _orders.Update(order);
        await _uow.SaveChangesAsync(ct);
        return OrderMapper.ToDto(order);
    }
}

public class DeliverOrderHandler : IRequestHandler<DeliverOrderCommand, OrderDto>
{
    private readonly IOrderRepository _orders;
    private readonly IUnitOfWork _uow;
    public DeliverOrderHandler(IOrderRepository orders, IUnitOfWork uow) { _orders = orders; _uow = uow; }

    public async Task<OrderDto> Handle(DeliverOrderCommand c, CancellationToken ct)
    {
        var order = await _orders.GetWithLinesAsync(c.Id, ct) ?? throw new OrderNotFoundException();
        order.ChangeStatus(OrderStatus.Delivered);
        _orders.Update(order);
        await _uow.SaveChangesAsync(ct);
        return OrderMapper.ToDto(order);
    }
}

public class CloseOrderHandler : IRequestHandler<CloseOrderCommand, OrderDto>
{
    private readonly IOrderRepository _orders;
    private readonly IUnitOfWork _uow;
    public CloseOrderHandler(IOrderRepository orders, IUnitOfWork uow) { _orders = orders; _uow = uow; }

    public async Task<OrderDto> Handle(CloseOrderCommand c, CancellationToken ct)
    {
        var order = await _orders.GetWithLinesAsync(c.Id, ct) ?? throw new OrderNotFoundException();
        order.ChangeStatus(OrderStatus.Closed);
        _orders.Update(order);
        await _uow.SaveChangesAsync(ct);
        return OrderMapper.ToDto(order);
    }
}
