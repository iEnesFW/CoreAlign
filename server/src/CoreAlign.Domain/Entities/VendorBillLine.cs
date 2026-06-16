using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities;

public class VendorBillLine : TenantEntity
{
    public Guid VendorBillId { get; internal set; }
    public int LineNumber { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductSku { get; private set; } = string.Empty;
    public string ProductName { get; private set; } = string.Empty;

    public Guid? PurchaseOrderLineId { get; private set; }

    public Guid? UomId { get; private set; }
    public string? UomCode { get; private set; }

    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal PoUnitCost { get; private set; }

    public decimal TaxRatePercent { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal LineSubtotal { get; private set; }
    public decimal LineTotal { get; private set; }
    public string? LineNotes { get; private set; }

    public VendorBill VendorBill { get; set; } = null!;
    public Product Product { get; set; } = null!;

    public decimal PriceVariance => Math.Round(Quantity * (UnitPrice - PoUnitCost), 4);
    public decimal ReceiptClearingCost => Math.Round(Quantity * PoUnitCost, 4);

    protected VendorBillLine() { }

    public VendorBillLine(
        Guid productId,
        string productSku,
        string productName,
        decimal quantity,
        decimal unitPrice,
        decimal poUnitCost = 0m,
        Guid? purchaseOrderLineId = null,
        decimal taxRatePercent = 0m,
        Guid? uomId = null,
        string? uomCode = null,
        string? lineNotes = null)
    {
        ProductId = productId;
        ProductSku = productSku;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
        PoUnitCost = purchaseOrderLineId is null ? unitPrice : poUnitCost;
        PurchaseOrderLineId = purchaseOrderLineId;
        TaxRatePercent = taxRatePercent;
        UomId = uomId;
        UomCode = uomCode;
        LineNotes = lineNotes;
        Recalculate();
    }

    public void SetLineNumber(int lineNumber) => LineNumber = lineNumber;

    public void Recalculate()
    {
        LineSubtotal = Math.Round(Quantity * UnitPrice, 4);
        TaxAmount = TaxRatePercent > 0 ? Math.Round(LineSubtotal * (TaxRatePercent / 100m), 4) : 0m;
        LineTotal = Math.Round(LineSubtotal + TaxAmount, 4);
    }
}
