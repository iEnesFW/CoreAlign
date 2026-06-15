using CoreAlign.Domain.Common;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Domain.Entities;

public class PurchaseOrderLine : TenantEntity
{
    public Guid PurchaseOrderId { get; internal set; }
    public int LineNumber { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductSku { get; private set; } = string.Empty;
    public string ProductName { get; private set; } = string.Empty;

    public Guid? UomId { get; private set; }
    public string? UomCode { get; private set; }

    public decimal Quantity { get; private set; }
    public decimal QuantityReceived { get; private set; }
    public decimal QuantityBilled { get; private set; }

    public decimal UnitCost { get; private set; }
    public decimal TaxRatePercent { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal LineSubtotal { get; private set; }
    public decimal LineTotal { get; private set; }
    public string? LineNotes { get; private set; }

    public PurchaseOrder PurchaseOrder { get; set; } = null!;
    public Product Product { get; set; } = null!;

    public decimal QuantityRemainingToReceive => Math.Max(0m, Quantity - QuantityReceived);

    protected PurchaseOrderLine() { }

    public PurchaseOrderLine(
        Guid productId,
        string productSku,
        string productName,
        decimal quantity,
        decimal unitCost,
        decimal taxRatePercent = 0m,
        Guid? uomId = null,
        string? uomCode = null,
        string? lineNotes = null)
    {
        ProductId = productId;
        ProductSku = productSku;
        ProductName = productName;
        Quantity = quantity;
        UnitCost = unitCost;
        TaxRatePercent = taxRatePercent;
        UomId = uomId;
        UomCode = uomCode;
        LineNotes = lineNotes;
        Recalculate();
    }

    public void SetLineNumber(int lineNumber) => LineNumber = lineNumber;

    public void Recalculate()
    {
        LineSubtotal = Math.Round(Quantity * UnitCost, 4);
        TaxAmount = TaxRatePercent > 0 ? Math.Round(LineSubtotal * (TaxRatePercent / 100m), 4) : 0m;
        LineTotal = Math.Round(LineSubtotal + TaxAmount, 4);
    }

    public void RecordReceipt(decimal qty)
    {
        if (qty <= 0m)
        {
            throw new InvalidOrderLineException("Receipt quantity must be positive.");
        }
        if (QuantityReceived + qty > Quantity)
        {
            throw new InvalidOrderLineException("Receipt exceeds the line's remaining quantity.");
        }
        QuantityReceived += qty;
    }

    public void RecordBill(decimal qty)
    {
        if (qty <= 0m) return;
        QuantityBilled += qty;
    }
}
