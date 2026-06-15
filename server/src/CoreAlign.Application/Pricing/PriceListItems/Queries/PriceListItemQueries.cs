using CoreAlign.Application.Pricing.Common;
using MediatR;

namespace CoreAlign.Application.Pricing.PriceListItems.Queries;

public record ListPriceListItemsQuery(Guid PriceListId) : IRequest<IReadOnlyList<PriceListItemDto>>;

public record GetPriceListItemByIdQuery(Guid PriceListId, Guid Id) : IRequest<PriceListItemDto?>;
