using CoreAlign.Application.Common;
using CoreAlign.Application.Pricing.Common;
using CoreAlign.Domain.Entities.Pricing;
using MediatR;

namespace CoreAlign.Application.Pricing.Discounts.Commands;

public record CreateDiscountRuleCommand(
    string Code,
    string Name,
    DiscountRuleScope Scope,
    DiscountValueType ValueType,
    decimal Value,
    Guid? CustomerGroupId = null,
    Guid? ProductCategoryId = null,
    Guid? ProductId = null,
    DateTime? ValidFromUtc = null,
    DateTime? ValidUntilUtc = null,
    decimal? MinQuantity = null,
    int Priority = 0,
    string? Description = null) : IRequest<DiscountRuleDto>, ITransactionalRequest;

public record UpdateDiscountRuleCommand(
    Guid Id,
    string Name,
    DiscountRuleScope Scope,
    DiscountValueType ValueType,
    decimal Value,
    Guid? CustomerGroupId,
    Guid? ProductCategoryId,
    Guid? ProductId,
    DateTime? ValidFromUtc,
    DateTime? ValidUntilUtc,
    decimal? MinQuantity,
    int Priority,
    bool IsActive,
    string? Description) : IRequest<DiscountRuleDto>, ITransactionalRequest;

public record DeleteDiscountRuleCommand(Guid Id) : IRequest<bool>, ITransactionalRequest;
