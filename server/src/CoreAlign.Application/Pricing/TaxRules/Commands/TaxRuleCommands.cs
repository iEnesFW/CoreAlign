using CoreAlign.Application.Common;
using CoreAlign.Application.Pricing.Common;
using CoreAlign.Domain.Entities.Pricing;
using MediatR;

namespace CoreAlign.Application.Pricing.TaxRules.Commands;

public record CreateTaxRuleCommand(
    string Code,
    string Name,
    TaxRuleScope Scope,
    decimal RatePercent,
    string? RegionCode = null,
    string? ProductClass = null,
    Guid? ProductCategoryId = null,
    Guid? ProductId = null,
    Guid? FallbackTaxRateId = null,
    DateTime? ValidFromUtc = null,
    DateTime? ValidUntilUtc = null,
    int Priority = 0,
    string? Description = null) : IRequest<TaxRuleDto>, ITransactionalRequest;

public record UpdateTaxRuleCommand(
    Guid Id,
    string Name,
    TaxRuleScope Scope,
    decimal RatePercent,
    string? RegionCode,
    string? ProductClass,
    Guid? ProductCategoryId,
    Guid? ProductId,
    Guid? FallbackTaxRateId,
    DateTime? ValidFromUtc,
    DateTime? ValidUntilUtc,
    int Priority,
    bool IsActive,
    string? Description) : IRequest<TaxRuleDto>, ITransactionalRequest;

public record DeleteTaxRuleCommand(Guid Id) : IRequest<bool>, ITransactionalRequest;
