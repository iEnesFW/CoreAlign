using CoreAlign.Application.Common;
using CoreAlign.Application.Orders.DTOs;
using CoreAlign.Application.Quotes.DTOs;
using MediatR;

namespace CoreAlign.Application.Quotes.Commands;

public record QuoteLineInput(
    Guid ProductId,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineDiscountPercent = 0m,
    decimal LineDiscountAmount = 0m,
    decimal TaxRatePercent = 0m,
    bool IsTaxInclusive = false,
    decimal WithholdingRatePercent = 0m,
    Guid? TaxRateId = null,
    Guid? UomId = null,
    string? UomCode = null,
    decimal UomConversionFactor = 1m,
    string? LineNotes = null,
    bool IsManualPriceOverride = false);

public record CreateQuoteCommand(
    string? QuoteNumber,
    Guid CustomerId,
    DateTime QuoteDate,
    DateTime ValidUntilUtc,
    string Currency,
    string? Notes,
    List<QuoteLineInput> Lines,
    Guid? BillingAddressId = null,
    Guid? ShippingAddressId = null,
    Guid? PaymentTermsId = null,
    Guid? PriceListId = null,
    decimal ExchangeRate = 1m,
    decimal ShippingCost = 0m,
    decimal HeaderDiscountPercent = 0m,
    decimal HeaderDiscountAmount = 0m,
    Guid? SalesRepUserId = null,
    string? InternalNotes = null,
    string? CustomerNotes = null,
    string? PublicNotes = null,
    string? TermsAndConditions = null,
    decimal RoundingAdjustment = 0m
) : IRequest<QuoteDto>, ITransactionalRequest;

public record UpdateQuoteCommand(
    Guid Id,
    string QuoteNumber,
    Guid CustomerId,
    DateTime QuoteDate,
    DateTime ValidUntilUtc,
    string Currency,
    string? Notes,
    List<QuoteLineInput> Lines,
    Guid? BillingAddressId = null,
    Guid? ShippingAddressId = null,
    Guid? PaymentTermsId = null,
    Guid? PriceListId = null,
    decimal ExchangeRate = 1m,
    decimal ShippingCost = 0m,
    decimal HeaderDiscountPercent = 0m,
    decimal HeaderDiscountAmount = 0m,
    Guid? SalesRepUserId = null,
    string? InternalNotes = null,
    string? CustomerNotes = null,
    string? PublicNotes = null,
    string? TermsAndConditions = null,
    decimal RoundingAdjustment = 0m
) : IRequest<QuoteDto>, ITransactionalRequest;

public record DeleteQuoteCommand(Guid Id) : IRequest<bool>, ITransactionalRequest;

public record SendQuoteCommand(Guid Id) : IRequest<QuoteDto>, ITransactionalRequest;

public record AcceptQuoteCommand(Guid Id) : IRequest<QuoteDto>, ITransactionalRequest;

public record RejectQuoteCommand(Guid Id, string? Reason = null) : IRequest<QuoteDto>, ITransactionalRequest;

public record ConvertQuoteToOrderCommand(Guid Id) : IRequest<OrderDto>, ITransactionalRequest;
