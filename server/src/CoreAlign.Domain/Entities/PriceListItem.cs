using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities;

public class PriceListItem : TenantEntity, IHasConcurrencyToken
{
    public long ConcurrencyToken { get; private set; }
    void IHasConcurrencyToken.BumpConcurrencyToken() => ConcurrencyToken++;

    public Guid PriceListId { get; private set; }
    public Guid ProductId { get; private set; }
    public decimal Price { get; private set; }
    public decimal? MinQuantity { get; private set; }
    public decimal? MaxQuantity { get; private set; }
    public decimal? DiscountPercent { get; private set; }

    public PriceList PriceList { get; set; } = null!;
    public Product Product { get; set; } = null!;

    protected PriceListItem() { }

    public PriceListItem(Guid priceListId, Guid productId, decimal price, decimal? minQuantity = null, decimal? maxQuantity = null, decimal? discountPercent = null)
    {
        if (price < 0m)
        {
            throw new ArgumentException("Price must be non-negative.", nameof(price));
        }
        PriceListId = priceListId;
        ProductId = productId;
        Price = price;
        MinQuantity = minQuantity;
        MaxQuantity = maxQuantity;
        DiscountPercent = discountPercent;
    }

    public bool MatchesQuantity(decimal quantity)
    {
        if (MinQuantity is { } min && quantity < min) return false;
        if (MaxQuantity is { } max && quantity > max) return false;
        return true;
    }

    public void Update(decimal price, decimal? minQuantity, decimal? maxQuantity, decimal? discountPercent)
    {
        if (price < 0m)
        {
            throw new ArgumentException("Price must be non-negative.", nameof(price));
        }
        Price = price;
        MinQuantity = minQuantity;
        MaxQuantity = maxQuantity;
        DiscountPercent = discountPercent;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
