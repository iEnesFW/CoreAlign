namespace CoreAlign.Application.Products.Variants;

public sealed record ProductVariantDto(
    Guid Id,
    Guid ParentProductId,
    string Sku,
    string? Barcode,
    string VariantAttributesJson,
    decimal? PriceOverride,
    decimal StockQuantity,
    bool IsActive,
    long ConcurrencyToken,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
