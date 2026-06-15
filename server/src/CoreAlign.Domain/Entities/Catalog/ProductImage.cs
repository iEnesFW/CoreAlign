using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities.Catalog;

public class ProductImage : TenantEntity
{
    public Guid ProductId { get; private set; }
    public string StorageKey { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public string? AltText { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsPrimary { get; private set; }
    public DateTime UploadedAtUtc { get; private set; } = DateTime.UtcNow;

    public Product? Product { get; private set; }

    protected ProductImage() { }

    public ProductImage(
        Guid productId,
        string storageKey,
        string contentType,
        long sizeBytes,
        string? altText,
        int displayOrder,
        bool isPrimary)
    {
        if (productId == Guid.Empty) throw new ArgumentException("ProductId is required.", nameof(productId));
        if (string.IsNullOrWhiteSpace(storageKey)) throw new ArgumentException("StorageKey is required.", nameof(storageKey));
        if (sizeBytes < 0) throw new ArgumentOutOfRangeException(nameof(sizeBytes));

        ProductId = productId;
        StorageKey = storageKey.Trim();
        ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType.Trim();
        SizeBytes = sizeBytes;
        AltText = string.IsNullOrWhiteSpace(altText) ? null : altText.Trim();
        DisplayOrder = displayOrder < 0 ? 0 : displayOrder;
        IsPrimary = isPrimary;
        UploadedAtUtc = DateTime.UtcNow;
    }

    public void UpdateMetadata(string? altText, int displayOrder, bool isPrimary)
    {
        AltText = string.IsNullOrWhiteSpace(altText) ? null : altText.Trim();
        DisplayOrder = displayOrder < 0 ? 0 : displayOrder;
        IsPrimary = isPrimary;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkPrimary(bool value)
    {
        IsPrimary = value;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Reorder(int displayOrder)
    {
        DisplayOrder = displayOrder < 0 ? 0 : displayOrder;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
