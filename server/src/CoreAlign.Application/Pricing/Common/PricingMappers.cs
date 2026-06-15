using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.Pricing;

namespace CoreAlign.Application.Pricing.Common;

public static class PricingMappers
{
    public static PriceListItemDto ToDto(PriceListItem item) => new()
    {
        Id = item.Id,
        PriceListId = item.PriceListId,
        ProductId = item.ProductId,
        Price = item.Price,
        MinQuantity = item.MinQuantity,
        MaxQuantity = item.MaxQuantity,
        DiscountPercent = item.DiscountPercent,
        CreatedAtUtc = item.CreatedAtUtc,
        UpdatedAtUtc = item.UpdatedAtUtc,
    };

    public static DiscountRuleDto ToDto(DiscountRule rule) => new()
    {
        Id = rule.Id,
        Code = rule.Code,
        Name = rule.Name,
        Scope = rule.Scope.ToString(),
        CustomerGroupId = rule.CustomerGroupId,
        ProductCategoryId = rule.ProductCategoryId,
        ProductId = rule.ProductId,
        ValidFromUtc = rule.ValidFromUtc,
        ValidUntilUtc = rule.ValidUntilUtc,
        MinQuantity = rule.MinQuantity,
        ValueType = rule.ValueType.ToString(),
        Value = rule.Value,
        Priority = rule.Priority,
        IsActive = rule.IsActive,
        Description = rule.Description,
    };

    public static TaxRuleDto ToDto(TaxRule rule) => new()
    {
        Id = rule.Id,
        Code = rule.Code,
        Name = rule.Name,
        Scope = rule.Scope.ToString(),
        RegionCode = rule.RegionCode,
        ProductClass = rule.ProductClass,
        ProductCategoryId = rule.ProductCategoryId,
        ProductId = rule.ProductId,
        RatePercent = rule.RatePercent,
        FallbackTaxRateId = rule.FallbackTaxRateId,
        ValidFromUtc = rule.ValidFromUtc,
        ValidUntilUtc = rule.ValidUntilUtc,
        Priority = rule.Priority,
        IsActive = rule.IsActive,
        Description = rule.Description,
    };
}
