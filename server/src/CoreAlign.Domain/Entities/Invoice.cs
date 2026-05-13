using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Events;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Domain.Entities;

public class Invoice : TenantEntity
{
    public string InvoiceNumber { get; private set; } = string.Empty;
    public InvoiceType Type { get; private set; } = InvoiceType.SalesInvoice;
    public InvoiceStatus Status { get; private set; } = InvoiceStatus.Draft;

    public Guid? OrderId { get; private set; }
    public Guid? OriginInvoiceId { get; private set; }
    public Guid? CreditNoteId { get; private set; }

    public Guid CustomerId { get; private set; }
    public string CustomerNameSnapshot { get; private set; } = string.Empty;
    public CustomerSnapshot? CustomerSnapshot { get; private set; }
    public AddressSnapshot? BillingAddressSnapshot { get; private set; }
    public AddressSnapshot? ShippingAddressSnapshot { get; private set; }

    public DateTime IssueDate { get; private set; } = DateTime.UtcNow;
    public DateTime DueDate { get; private set; } = DateTime.UtcNow.AddDays(30);
    public DateTime PostingDate { get; private set; } = DateTime.UtcNow.Date;
    public DateTime? IssuedAtUtc { get; private set; }
    public DateTime? PaidAtUtc { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }
    public DateTime? VoidedAtUtc { get; private set; }
    public DateTime? SentAtUtc { get; private set; }

    public string Currency { get; private set; } = "TRY";
    public decimal ExchangeRate { get; private set; } = 1m;

    public Guid? PaymentTermsId { get; private set; }
    public int? PaymentTermsNetDaysSnapshot { get; private set; }

    public decimal Subtotal { get; private set; }
    public decimal LineDiscountTotal { get; private set; }
    public decimal HeaderDiscountAmount { get; private set; }
    public decimal HeaderDiscountPercent { get; private set; }
    public decimal TaxableTotal { get; private set; }
    public decimal TaxTotal { get; private set; }
    public decimal WithholdingTotal { get; private set; }
    public decimal ShippingCost { get; private set; }
    public decimal RoundingAdjustment { get; private set; }
    public decimal Total { get; private set; }

    public string? TaxBreakdownJson { get; private set; }

    public decimal AmountPaid { get; private set; }
    public decimal AmountDue => Math.Max(0m, Total - AmountPaid);

    public Guid? ApprovedByUserId { get; private set; }
    public string? CancelReason { get; private set; }
    public string? VoidReason { get; private set; }

    public string? InternalNotes { get; private set; }
    public string? PublicNotes { get; private set; }
    public string? TermsAndConditions { get; private set; }
    public string? Notes { get; private set; }

    public string? EInvoiceUuid { get; private set; }
    public string? EInvoiceStatus { get; private set; }
    public string? EInvoicePdfPath { get; private set; }

    public bool IsPostedToLedger { get; private set; }
    public bool IsPeriodLocked { get; private set; }

    public Order? Order { get; set; }
    public Customer Customer { get; set; } = null!;
    public ICollection<InvoiceLine> Lines { get; set; } = new List<InvoiceLine>();

    public bool IsEditable => Status == InvoiceStatus.Draft;
    public bool IsIssued => Status == InvoiceStatus.Issued || Status == InvoiceStatus.Sent
                            || Status == InvoiceStatus.PartiallyPaid || Status == InvoiceStatus.Paid
                            || Status == InvoiceStatus.Overdue;
    public bool IsFinalized => Status == InvoiceStatus.Paid || Status == InvoiceStatus.Void
                               || Status == InvoiceStatus.Cancelled;

    protected Invoice() { }

    public Invoice(
        string invoiceNumber,
        Guid customerId,
        string customerNameSnapshot,
        string currency,
        InvoiceType type = InvoiceType.SalesInvoice)
    {
        InvoiceNumber = invoiceNumber;
        CustomerId = customerId;
        CustomerNameSnapshot = customerNameSnapshot;
        Currency = currency;
        Type = type;
    }

