using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities;

public class InvoiceLine : TenantEntity
{
    public Guid InvoiceId { get; set; }
    public int LineNumber { get; private set; }
    public Guid? ProductId { get; set; }
    public string ProductSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? Description { get; private set; }

    public Guid? UomId { get; private set; }
    public string? UomCode { get; private set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    public decimal LineDiscountPercent { get; private set; }
    public decimal LineDiscountAmount { get; private set; }

    public Guid? TaxRateId { get; private set; }
    public decimal TaxRatePercent { get; private set; }
    public decimal TaxAmount { get; private set; }
    public bool IsTaxInclusive { get; private set; }

    public decimal WithholdingRatePercent { get; private set; }
    public decimal WithholdingAmount { get; private set; }

    public decimal LineSubtotal { get; private set; }
    public decimal LineNetAmount { get; private set; }
    public decimal LineTotal { get; private set; }

    public string? RevenueAccountCode { get; private set; }
    public string? CostCenter { get; private set; }
    public string? Project { get; private set; }
    public Guid? OriginOrderLineId { get; private set; }

    public Invoice Invoice { get; set; } = null!;
    public Product? Product { get; set; }

    protected InvoiceLine() { }

    public InvoiceLine(Guid productId, string productSku, string productName, decimal quantity, decimal unitPrice)
    {
        ProductId = productId;
        ProductSku = productSku;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
        Recalculate();
    }

    public InvoiceLine(string sku, string name, string? description, decimal quantity, decimal unitPrice)
    {
        ProductSku = sku;
        ProductName = name;
        Description = description;
        Quantity = quantity;
        UnitPrice = unitPrice;
        Recalculate();
    }

    public void SetLineNumber(int n) => LineNumber = n;

    public void ApplyPricing(
        decimal quantity,
        decimal unitPrice,
        decimal lineDiscountPercent,
        decimal lineDiscountAmount,
        decimal taxRatePercent,
        Guid? taxRateId,
        bool isTaxInclusive,
        decimal withholdingRatePercent,
        Guid? uomId,
        string? uomCode,
        string? description,
        string? revenueAccountCode,
        string? costCenter,
        string? project,
        Guid? originOrderLineId)
    {
        Quantity = quantity;
        UnitPrice = unitPrice;
        LineDiscountPercent = lineDiscountPercent;
        LineDiscountAmount = lineDiscountAmount;
        TaxRatePercent = taxRatePercent;
        TaxRateId = taxRateId;
        IsTaxInclusive = isTaxInclusive;
        WithholdingRatePercent = withholdingRatePercent;
        UomId = uomId;
        UomCode = uomCode;
        Description = description;
        RevenueAccountCode = revenueAccountCode;
        CostCenter = costCenter;
        Project = project;
        OriginOrderLineId = originOrderLineId;
        Recalculate();
    }

    public void Recalculate()
    {
        var gross = Math.Round(Quantity * UnitPrice, 4);
        LineSubtotal = gross;

        var pctDiscount = LineDiscountPercent > 0 ? gross * (LineDiscountPercent / 100m) : 0m;
        var totalDiscount = pctDiscount + LineDiscountAmount;
        if (totalDiscount > gross) totalDiscount = gross;

        var net = Math.Round(gross - totalDiscount, 4);
        LineNetAmount = net;

        if (IsTaxInclusive)
        {
            var taxBase = TaxRatePercent > 0 ? net / (1 + (TaxRatePercent / 100m)) : net;
            TaxAmount = Math.Round(net - taxBase, 4);
            LineTotal = net;
        }
        else
        {
            TaxAmount = TaxRatePercent > 0 ? Math.Round(net * (TaxRatePercent / 100m), 4) : 0m;
            LineTotal = Math.Round(net + TaxAmount, 4);
        }

        WithholdingAmount = WithholdingRatePercent > 0
            ? Math.Round((net + (IsTaxInclusive ? 0m : TaxAmount)) * (WithholdingRatePercent / 100m), 4)
            : 0m;
    }
}
