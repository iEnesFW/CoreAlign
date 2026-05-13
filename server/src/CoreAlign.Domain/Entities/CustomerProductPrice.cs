using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities;

public class CustomerProductPrice : TenantEntity
{
    public Guid CustomerId { get; private set; }
    public Guid ProductId { get; private set; }
    public string Currency { get; private set; } = "TRY";
    public decimal Price { get; private set; }
    public decimal? DiscountPercent { get; private set; }
    public decimal? MinQuantity { get; private set; }
    public decimal? MaxQuantity { get; private set; }
    public DateTime? ValidFromUtc { get; private set; }
    public DateTime? ValidUntilUtc { get; private set; }
    public string? Notes { get; private set; }
    public bool IsActive { get; private set; } = true;

    public Customer Customer { get; set; } = null!;
    public Product Product { get; set; } = null!;

    protected CustomerProductPrice() { }

    public CustomerProductPrice(
        Guid customerId,
        Guid productId,
        decimal price,
        string currency = "TRY",
        decimal? discountPercent = null,
        decimal? minQuantity = null,
        decimal? maxQuantity = null,
        DateTime? validFromUtc = null,
        DateTime? validUntilUtc = null,
        string? notes = null)
    {
        if (price < 0m) throw new ArgumentException("Price must be non-negative.", nameof(price));
        CustomerId = customerId;
        ProductId = productId;
        Price = price;
        Currency = currency;
        DiscountPercent = discountPercent;
        MinQuantity = minQuantity;
        MaxQuantity = maxQuantity;
        ValidFromUtc = validFromUtc;
        ValidUntilUtc = validUntilUtc;
        Notes = notes;
    }

    public bool IsValid(DateTime nowUtc, decimal quantity)
    {
        if (!IsActive) return false;
        if (ValidFromUtc is { } from && nowUtc < from) return false;
        if (ValidUntilUtc is { } until && nowUtc > until) return false;
        if (MinQuantity is { } min && quantity < min) return false;
        if (MaxQuantity is { } max && quantity > max) return false;
        return true;
    }

    public void Update(
        decimal price,
        string currency,
        decimal? discountPercent,
        decimal? minQuantity,
        decimal? maxQuantity,
        DateTime? validFromUtc,
        DateTime? validUntilUtc,
        string? notes,
        bool isActive)
    {
        if (price < 0m) throw new ArgumentException("Price must be non-negative.", nameof(price));
        Price = price;
        Currency = currency;
        DiscountPercent = discountPercent;
        MinQuantity = minQuantity;
        MaxQuantity = maxQuantity;
        ValidFromUtc = validFromUtc;
        ValidUntilUtc = validUntilUtc;
        Notes = notes;
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
