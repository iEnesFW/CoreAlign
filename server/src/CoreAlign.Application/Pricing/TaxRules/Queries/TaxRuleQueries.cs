using CoreAlign.Application.Pricing.Common;
using MediatR;

namespace CoreAlign.Application.Pricing.TaxRules.Queries;

public record ListTaxRulesQuery(bool? IsActive = null) : IRequest<IReadOnlyList<TaxRuleDto>>;

public record GetTaxRuleByIdQuery(Guid Id) : IRequest<TaxRuleDto?>;
