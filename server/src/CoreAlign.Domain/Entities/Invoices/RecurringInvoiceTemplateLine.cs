using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities.Invoices;

public class RecurringInvoiceTemplateLine : TenantEntity
{
    public Guid TemplateId { get; private set; }
    public RecurringInvoiceTemplate? Template { get; private set; }
    public int LineNumber { get; private set; }
    public Guid? ProductId { get; private set; }
    public string ProductSku { get; private set; } = string.Empty;
    public string ProductName { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal TaxRatePercent { get; private set; }
    public Guid? TaxRateId { get; private set; }
    public decimal? LineDiscountPercent { get; private set; }
    public decimal? LineDiscountAmount { get; private set; }
    public decimal? WithholdingRatePercent { get; private set; }
    public bool IsTaxInclusive { get; private set; }
    public Guid? UomId { get; private set; }
    public string? UomCode { get; private set; }

    protected RecurringInvoiceTemplateLine() { }

    public RecurringInvoiceTemplateLine(
        Guid? productId,
        string productSku,
        string productName,
        string? description,
        decimal quantity,
        decimal unitPrice,
        decimal taxRatePercent = 0m,
        Guid? taxRateId = null,
        decimal? lineDiscountPercent = null,
        decimal? lineDiscountAmount = null,
        decimal? withholdingRatePercent = null,
        bool isTaxInclusive = false,
        Guid? uomId = null,
        string? uomCode = null)
    {
        if (string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException("Product name is required.", nameof(productName));
        if (quantity <= 0m)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        if (unitPrice < 0m)
            throw new ArgumentOutOfRangeException(nameof(unitPrice), "Unit price cannot be negative.");

        ProductId = productId;
        ProductSku = productSku ?? string.Empty;
        ProductName = productName.Trim();
        Description = description;
        Quantity = Math.Round(quantity, 4);
        UnitPrice = Math.Round(unitPrice, 4);
        TaxRatePercent = taxRatePercent;
        TaxRateId = taxRateId;
        LineDiscountPercent = lineDiscountPercent;
        LineDiscountAmount = lineDiscountAmount;
        WithholdingRatePercent = withholdingRatePercent;
        IsTaxInclusive = isTaxInclusive;
        UomId = uomId;
        UomCode = uomCode;
    }

    internal void AttachTo(RecurringInvoiceTemplate template, int lineNumber)
    {
        Template = template;
        TemplateId = template.Id;
        LineNumber = lineNumber;
        TenantId = template.TenantId;
    }
}
