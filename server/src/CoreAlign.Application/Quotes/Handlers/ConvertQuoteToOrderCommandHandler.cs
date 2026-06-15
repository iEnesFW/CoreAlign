using CoreAlign.Application.Orders.DTOs;
using CoreAlign.Application.Orders.Handlers;
using CoreAlign.Application.Quotes.Commands;
using CoreAlign.Domain.Common;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Quotes.Handlers;

public class ConvertQuoteToOrderCommandHandler : IRequestHandler<ConvertQuoteToOrderCommand, OrderDto>
{
    private readonly IQuoteRepository _quoteRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IDocumentSequenceRepository _sequenceRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ConvertQuoteToOrderCommandHandler(
        IQuoteRepository quoteRepository,
        IOrderRepository orderRepository,
        ICustomerRepository customerRepository,
        IDocumentSequenceRepository sequenceRepository,
        IUnitOfWork unitOfWork)
    {
        _quoteRepository = quoteRepository;
        _orderRepository = orderRepository;
        _customerRepository = customerRepository;
        _sequenceRepository = sequenceRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<OrderDto> Handle(ConvertQuoteToOrderCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        await _quoteRepository.AcquireConversionLockAsync(request.Id, cancellationToken);

        var quote = await _quoteRepository.GetWithLinesAsync(request.Id, cancellationToken)
            ?? throw new QuoteNotFoundException();

        if (quote.Status != QuoteStatus.Accepted)
        {
            throw new InvalidQuoteStatusTransitionException(quote.Status.ToString(), "Convert");
        }
        if (quote.ConvertedOrderId.HasValue)
        {
            throw new QuoteAlreadyConvertedException();
        }

        var customer = await _customerRepository.GetByIdAsync(quote.CustomerId, cancellationToken)
            ?? throw new CustomerNotFoundException();

        var orderNumber = await _sequenceRepository.ConsumeAsync(
            DocumentSequenceType.OrderNumber, DateTime.UtcNow, cancellationToken);

        var order = new Order(orderNumber, quote.CustomerId, DateTime.UtcNow, quote.Currency, quote.Notes);

        order.UpdateDetails(
            type: OrderType.Standard,
            source: OrderSource.Manual,
            requestedDeliveryDate: null,
            promisedDeliveryDate: null,
            billingAddressId: quote.BillingAddressId,
            shippingAddressId: quote.ShippingAddressId,
            paymentTermsId: quote.PaymentTermsId,
            priceListId: quote.PriceListId,
            exchangeRate: quote.ExchangeRate,
            shippingCost: quote.ShippingCost,
            headerDiscountPercent: quote.HeaderDiscountPercent,
            headerDiscountAmount: quote.HeaderDiscountAmount,
            salesRepUserId: quote.SalesRepUserId,
            channel: null,
            internalNotes: quote.InternalNotes,
            customerNotes: quote.CustomerNotes,
            originOrderId: null,
            roundingAdjustment: quote.RoundingAdjustment);

        if (quote.CustomerSnapshot != null)
        {
            order.ApplySnapshots(
                quote.CustomerSnapshot,
                quote.BillingAddressSnapshot,
                quote.ShippingAddressSnapshot,
                quote.PaymentTermsNetDaysSnapshot,
                dueDate: null);
        }
        else
        {
            var cs = new CustomerSnapshot
            {
                Code = customer.Code,
                LegalName = customer.LegalName ?? customer.Name,
                TradeName = customer.TradeName,
                TaxNumber = customer.TaxNumber,
                TaxOffice = customer.TaxOffice,
                NationalId = customer.NationalId,
                Email = customer.Email,
                Phone = customer.Phone,
            };
            order.ApplySnapshots(cs, null, null, quote.PaymentTermsNetDaysSnapshot, null);
        }

        var orderLines = quote.Lines
            .OrderBy(l => l.LineNumber)
            .Select((ql, idx) =>
            {
                var line = new OrderLine(ql.ProductId, ql.ProductSku, ql.ProductName, ql.Quantity, ql.UnitPrice);
                line.SetLineNumber(idx + 1);
                line.ApplyPricing(
                    quantity: ql.Quantity,
                    listPriceSnapshot: ql.ListPriceSnapshot,
                    unitPrice: ql.UnitPrice,
                    lineDiscountPercent: ql.LineDiscountPercent,
                    lineDiscountAmount: ql.LineDiscountAmount,
                    isManualPriceOverride: ql.IsManualPriceOverride,
                    taxRatePercent: ql.TaxRatePercent,
                    taxRateId: ql.TaxRateId,
                    isTaxInclusive: ql.IsTaxInclusive,
                    withholdingRatePercent: ql.WithholdingRatePercent,
                    unitCostSnapshot: 0m,
                    uomId: ql.UomId,
                    uomCode: ql.UomCode,
                    uomConversionFactor: ql.UomConversionFactor,
                    warehouseId: null,
                    lineNotes: ql.LineNotes,
                    parentLineId: null,
                    isKitComponent: false,
                    productDescriptionSnapshot: ql.ProductDescriptionSnapshot);
                return line;
            })
            .ToList();

        order.ReplaceLines(orderLines);
        order.LinkSourceQuote(quote.Id);

        await _orderRepository.AddAsync(order, cancellationToken);
        quote.AttachConvertedOrder(order.Id);
        _quoteRepository.Update(quote);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        order.Customer = customer;
        return OrderMapper.ToDto(order);
    }
}
