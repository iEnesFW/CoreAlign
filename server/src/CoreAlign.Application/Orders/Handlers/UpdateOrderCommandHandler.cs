using CoreAlign.Application.Orders.Commands;
using CoreAlign.Application.Orders.DTOs;
using CoreAlign.Application.Orders.EventHandlers;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Events;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Orders.Handlers;

public class UpdateOrderCommandHandler : IRequestHandler<UpdateOrderCommand, OrderDto>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IProductRepository _productRepository;
    private readonly IProductComponentRepository _componentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateOrderCommandHandler(
        IOrderRepository orderRepository,
        ICustomerRepository customerRepository,
        IProductRepository productRepository,
        IProductComponentRepository componentRepository,
        IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _customerRepository = customerRepository;
        _productRepository = productRepository;
        _componentRepository = componentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<OrderDto> Handle(UpdateOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new OrderNotFoundException();

        var incomingLines = request.Lines
            .Select(l => (l.ProductId, l.Quantity, l.UnitPrice))
            .ToList();

        var headerOrLinesChanged =
            !order.HasSameHeader(request.OrderNumber, request.CustomerId, request.OrderDate, request.Currency, request.Notes)
            || !order.HasSameLines(incomingLines);

        if (!order.IsDraft && headerOrLinesChanged)
        {
            throw new OrderImmutableException(order.Status.ToString());
        }

        if (order.IsDraft && headerOrLinesChanged)
        {
            await ApplyDraftUpdateAsync(order, request, cancellationToken);
        }

        if (WillTransitionToConfirmed(order.Status, request.Status))
        {
            await EnsureSufficientStockAsync(order, cancellationToken);
        }

        if (order.Status != request.Status)
        {
            order.ChangeStatus(request.Status);
        }

        _orderRepository.Update(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (order.Customer is null)
        {
            var loadedCustomer = await _customerRepository.GetByIdAsync(order.CustomerId, cancellationToken);
            if (loadedCustomer is not null)
            {
                order.Customer = loadedCustomer;
            }
        }

        return OrderMapper.ToDto(order);
    }

    private static bool WillTransitionToConfirmed(OrderStatus from, OrderStatus to)
        => from == OrderStatus.Draft && to == OrderStatus.Confirmed;

    private async Task EnsureSufficientStockAsync(Order order, CancellationToken cancellationToken)
    {
        var lineProductIds = order.Lines.Select(l => l.ProductId).Distinct().ToList();
        var bomTree = await _componentRepository.GetTreeForProductsAsync(lineProductIds, cancellationToken);
        var snapshots = order.Lines.Select(l => new OrderLineSnapshot(l.ProductId, l.Quantity));
        var leafTotals = BomResolver.ExpandToLeaves(snapshots, bomTree);

        var leafProductIds = leafTotals.Keys.ToList();
        var products = await _productRepository.GetByIdsAsync(leafProductIds, cancellationToken);
        foreach (var (productId, required) in leafTotals)
        {
            var product = products[productId];
            if (product.IsStockTracked && product.StockQuantity < required)
            {
                throw new InsufficientStockException(product.Name, product.StockQuantity, required);
            }
        }
    }

    private async Task ApplyDraftUpdateAsync(Order order, UpdateOrderCommand request, CancellationToken cancellationToken)
    {
        if (!string.Equals(order.OrderNumber, request.OrderNumber, StringComparison.OrdinalIgnoreCase) &&
            await _orderRepository.OrderNumberExistsAsync(request.OrderNumber, request.Id, cancellationToken))
        {
            throw new DuplicateOrderNumberException();
        }

        var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken)
            ?? throw new CustomerNotFoundException();

        var productIds = request.Lines.Select(l => l.ProductId).Distinct().ToList();
        var products = await _productRepository.GetByIdsAsync(productIds, cancellationToken);
        if (products.Count != productIds.Count)
        {
            throw new InvalidOrderLineException("One or more products were not found.");
        }

        order.UpdateHeader(request.OrderNumber, request.CustomerId, request.OrderDate, request.Currency, request.Notes);
        order.UpdateDetails(
            request.Type,
            request.Source,
            request.RequestedDeliveryDate,
            request.PromisedDeliveryDate,
            request.BillingAddressId,
            request.ShippingAddressId,
            request.PaymentTermsId,
            request.PriceListId,
            request.ExchangeRate,
            request.ShippingCost,
            request.HeaderDiscountPercent,
            request.HeaderDiscountAmount,
            request.SalesRepUserId,
            request.Channel,
            request.InternalNotes,
            request.CustomerNotes,
            request.OriginOrderId);

        var newLines = request.Lines.Select((input, idx) =>
        {
            var product = products[input.ProductId];
            var line = new OrderLine(product.Id, product.Sku, product.Name, input.Quantity, input.UnitPrice);
            line.SetLineNumber(idx + 1);
            line.ApplyPricing(
                input.Quantity,
                product.ListPrice == 0m ? input.UnitPrice : product.ListPrice,
                input.UnitPrice,
                input.LineDiscountPercent,
                input.LineDiscountAmount,
                input.IsManualPriceOverride,
                input.TaxRatePercent,
                input.TaxRateId,
                input.IsTaxInclusive,
                input.WithholdingRatePercent,
                input.UnitCostSnapshot == 0m ? product.AverageCost : input.UnitCostSnapshot,
                input.UomId,
                input.UomCode,
                input.UomConversionFactor,
                input.WarehouseId,
                input.LineNotes,
                null,
                false,
                product.Description);
            return line;
        });
        order.ReplaceLines(newLines);
        order.Customer = customer;
    }
}
