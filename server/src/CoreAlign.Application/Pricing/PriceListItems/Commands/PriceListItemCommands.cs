using CoreAlign.Application.Common;
using CoreAlign.Application.Pricing.Common;
using MediatR;

namespace CoreAlign.Application.Pricing.PriceListItems.Commands;

public record AddPriceListItemCommand(
    Guid PriceListId,
    Guid ProductId,
    decimal Price,
    decimal? MinQuantity = null,
    decimal? MaxQuantity = null,
    decimal? DiscountPercent = null) : IRequest<PriceListItemDto>, ITransactionalRequest;

public record UpdatePriceListItemCommand(
    Guid PriceListId,
    Guid Id,
    decimal Price,
    decimal? MinQuantity,
    decimal? MaxQuantity,
    decimal? DiscountPercent) : IRequest<PriceListItemDto>, ITransactionalRequest;

public record RemovePriceListItemCommand(Guid PriceListId, Guid Id)
    : IRequest<bool>, ITransactionalRequest;
