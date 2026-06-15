namespace CoreAlign.Application.Pricing.Common;

public sealed class PriceListItemDto
{
    public Guid Id { get; set; }
    public Guid PriceListId { get; set; }
    public Guid ProductId { get; set; }
    public decimal Price { get; set; }
    public decimal? MinQuantity { get; set; }
    public decimal? MaxQuantity { get; set; }
    public decimal? DiscountPercent { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public sealed class DiscountRuleDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public Guid? CustomerGroupId { get; set; }
    public Guid? ProductCategoryId { get; set; }
    public Guid? ProductId { get; set; }
    public DateTime? ValidFromUtc { get; set; }
    public DateTime? ValidUntilUtc { get; set; }
    public decimal? MinQuantity { get; set; }
    public string ValueType { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
}

public sealed class TaxRuleDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public string? RegionCode { get; set; }
    public string? ProductClass { get; set; }
    public Guid? ProductCategoryId { get; set; }
    public Guid? ProductId { get; set; }
    public decimal RatePercent { get; set; }
    public Guid? FallbackTaxRateId { get; set; }
    public DateTime? ValidFromUtc { get; set; }
    public DateTime? ValidUntilUtc { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
}
