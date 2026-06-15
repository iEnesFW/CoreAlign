using CoreAlign.Application.Quotes.Commands;
using CoreAlign.Application.Quotes.DTOs;
using CoreAlign.Domain.Common;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Quotes.Handlers;

public class CreateQuoteCommandHandler : IRequestHandler<CreateQuoteCommand, QuoteDto>
{
    private readonly IQuoteRepository _quoteRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IProductRepository _productRepository;
    private readonly ICustomerAddressRepository _addressRepository;
    private readonly IPaymentTermRepository _paymentTermRepository;
    private readonly IDocumentSequenceRepository _sequenceRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateQuoteCommandHandler(
        IQuoteRepository quoteRepository,
        ICustomerRepository customerRepository,
        IProductRepository productRepository,
        ICustomerAddressRepository addressRepository,
        IPaymentTermRepository paymentTermRepository,
        IDocumentSequenceRepository sequenceRepository,
        IUnitOfWork unitOfWork)
    {
        _quoteRepository = quoteRepository;
        _customerRepository = customerRepository;
        _productRepository = productRepository;
        _addressRepository = addressRepository;
        _paymentTermRepository = paymentTermRepository;
        _sequenceRepository = sequenceRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<QuoteDto> Handle(CreateQuoteCommand request, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken)
            ?? throw new CustomerNotFoundException();

        var productIds = request.Lines.Select(l => l.ProductId).Distinct().ToList();
        var products = await _productRepository.GetByIdsAsync(productIds, cancellationToken);
        if (products.Count != productIds.Count)
        {
            throw new InvalidQuoteLineException("One or more products were not found.");
        }

        var quoteNumber = string.IsNullOrWhiteSpace(request.QuoteNumber)
            ? await _sequenceRepository.ConsumeAsync(DocumentSequenceType.QuoteNumber, DateTime.UtcNow, cancellationToken)
            : request.QuoteNumber;

        if (await _quoteRepository.QuoteNumberExistsAsync(quoteNumber, null, cancellationToken))
        {
            throw new DuplicateQuoteNumberException();
        }

        var quote = new Quote(
            quoteNumber,
            request.CustomerId,
            request.QuoteDate,
            request.ValidUntilUtc,
            request.Currency,
            request.Notes);

        quote.UpdateDetails(
            request.BillingAddressId,
            request.ShippingAddressId,
            request.PaymentTermsId ?? customer.PaymentTermsId,
            request.PriceListId ?? customer.PriceListId,
            request.ExchangeRate,
            request.ShippingCost,
            request.HeaderDiscountPercent,
            request.HeaderDiscountAmount,
            request.SalesRepUserId,
            request.InternalNotes,
            request.CustomerNotes,
            request.PublicNotes,
            request.TermsAndConditions,
            request.RoundingAdjustment);

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

        quote.ApplySnapshots(
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
            paymentTerm?.NetDays);

        var lines = request.Lines.Select((input, idx) =>
        {
            if (!products.TryGetValue(input.ProductId, out var product))
            {
                throw new InvalidQuoteLineException("Validation.ProductNotFoundOrCrossTenant");
            }
            var line = new QuoteLine(product.Id, product.Sku, product.Name, input.Quantity, input.UnitPrice);
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
                input.UomId,
                input.UomCode,
                input.UomConversionFactor,
                input.LineNotes,
                product.Description);
            return line;
        }).ToList();

        quote.ReplaceLines(lines);
        await _quoteRepository.AddAsync(quote, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        quote.Customer = customer;
        return QuoteMapper.ToDto(quote);
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
