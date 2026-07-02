using CoreAlign.Application.Orders.Commands;
using CoreAlign.Application.Orders.DTOs;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Orders.Handlers;

public class CreateOrderFromPreviousCommandHandler : IRequestHandler<CreateOrderFromPreviousCommand, OrderDto>
{
    private readonly IOrderRepository _orders;
    private readonly IMediator _mediator;

    public CreateOrderFromPreviousCommandHandler(IOrderRepository orders, IMediator mediator)
    {
        _orders = orders;
        _mediator = mediator;
    }

    public async Task<OrderDto> Handle(CreateOrderFromPreviousCommand request, CancellationToken cancellationToken)
    {
        var previous = await _orders.GetWithLinesAsync(request.PreviousOrderId, cancellationToken)
            ?? throw new OrderNotFoundException();

        // Reorder copies only real product lines into a fresh Draft (service lines carry
        // ProductId=Guid.Empty and would fail the create handler's product lookup).
        var lines = previous.Lines
            .Where(l => !l.IsService && l.ProductId != Guid.Empty)
            .OrderBy(l => l.LineNumber)
            .Select(l => new OrderLineInput(
                ProductId: l.ProductId,
                Quantity: l.Quantity,
                UnitPrice: l.UnitPrice,
                LineDiscountPercent: l.LineDiscountPercent,
                TaxRatePercent: l.TaxRatePercent,
                IsTaxInclusive: l.IsTaxInclusive,
                WithholdingRatePercent: l.WithholdingRatePercent,
                TaxRateId: l.TaxRateId,
                UomId: l.UomId,
                UomCode: l.UomCode,
                WarehouseId: l.WarehouseId,
                LineNotes: l.LineNotes))
            .ToList();

        if (lines.Count == 0)
        {
            throw new InvalidOrderLineException("The source order has no reorderable product lines.");
        }

        var create = new CreateOrderCommand(
            OrderNumber: string.Empty,
            CustomerId: previous.CustomerId,
            OrderDate: DateTime.UtcNow,
            Currency: previous.Currency,
            Notes: previous.Notes,
            Lines: lines,
            Type: previous.Type,
            Source: previous.Source,
            PaymentTermsId: previous.PaymentTermsId,
            PriceListId: previous.PriceListId,
            BillingAddressId: previous.BillingAddressId,
            ShippingAddressId: previous.ShippingAddressId,
            ExchangeRate: previous.ExchangeRate,
            ShippingCost: previous.ShippingCost,
            HeaderDiscountPercent: previous.HeaderDiscountPercent,
            Channel: previous.Channel,
            CustomerNotes: previous.CustomerNotes,
            OriginOrderId: previous.Id);

        return await _mediator.Send(create, cancellationToken);
    }
}
