using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities;

public class QuoteLine : TenantEntity
{
    public Guid QuoteId { get; internal set; }
    public int LineNumber { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductSku { get; private set; } = string.Empty;
    public string ProductName { get; private set; } = string.Empty;
    public string? ProductDescriptionSnapshot { get; private set; }

    public Guid? UomId { get; private set; }
    public string? UomCode { get; private set; }
    public decimal UomConversionFactor { get; private set; } = 1m;

    public decimal Quantity { get; private set; }
    public decimal ListPriceSnapshot { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal LineDiscountPercent { get; private set; }
    public decimal LineDiscountAmount { get; private set; }
    public bool IsManualPriceOverride { get; private set; }

    public Guid? TaxRateId { get; private set; }
    public decimal TaxRatePercent { get; private set; }
    public decimal TaxAmount { get; private set; }
    public bool IsTaxInclusive { get; private set; }

    public decimal WithholdingRatePercent { get; private set; }
    public decimal WithholdingAmount { get; private set; }

    public decimal LineSubtotal { get; private set; }
    public decimal LineNetAmount { get; private set; }
    public decimal LineTotal { get; private set; }

    public string? LineNotes { get; private set; }

    public Quote Quote { get; set; } = null!;
    public Product Product { get; set; } = null!;

    protected QuoteLine() { }

    public QuoteLine(Guid productId, string productSku, string productName, decimal quantity, decimal unitPrice)
    {
        ProductId = productId;
        ProductSku = productSku;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
        ListPriceSnapshot = unitPrice;
        Recalculate();
    }

    public void SetLineNumber(int lineNumber) => LineNumber = lineNumber;

    public void ApplyPricing(
        decimal quantity,
        decimal listPriceSnapshot,
        decimal unitPrice,
        decimal lineDiscountPercent,
        decimal lineDiscountAmount,
        bool isManualPriceOverride,
        decimal taxRatePercent,
        Guid? taxRateId,
        bool isTaxInclusive,
        decimal withholdingRatePercent,
        Guid? uomId,
        string? uomCode,
        decimal uomConversionFactor,
        string? lineNotes,
        string? productDescriptionSnapshot)
    {
        Quantity = quantity;
        ListPriceSnapshot = listPriceSnapshot;
        UnitPrice = unitPrice;
        LineDiscountPercent = lineDiscountPercent;
        LineDiscountAmount = lineDiscountAmount;
        IsManualPriceOverride = isManualPriceOverride;
        TaxRatePercent = taxRatePercent;
        TaxRateId = taxRateId;
        IsTaxInclusive = isTaxInclusive;
        WithholdingRatePercent = withholdingRatePercent;
        UomId = uomId;
        UomCode = uomCode;
        UomConversionFactor = uomConversionFactor > 0 ? uomConversionFactor : 1m;
        LineNotes = lineNotes;
        ProductDescriptionSnapshot = productDescriptionSnapshot;
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

    public decimal LineTaxAmount => TaxAmount;
    public decimal LineWithholdingAmount => WithholdingAmount;
}
