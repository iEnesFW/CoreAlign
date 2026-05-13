using CoreAlign.Application.Orders.Commands;
using CoreAlign.Application.Orders.DTOs;
using CoreAlign.Domain.Common;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Orders.Handlers;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, OrderDto>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IProductRepository _productRepository;
    private readonly ICustomerAddressRepository _addressRepository;
    private readonly IPaymentTermRepository _paymentTermRepository;
    private readonly IDocumentSequenceRepository _sequenceRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateOrderCommandHandler(
        IOrderRepository orderRepository,
        ICustomerRepository customerRepository,
        IProductRepository productRepository,
        ICustomerAddressRepository addressRepository,
        IPaymentTermRepository paymentTermRepository,
        IDocumentSequenceRepository sequenceRepository,
        IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _customerRepository = customerRepository;
        _productRepository = productRepository;
        _addressRepository = addressRepository;
        _paymentTermRepository = paymentTermRepository;
        _sequenceRepository = sequenceRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<OrderDto> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken)
            ?? throw new CustomerNotFoundException();

        var productIds = request.Lines.Select(l => l.ProductId).Distinct().ToList();
        var products = await _productRepository.GetByIdsAsync(productIds, cancellationToken);
        if (products.Count != productIds.Count)
        {
            throw new InvalidOrderLineException("One or more products were not found.");
        }

        var orderNumber = string.IsNullOrWhiteSpace(request.OrderNumber)
            ? await _sequenceRepository.ConsumeAsync(DocumentSequenceType.OrderNumber, DateTime.UtcNow, cancellationToken)
            : request.OrderNumber;

        if (!string.IsNullOrWhiteSpace(request.OrderNumber) &&
            await _orderRepository.OrderNumberExistsAsync(orderNumber, null, cancellationToken))
        {
            throw new DuplicateOrderNumberException();
        }

        var order = new Order(orderNumber, request.CustomerId, request.OrderDate, request.Currency, request.Notes);

        order.UpdateDetails(
            request.Type,
            request.Source,
            request.RequestedDeliveryDate,
            request.PromisedDeliveryDate,
            request.BillingAddressId,
            request.ShippingAddressId,
            request.PaymentTermsId ?? customer.PaymentTermsId,
            request.PriceListId ?? customer.PriceListId,
            request.ExchangeRate,
            request.ShippingCost,
            request.HeaderDiscountPercent,
            request.HeaderDiscountAmount,
            request.SalesRepUserId,
            request.Channel,
            request.InternalNotes,
            request.CustomerNotes,
            request.OriginOrderId);

        var billingAddress = request.BillingAddressId.HasValue
            ? await _addressRepository.GetByIdAsync(request.BillingAddressId.Value, cancellationToken)
            : null;
        var shippingAddress = request.ShippingAddressId.HasValue
            ? await _addressRepository.GetByIdAsync(request.ShippingAddressId.Value, cancellationToken)
            : null;

        var paymentTermsId = request.PaymentTermsId ?? customer.PaymentTermsId;
        PaymentTerm? paymentTerm = null;
        if (paymentTermsId.HasValue)
        {
            paymentTerm = await _paymentTermRepository.GetByIdAsync(paymentTermsId.Value, cancellationToken);
        }

        order.ApplySnapshots(
            new CustomerSnapshot
            {
                Code = customer.Code,
                LegalName = customer.LegalName ?? customer.Name,
                TradeName = customer.TradeName,
                TaxNumber = customer.TaxNumber,
                TaxOffice = customer.TaxOffice,
                NationalId = customer.NationalId,
                Email = customer.Email,
                Phone = customer.Phone,
            },
            billingAddress is null ? null : ToSnapshot(billingAddress),
            shippingAddress is null ? null : ToSnapshot(shippingAddress),
            paymentTerm?.NetDays,
            paymentTerm?.ResolveDueDate(request.OrderDate));

        var lines = request.Lines.Select((input, idx) =>
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
        }).ToList();

        order.ReplaceLines(lines);
        await _orderRepository.AddAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        order.Customer = customer;
        return OrderMapper.ToDto(order);
    }

    private static AddressSnapshot ToSnapshot(CustomerAddress a) => new()
    {
        Label = a.Label,
        Line1 = a.Line1,
        Line2 = a.Line2,
        City = a.City,
        State = a.State,
        PostalCode = a.PostalCode,
        Country = a.Country,
    };
}
