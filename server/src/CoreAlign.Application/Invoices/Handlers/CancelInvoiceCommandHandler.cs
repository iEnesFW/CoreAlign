using CoreAlign.Application.Common;
using CoreAlign.Application.Invoices.Commands;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Invoices.Handlers;

public class CancelInvoiceCommandHandler : IRequestHandler<CancelInvoiceCommand, bool>
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CancelInvoiceCommandHandler(
        IInvoiceRepository invoiceRepository,
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork)
    {
        _invoiceRepository = invoiceRepository;
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(CancelInvoiceCommand request, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceRepository.GetWithLinesAsync(request.Id, cancellationToken)
            ?? throw new InvoiceNotFoundException();

        if (invoice.Status is InvoiceStatus.Paid or InvoiceStatus.Cancelled)
        {
            throw new InvoiceStatusTransitionException(invoice.Status.ToString(), "cancel");
        }

        invoice.Cancel(DateTime.UtcNow);
        _invoiceRepository.Update(invoice);
        await ReleaseOrderQuantityAsync(invoice, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    // WHY the order has to be released: the from-order generator advances OrderLine.QuantityInvoiced
    // and ExistsForOrderAsync blocks a second invoice, so a cancelled invoice left the order
    // permanently unbillable — the shipped goods could never be charged for. Giving the quantity
    // back (and letting the guard ignore cancelled invoices) makes the correction possible.
    private async Task ReleaseOrderQuantityAsync(Invoice invoice, CancellationToken cancellationToken)
    {
        if (invoice.OrderId is not Guid orderId) return;

        var order = await _orderRepository.GetWithLinesAsync(orderId, cancellationToken);
        if (order is null) return;

        var released = false;
        foreach (var line in invoice.Lines)
        {
            if (line.OriginOrderLineId is not Guid orderLineId) continue;
            var orderLine = order.Lines.FirstOrDefault(l => l.Id == orderLineId);
            if (orderLine is null) continue;
            orderLine.ReverseInvoice(line.Quantity);
            released = true;
        }

        if (released)
        {
            _orderRepository.Update(order);
        }
    }
}
