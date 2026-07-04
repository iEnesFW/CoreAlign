using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Domain.Entities;

public class VendorBill : TenantEntity, IHasConcurrencyToken
{
    public long ConcurrencyToken { get; private set; }
    void IHasConcurrencyToken.BumpConcurrencyToken() => ConcurrencyToken++;

    public Guid VendorId { get; private set; }
    public string VendorName { get; private set; } = string.Empty;
    public string BillNumber { get; private set; } = string.Empty;
    public DateTime BillDate { get; private set; }
    public DateTime? DueDate { get; private set; }
    public string Currency { get; private set; } = "TRY";
    public decimal ExchangeRate { get; private set; } = 1m;
    public decimal Subtotal { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal Total { get; private set; }
    public decimal AmountPaid { get; private set; }
    public VendorBillStatus Status { get; private set; } = VendorBillStatus.Draft;
    public Guid? PurchaseOrderId { get; private set; }
    public string? Notes { get; private set; }
    public DateTime? PostedAtUtc { get; private set; }

    public bool RequiresApproval { get; private set; }
    public DateTime? HeldAtUtc { get; private set; }
    public string? HoldReason { get; private set; }
    public Guid? ApprovedByUserId { get; private set; }
    public DateTime? ApprovedAtUtc { get; private set; }

    public Vendor Vendor { get; set; } = null!;
    public ICollection<VendorBillLine> Lines { get; private set; } = new List<VendorBillLine>();

    public decimal AmountDue => Math.Max(0m, Total - AmountPaid);

    protected VendorBill() { }

    public VendorBill(
        Guid vendorId,
        string vendorName,
        string billNumber,
        DateTime billDate,
        string currency,
        decimal subtotal,
        decimal taxAmount,
        DateTime? dueDate = null,
        decimal exchangeRate = 1m,
        Guid? purchaseOrderId = null,
        string? notes = null)
    {
        VendorId = vendorId;
        VendorName = vendorName;
        BillNumber = billNumber;
        BillDate = billDate;
        DueDate = dueDate;
        Currency = currency;
        ExchangeRate = exchangeRate > 0 ? exchangeRate : 1m;
        Subtotal = Math.Round(subtotal, 4);
        TaxAmount = Math.Round(taxAmount, 4);
        Total = Math.Round(subtotal + taxAmount, 4);
        PurchaseOrderId = purchaseOrderId;
        Notes = notes;
    }

    public void ReplaceLines(IEnumerable<VendorBillLine> newLines)
    {
        if (Status is not (VendorBillStatus.Draft or VendorBillStatus.PendingApproval))
        {
            throw new InvalidOrderStatusTransitionException(Status.ToString(), "EditLines");
        }
        Lines.Clear();
        var i = 1;
        foreach (var line in newLines)
        {
            line.SetLineNumber(i++);
            Lines.Add(line);
        }
        if (Lines.Count > 0)
        {
            Recalculate();
        }
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Recalculate()
    {
        Subtotal = Math.Round(Lines.Sum(l => l.LineSubtotal), 4);
        TaxAmount = Math.Round(Lines.Sum(l => l.TaxAmount), 4);
        Total = Math.Round(Lines.Sum(l => l.LineTotal), 4);
    }

    public void Post()
    {
        if (Status != VendorBillStatus.Draft)
        {
            throw new InvalidOrderStatusTransitionException(Status.ToString(), VendorBillStatus.Posted.ToString());
        }
        Status = VendorBillStatus.Posted;
        PostedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = PostedAtUtc.Value;
    }

    public void PlaceOnHold(string reason)
    {
        if (Status != VendorBillStatus.Draft)
        {
            throw new InvalidOrderStatusTransitionException(Status.ToString(), VendorBillStatus.PendingApproval.ToString());
        }
        Status = VendorBillStatus.PendingApproval;
        RequiresApproval = true;
        HeldAtUtc = DateTime.UtcNow;
        HoldReason = reason;
        UpdatedAtUtc = HeldAtUtc.Value;
    }

    public void ApproveAndPost(Guid approverUserId)
    {
        if (Status != VendorBillStatus.PendingApproval)
        {
            throw new InvalidOrderStatusTransitionException(Status.ToString(), VendorBillStatus.Posted.ToString());
        }
        ApprovedByUserId = approverUserId;
        ApprovedAtUtc = DateTime.UtcNow;
        Status = VendorBillStatus.Posted;
        PostedAtUtc = ApprovedAtUtc.Value;
        UpdatedAtUtc = PostedAtUtc.Value;
    }

    public void UpdateDraft(
        string billNumber,
        DateTime billDate,
        DateTime? dueDate,
        string currency,
        decimal exchangeRate,
        decimal subtotal,
        decimal taxAmount,
        Guid? purchaseOrderId,
        string? notes)
    {
        if (Status != VendorBillStatus.Draft)
        {
            throw new InvalidOrderStatusTransitionException(Status.ToString(), "Update");
        }
        BillNumber = billNumber;
        BillDate = billDate;
        DueDate = dueDate;
        Currency = currency;
        ExchangeRate = exchangeRate > 0 ? exchangeRate : 1m;
        Subtotal = Math.Round(subtotal, 4);
        TaxAmount = Math.Round(taxAmount, 4);
        Total = Math.Round(subtotal + taxAmount, 4);
        PurchaseOrderId = purchaseOrderId;
        Notes = notes;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void RecordPayment(decimal amount)
    {
        if (amount <= 0m)
        {
            throw new StockMovementValidationException("Payment amount must be positive.");
        }
        if (Status is VendorBillStatus.Draft or VendorBillStatus.Cancelled)
        {
            throw new InvalidOrderStatusTransitionException(Status.ToString(), "Payment");
        }
        if (AmountPaid + amount > Total + 0.0001m)
        {
            throw new StockMovementValidationException("Payment exceeds the amount due on this bill.");
        }
        AmountPaid = Math.Round(AmountPaid + amount, 4);
        Status = AmountPaid >= Total ? VendorBillStatus.Paid : VendorBillStatus.PartiallyPaid;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status is VendorBillStatus.Paid or VendorBillStatus.Cancelled)
        {
            throw new InvalidOrderStatusTransitionException(Status.ToString(), VendorBillStatus.Cancelled.ToString());
        }
        Status = VendorBillStatus.Cancelled;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void ReverseRecordedPayment(decimal amount)
    {
        if (amount <= 0m) return;
        if (Status == VendorBillStatus.Cancelled) return;
        var next = Math.Max(0m, Math.Round(AmountPaid - amount, 4));
        AmountPaid = next;
        Status = AmountPaid <= 0m ? VendorBillStatus.Posted : VendorBillStatus.PartiallyPaid;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
