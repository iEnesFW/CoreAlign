using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities.Sales;

public class OrderTemplateLine : TenantEntity
{
    public Guid OrderTemplateId { get; private set; }
    public OrderTemplate? OrderTemplate { get; private set; }
    public int LineNumber { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductSku { get; private set; } = string.Empty;
    public string ProductName { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public string? Notes { get; private set; }

    protected OrderTemplateLine() { }

    public OrderTemplateLine(
        Guid productId,
        string productSku,
        string productName,
        decimal quantity,
        decimal unitPrice,
        string? notes = null)
    {
        if (productId == Guid.Empty) throw new ArgumentException("Product id is required.", nameof(productId));
        if (string.IsNullOrWhiteSpace(productSku)) throw new ArgumentException("Product sku is required.", nameof(productSku));
        if (string.IsNullOrWhiteSpace(productName)) throw new ArgumentException("Product name is required.", nameof(productName));
        if (quantity <= 0m) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        if (unitPrice < 0m) throw new ArgumentOutOfRangeException(nameof(unitPrice), "Unit price cannot be negative.");

        ProductId = productId;
        ProductSku = productSku;
        ProductName = productName;
        Quantity = Math.Round(quantity, 4);
        UnitPrice = Math.Round(unitPrice, 4);
        Notes = notes;
    }

    internal void AttachTo(OrderTemplate template, int lineNumber)
    {
        OrderTemplate = template;
        OrderTemplateId = template.Id;
        LineNumber = lineNumber;
        TenantId = template.TenantId;
    }
}
