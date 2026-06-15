using CoreAlign.Domain.Common;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Domain.Entities.Catalog;

public class ProductVariant : TenantEntity, IHasConcurrencyToken
{
    public Guid ParentProductId { get; private set; }
    public string Sku { get; private set; } = string.Empty;
    public string? Barcode { get; private set; }
    public string VariantAttributesJson { get; private set; } = "{}";
    public decimal? PriceOverride { get; private set; }
    public decimal StockQuantity { get; private set; }
    public bool IsActive { get; private set; } = true;

    public long ConcurrencyToken { get; private set; }
    void IHasConcurrencyToken.BumpConcurrencyToken() => ConcurrencyToken++;

    public Product? Parent { get; private set; }

    protected ProductVariant() { }

    public ProductVariant(
        Guid parentProductId,
        string sku,
        string variantAttributesJson,
        string? barcode = null,
        decimal? priceOverride = null,
        decimal stockQuantity = 0m,
        bool isActive = true)
    {
        if (parentProductId == Guid.Empty)
        {
            throw new ArgumentException("ParentProductId is required.", nameof(parentProductId));
        }
        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new ArgumentException("Sku is required.", nameof(sku));
        }
        if (priceOverride is < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(priceOverride), "Price override cannot be negative.");
        }
        if (stockQuantity < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(stockQuantity), "Stock quantity cannot be negative.");
        }

        ParentProductId = parentProductId;
        Sku = sku.Trim();
        Barcode = string.IsNullOrWhiteSpace(barcode) ? null : barcode.Trim();
        VariantAttributesJson = string.IsNullOrWhiteSpace(variantAttributesJson) ? "{}" : variantAttributesJson.Trim();
        PriceOverride = priceOverride;
        StockQuantity = stockQuantity;
        IsActive = isActive;
    }

    public void UpdateDetails(
        string sku,
        string? barcode,
        string variantAttributesJson,
        decimal? priceOverride,
        bool isActive)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new ArgumentException("Sku is required.", nameof(sku));
        }
        if (priceOverride is < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(priceOverride), "Price override cannot be negative.");
        }

        Sku = sku.Trim();
        Barcode = string.IsNullOrWhiteSpace(barcode) ? null : barcode.Trim();
        VariantAttributesJson = string.IsNullOrWhiteSpace(variantAttributesJson) ? "{}" : variantAttributesJson.Trim();
        PriceOverride = priceOverride;
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void AdjustStock(decimal delta)
    {
        var next = StockQuantity + delta;
        if (next < 0m)
        {
            throw new InsufficientStockException(Sku, StockQuantity, Math.Abs(delta));
        }
        StockQuantity = next;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetStock(decimal value)
    {
        if (value < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Stock quantity cannot be negative.");
        }
        StockQuantity = value;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