    public void EnsureDraft()
    {
        if (Status != InvoiceStatus.Draft)
        {
            throw new InvoiceImmutableException(Status.ToString());
        }
    }

    public void UpdateDetails(
        DateTime issueDate,
        DateTime dueDate,
        DateTime postingDate,
        decimal exchangeRate,
        Guid? paymentTermsId,
        int? paymentTermsNetDaysSnapshot,
        decimal headerDiscountPercent,
        decimal headerDiscountAmount,
        decimal shippingCost,
        decimal roundingAdjustment,
        string? internalNotes,
        string? publicNotes,
        string? termsAndConditions,
        string? notes)
    {
        EnsureDraft();
        IssueDate = issueDate;
        DueDate = dueDate;
        PostingDate = postingDate;
        ExchangeRate = exchangeRate > 0 ? exchangeRate : 1m;
        PaymentTermsId = paymentTermsId;
        PaymentTermsNetDaysSnapshot = paymentTermsNetDaysSnapshot;
        HeaderDiscountPercent = headerDiscountPercent;
        HeaderDiscountAmount = headerDiscountAmount;
        ShippingCost = shippingCost;
        RoundingAdjustment = roundingAdjustment;
        InternalNotes = internalNotes;
        PublicNotes = publicNotes;
        TermsAndConditions = termsAndConditions;
        Notes = notes;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void ApplySnapshots(
        CustomerSnapshot customerSnapshot,
        AddressSnapshot? billingAddressSnapshot,
        AddressSnapshot? shippingAddressSnapshot)
    {
        CustomerSnapshot = customerSnapshot;
        CustomerNameSnapshot = customerSnapshot.LegalName;
        BillingAddressSnapshot = billingAddressSnapshot;
        ShippingAddressSnapshot = shippingAddressSnapshot;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void AttachToOrder(Guid orderId) => OrderId = orderId;
    public void AttachOriginInvoice(Guid originInvoiceId) => OriginInvoiceId = originInvoiceId;
    public void AttachCreditNote(Guid creditNoteId)
    {
        CreditNoteId = creditNoteId;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Recalculate()
    {
        Subtotal = Math.Round(Lines.Sum(l => l.LineSubtotal), 4);
        LineDiscountTotal = Math.Round(Lines.Sum(l => l.LineDiscountAmount), 4);
        var lineNet = Math.Round(Lines.Sum(l => l.LineNetAmount), 4);
        var headerDiscount = HeaderDiscountAmount + (lineNet * (HeaderDiscountPercent / 100m));
        var afterHeaderDiscount = lineNet - headerDiscount;
        TaxableTotal = Math.Round(afterHeaderDiscount, 4);
        TaxTotal = Math.Round(Lines.Sum(l => l.TaxAmount), 4);
        WithholdingTotal = Math.Round(Lines.Sum(l => l.WithholdingAmount), 4);
        Total = Math.Round(TaxableTotal + TaxTotal - WithholdingTotal + ShippingCost + RoundingAdjustment, 4);

        var breakdown = Lines
            .GroupBy(l => l.TaxRatePercent)
            .Select(g => new
            {
                rate = g.Key,
                @base = Math.Round(g.Sum(l => l.LineNetAmount), 4),
                amount = Math.Round(g.Sum(l => l.TaxAmount), 4),
            })
            .ToList();
        TaxBreakdownJson = System.Text.Json.JsonSerializer.Serialize(breakdown);
    }

    public void ReplaceLines(IEnumerable<InvoiceLine> newLines)
    {
        EnsureDraft();
        Lines.Clear();
        foreach (var line in newLines) Lines.Add(line);
        Recalculate();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Issue(string assignedNumber, Guid? approvedByUserId = null)
    {
        if (Status != InvoiceStatus.Draft)
        {
            throw new InvoiceStatusTransitionException(Status.ToString(), "issue");
        }
        if (Lines.Count == 0)
        {
            throw new CannotIssueEmptyInvoiceException();
        }
        Recalculate();
        var now = DateTime.UtcNow;
        InvoiceNumber = assignedNumber;
        Status = InvoiceStatus.Issued;
        IssuedAtUtc = now;
        ApprovedByUserId = approvedByUserId;
        IsPostedToLedger = true;
        UpdatedAtUtc = now;
        AddDomainEvent(new InvoiceIssuedEvent(TenantId, Id, CustomerId, OrderId, InvoiceNumber, Type, Total, Currency, now));
    }

    public void MarkSent()
    {
        if (!IsIssued) throw new InvoiceStatusTransitionException(Status.ToString(), "send");
        SentAtUtc = DateTime.UtcNow;
        if (Status == InvoiceStatus.Issued) Status = InvoiceStatus.Sent;
        UpdatedAtUtc = SentAtUtc.Value;
    }

    public void RecordPayment(decimal amount, DateTime now)
    {
        if (!IsIssued && Status != InvoiceStatus.Overdue)
        {
            throw new InvoiceStatusTransitionException(Status.ToString(), "record payment");
        }
        if (amount <= 0m) return;
        var remaining = Math.Max(0m, Total - AmountPaid);
        if (amount > remaining)
        {
            throw new CannotOverPayInvoiceException(remaining, amount);
        }
        AmountPaid = Math.Round(AmountPaid + amount, 4);
        if (AmountPaid >= Total)
        {
            Status = InvoiceStatus.Paid;
            PaidAtUtc = now;
            AddDomainEvent(new InvoicePaidEvent(TenantId, Id, CustomerId, InvoiceNumber, Total, Currency, now));
        }
        else
        {
            Status = InvoiceStatus.PartiallyPaid;
            AddDomainEvent(new InvoicePartiallyPaidEvent(TenantId, Id, CustomerId, InvoiceNumber, amount, Total - AmountPaid, Currency, now));
        }
        UpdatedAtUtc = now;
    }

    public void ReversePayment(decimal amount, DateTime now)
    {
        if (amount <= 0m) return;
        AmountPaid = Math.Max(0m, AmountPaid - amount);
        if (AmountPaid <= 0m)
        {
            if (Status == InvoiceStatus.Paid || Status == InvoiceStatus.PartiallyPaid)
            {
                Status = InvoiceStatus.Issued;
            }
            PaidAtUtc = null;
        }
        else
        {
            Status = InvoiceStatus.PartiallyPaid;
        }
        UpdatedAtUtc = now;
    }

    public void Void(string? reason, Guid? creditNoteId)
    {
        if (Status == InvoiceStatus.Void || Status == InvoiceStatus.Cancelled)
        {
            return;
        }
        var now = DateTime.UtcNow;
        var wasIssued = IsIssued;
        Status = InvoiceStatus.Void;
        VoidReason = reason;
        VoidedAtUtc = now;
        CreditNoteId = creditNoteId;
        UpdatedAtUtc = now;
        if (wasIssued)
        {
            AddDomainEvent(new InvoiceVoidedEvent(TenantId, Id, CustomerId, InvoiceNumber, Total, Currency, reason, now));
        }
    }

    public void Cancel(DateTime now)
    {
        if (Status == InvoiceStatus.Cancelled) return;
        var wasIssued = IsIssued;
        Status = InvoiceStatus.Cancelled;
        CancelledAtUtc = now;
        UpdatedAtUtc = now;
        AddDomainEvent(new InvoiceCancelledEvent(TenantId, Id, CustomerId, InvoiceNumber, Total, Currency, wasIssued, now));
    }

    public void MarkAsPaid(DateTime now)
    {
        if (AmountPaid < Total)
        {
            AmountPaid = Total;
        }
        Status = InvoiceStatus.Paid;
        PaidAtUtc = now;
        UpdatedAtUtc = now;
        AddDomainEvent(new InvoicePaidEvent(TenantId, Id, CustomerId, InvoiceNumber, Total, Currency, now));
    }

    public void RegisterEInvoice(string uuid, string status, string? pdfPath)
    {
        EInvoiceUuid = uuid;
        EInvoiceStatus = status;
        EInvoicePdfPath = pdfPath;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
