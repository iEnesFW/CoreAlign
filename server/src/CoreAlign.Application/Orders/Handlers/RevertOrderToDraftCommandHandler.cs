using CoreAlign.Application.Orders.Commands;
using CoreAlign.Application.Orders.DTOs;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Orders.Handlers;

public class RevertOrderToDraftCommandHandler : IRequestHandler<RevertOrderToDraftCommand, OrderDto>
{
    private readonly IOrderRepository _orders;
    private readonly IInvoiceRepository _invoices;
    private readonly IShipmentRepository _shipments;
    private readonly IAllocationService _allocationService;
    private readonly IUnitOfWork _uow;

    public RevertOrderToDraftCommandHandler(
        IOrderRepository orders,
        IInvoiceRepository invoices,
        IShipmentRepository shipments,
        IAllocationService allocationService,
        IUnitOfWork uow)
    {
        _orders = orders;
        _invoices = invoices;
        _shipments = shipments;
        _allocationService = allocationService;
        _uow = uow;
    }

    public async Task<OrderDto> Handle(RevertOrderToDraftCommand c, CancellationToken ct)
    {
        var order = await _orders.GetWithLinesAsync(c.Id, ct) ?? throw new OrderNotFoundException();

        var invoice = await _invoices.GetByOrderIdAsync(order.Id, ct);
        if (invoice is not null && invoice.Status is not (InvoiceStatus.Cancelled or InvoiceStatus.Void))
        {
            throw new OrderRevertBlockedException(
                $"Order has invoice '{invoice.InvoiceNumber}'. Cancel or void the invoice before reverting the order to draft.");
        }

        var shipments = await _shipments.GetByOrderAsync(order.Id, ct);
        var activeShipment = shipments.FirstOrDefault(s => s.Status != ShipmentStatus.Cancelled);
        if (activeShipment is not null)
        {
            throw new OrderRevertBlockedException(
                $"Order has shipment '{activeShipment.ShipmentNumber}'. Cancel the shipment before reverting the order to draft.");
        }

        if (order.Status == OrderStatus.Allocated)
        {
            await _allocationService.ReleaseByOrderAsync(order.Id, ct);
        }

        order.RevertToDraft();
        _orders.Update(order);
        await _uow.SaveChangesAsync(ct);
        return OrderMapper.ToDto(order);
    }
}
