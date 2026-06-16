using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities;

public class GoodsReceiptLine : TenantEntity
{
    public Guid GoodsReceiptId { get; internal set; }
    public int LineNumber { get; private set; }
    public Guid PurchaseOrderLineId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductSku { get; private set; } = string.Empty;
    public string ProductName { get; private set; } = string.Empty;
    public decimal QuantityReceived { get; private set; }
    public decimal UnitCost { get; private set; }
    public Guid? StockMovementId { get; private set; }

    public GoodsReceipt GoodsReceipt { get; set; } = null!;
    public PurchaseOrderLine PurchaseOrderLine { get; set; } = null!;
    public Product Product { get; set; } = null!;

    public decimal LineCost => Math.Round(QuantityReceived * UnitCost, 4);

    protected GoodsReceiptLine() { }

    public GoodsReceiptLine(
        Guid purchaseOrderLineId,
        Guid productId,
        string productSku,
        string productName,
        decimal quantityReceived,
        decimal unitCost)
    {
        PurchaseOrderLineId = purchaseOrderLineId;
        ProductId = productId;
        ProductSku = productSku;
        ProductName = productName;
        QuantityReceived = quantityReceived;
        UnitCost = unitCost;
    }

    public void SetLineNumber(int lineNumber) => LineNumber = lineNumber;

    public void SetMovementId(Guid movementId) => StockMovementId = movementId;
}
