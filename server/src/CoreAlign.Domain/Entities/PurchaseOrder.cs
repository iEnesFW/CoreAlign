using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Domain.Entities;

public class PurchaseOrder : TenantEntity
{
    public string PoNumber { get; private set; } = string.Empty;
    public Guid VendorId { get; private set; }
    public string VendorName { get; private set; } = string.Empty;
    public DateTime OrderDate { get; private set; }
    public DateTime? ExpectedDate { get; private set; }
    public string Currency { get; private set; } = "TRY";
    public decimal ExchangeRate { get; private set; } = 1m;
    public Guid? WarehouseId { get; private set; }
    public PurchaseOrderStatus Status { get; private set; } = PurchaseOrderStatus.Draft;

    public decimal Subtotal { get; private set; }
    public decimal TaxTotal { get; private set; }
    public decimal Total { get; private set; }

    public string? Notes { get; private set; }
    public Guid? ApprovedByUserId { get; private set; }
    public DateTime? SubmittedAtUtc { get; private set; }
    public DateTime? ApprovedAtUtc { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }
    public string? CancelReason { get; private set; }

    public Vendor Vendor { get; set; } = null!;
    public ICollection<PurchaseOrderLine> Lines { get; private set; } = new List<PurchaseOrderLine>();

    public bool IsEditable => Status == PurchaseOrderStatus.Draft;
    public bool IsCancellable => Status is PurchaseOrderStatus.Draft or PurchaseOrderStatus.Submitted
        or PurchaseOrderStatus.Approved or PurchaseOrderStatus.PartiallyReceived;
    public bool IsReceivable => Status is PurchaseOrderStatus.Approved or PurchaseOrderStatus.PartiallyReceived;

    protected PurchaseOrder() { }

    public PurchaseOrder(string poNumber, Guid vendorId, string vendorName, DateTime orderDate, string currency)
    {
        PoNumber = poNumber;
        VendorId = vendorId;
        VendorName = vendorName;
        OrderDate = orderDate;
        Currency = currency;
    }

    public void UpdateHeader(
        Guid vendorId,
        string vendorName,
        DateTime orderDate,
        DateTime? expectedDate,
        string currency,
        decimal exchangeRate,
        Guid? warehouseId,
        string? notes)
    {
        EnsureDraft();
        VendorId = vendorId;
        VendorName = vendorName;
        OrderDate = orderDate;
        ExpectedDate = expectedDate;
        Currency = currency;
        ExchangeRate = exchangeRate > 0 ? exchangeRate : 1m;
        WarehouseId = warehouseId;
        Notes = notes;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void ReplaceLines(IEnumerable<PurchaseOrderLine> newLines)
    {
        EnsureDraft();
        Lines.Clear();
        var i = 1;
        foreach (var line in newLines)
        {
            line.SetLineNumber(i++);
            Lines.Add(line);
        }
        Recalculate();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Recalculate()
    {
        Subtotal = Math.Round(Lines.Sum(l => l.LineSubtotal), 4);
        TaxTotal = Math.Round(Lines.Sum(l => l.TaxAmount), 4);
        Total = Math.Round(Lines.Sum(l => l.LineTotal), 4);
    }

    public void Submit()
    {
        if (Status != PurchaseOrderStatus.Draft)
        {
            throw new InvalidOrderStatusTransitionException(Status.ToString(), PurchaseOrderStatus.Submitted.ToString());
        }
        if (Lines.Count == 0)
        {
            throw new InvalidOrderLineException("Cannot submit a purchase order with no lines.");
        }
        Status = PurchaseOrderStatus.Submitted;
        SubmittedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = SubmittedAtUtc.Value;
    }

    public void Approve(Guid approvedByUserId)
    {
        if (Status != PurchaseOrderStatus.Submitted)
        {
            throw new InvalidOrderStatusTransitionException(Status.ToString(), PurchaseOrderStatus.Approved.ToString());
        }
        Status = PurchaseOrderStatus.Approved;
        ApprovedByUserId = approvedByUserId;
        ApprovedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = ApprovedAtUtc.Value;
    }

    public void Cancel(string? reason)
    {
        if (!IsCancellable)
        {
            throw new InvalidOrderStatusTransitionException(Status.ToString(), PurchaseOrderStatus.Cancelled.ToString());
        }
        Status = PurchaseOrderStatus.Cancelled;
        CancelReason = reason;
        CancelledAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = CancelledAtUtc.Value;
    }

    public void Close()
    {
        if (Status is PurchaseOrderStatus.Cancelled or PurchaseOrderStatus.Closed)
        {
            throw new InvalidOrderStatusTransitionException(Status.ToString(), PurchaseOrderStatus.Closed.ToString());
        }
        Status = PurchaseOrderStatus.Closed;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    // Records a goods receipt against a line and advances the PO receive status.
    public PurchaseOrderLine RecordLineReceipt(Guid lineId, decimal quantity)
    {
        if (!IsReceivable)
        {
            throw new InvalidOrderStatusTransitionException(Status.ToString(), PurchaseOrderStatus.Received.ToString());
        }
        var line = Lines.FirstOrDefault(l => l.Id == lineId)
            ?? throw new InvalidOrderLineException("Purchase order line not found.");
        line.RecordReceipt(quantity);
        Status = Lines.All(l => l.QuantityReceived >= l.Quantity)
            ? PurchaseOrderStatus.Received
            : PurchaseOrderStatus.PartiallyReceived;
        UpdatedAtUtc = DateTime.UtcNow;
        return line;
    }

    private void EnsureDraft()
    {
        if (Status != PurchaseOrderStatus.Draft)
        {
            throw new OrderImmutableException(Status.ToString());
        }
    }
}
