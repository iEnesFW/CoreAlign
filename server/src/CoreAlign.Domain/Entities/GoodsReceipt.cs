using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Domain.Entities;

public class GoodsReceipt : TenantEntity
{
    public string GrnNumber { get; private set; } = string.Empty;
    public Guid VendorId { get; private set; }
    public string VendorName { get; private set; } = string.Empty;
    public Guid PurchaseOrderId { get; private set; }
    public string PoNumber { get; private set; } = string.Empty;
    public DateTime ReceiptDateUtc { get; private set; }
    public Guid WarehouseId { get; private set; }
    public GoodsReceiptStatus Status { get; private set; } = GoodsReceiptStatus.Posted;
    public Guid? ReceivedByUserId { get; private set; }
    public string? Notes { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;
    public string Currency { get; private set; } = "TRY";
    public decimal ExchangeRate { get; private set; } = 1m;

    public DateTime? ReversedAtUtc { get; private set; }
    public Guid? ReversedByUserId { get; private set; }
    public string? ReversalReason { get; private set; }

    public PurchaseOrder PurchaseOrder { get; set; } = null!;
    public Vendor Vendor { get; set; } = null!;
    public Warehouse Warehouse { get; set; } = null!;
    public ICollection<GoodsReceiptLine> Lines { get; private set; } = new List<GoodsReceiptLine>();

    public decimal TotalCost => Math.Round(Lines.Sum(l => l.LineCost), 4);

    protected GoodsReceipt() { }

    public GoodsReceipt(
        string grnNumber,
        PurchaseOrder po,
        Guid warehouseId,
        DateTime receiptDateUtc,
        string idempotencyKey,
        Guid? receivedByUserId = null,
        string? notes = null)
    {
        GrnNumber = grnNumber;
        VendorId = po.VendorId;
        VendorName = po.VendorName;
        PurchaseOrderId = po.Id;
        PoNumber = po.PoNumber;
        WarehouseId = warehouseId;
        ReceiptDateUtc = receiptDateUtc;
        IdempotencyKey = idempotencyKey;
        Currency = po.Currency;
        ExchangeRate = po.ExchangeRate;
        ReceivedByUserId = receivedByUserId;
        Notes = notes;
        Status = GoodsReceiptStatus.Posted;
    }

    public void AddLine(GoodsReceiptLine line)
    {
        line.SetLineNumber(Lines.Count + 1);
        Lines.Add(line);
    }

    public void MarkReversed(string? reason, Guid userId, DateTime nowUtc)
    {
        if (Status != GoodsReceiptStatus.Posted)
        {
            throw new InvalidOrderStatusTransitionException(Status.ToString(), GoodsReceiptStatus.Reversed.ToString());
        }
        Status = GoodsReceiptStatus.Reversed;
        ReversalReason = reason;
        ReversedByUserId = userId;
        ReversedAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
    }
}
