using CoreAlign.Application.Pricing.Common;
using MediatR;

namespace CoreAlign.Application.Pricing.Discounts.Queries;

public record ListDiscountRulesQuery(bool? IsActive = null) : IRequest<IReadOnlyList<DiscountRuleDto>>;

public record GetDiscountRuleByIdQuery(Guid Id) : IRequest<DiscountRuleDto?>;
