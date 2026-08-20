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
    public decimal QuantityAwaitingInspection { get; private set; }
    public decimal QuantityBilled { get; private set; }

    public decimal UnitCost { get; private set; }
    public decimal TaxRatePercent { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal LineSubtotal { get; private set; }
    public decimal LineTotal { get; private set; }
    public string? LineNotes { get; private set; }

    public PurchaseOrder PurchaseOrder { get; set; } = null!;
    public Product Product { get; set; } = null!;

    // WHY the awaiting bucket is separate: QuantityReceived is the quantity whose stock and
    // GR/IR credit have actually been recognised, so the write-off residual and the three-way
    // match ceiling can trust it. Goods held for inspection occupy the line (they cannot be
    // received twice) without claiming a credit that was never booked.
    public decimal QuantityRemainingToReceive =>
        Math.Max(0m, Quantity - QuantityReceived - QuantityAwaitingInspection);

    public decimal QuantityClaimed => QuantityReceived + QuantityAwaitingInspection;

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
        EnsureReceiptFits(qty);
        QuantityReceived += qty;
    }

    public void RecordReceiptAwaitingInspection(decimal qty)
    {
        EnsureReceiptFits(qty);
        QuantityAwaitingInspection += qty;
    }

    public void ApproveInspection(decimal qty)
    {
        if (qty <= 0m) return;
        var moved = Math.Min(qty, QuantityAwaitingInspection);
        QuantityAwaitingInspection -= moved;
        QuantityReceived += moved;
    }

    public void RejectInspection(decimal qty)
    {
        if (qty <= 0m) return;
        QuantityAwaitingInspection = Math.Max(0m, QuantityAwaitingInspection - qty);
    }

    public void ReverseReceipt(decimal qty)
    {
        if (qty <= 0m) return;
        if (QuantityReceived - qty < QuantityBilled)
        {
            throw new ReceiptReversalBelowBilledException(ProductSku, QuantityBilled, QuantityReceived - qty);
        }
        QuantityReceived = Math.Max(0m, QuantityReceived - qty);
    }

    private void EnsureReceiptFits(decimal qty)
    {
        if (qty <= 0m)
        {
            throw new InvalidOrderLineException("Receipt quantity must be positive.");
        }
        if (QuantityClaimed + qty > Quantity)
        {
            throw new InvalidOrderLineException("Receipt exceeds the line's remaining quantity.");
        }
    }

    public void RecordBill(decimal qty)
    {
        if (qty <= 0m) return;
        QuantityBilled += qty;
    }

    public void ReverseBill(decimal qty)
    {
        if (qty <= 0m) return;
        QuantityBilled = Math.Max(0m, QuantityBilled - qty);
    }
}
