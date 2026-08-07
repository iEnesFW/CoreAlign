using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Events;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Domain.Entities;

public class Invoice : TenantEntity, IHasConcurrencyToken
{
    public long ConcurrencyToken { get; private set; }
    void IHasConcurrencyToken.BumpConcurrencyToken() => ConcurrencyToken++;

    public string InvoiceNumber { get; private set; } = string.Empty;
    public InvoiceType Type { get; private set; } = InvoiceType.SalesInvoice;
    public InvoiceStatus Status { get; private set; } = InvoiceStatus.Draft;

    public Guid? OrderId { get; private set; }
    public Guid? OriginInvoiceId { get; private set; }
    public Guid? CreditNoteId { get; private set; }
    public Guid? ReturnRequestId { get; private set; }

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

    public decimal? FxRateSnapshot { get; private set; }
    public string? FxSource { get; private set; }
    public DateTime? FxLockedAtUtc { get; private set; }

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
    public string? EInvoiceProfile { get; private set; }
    public string? EInvoiceGibStatusCode { get; private set; }
    public string? EInvoiceRejectReason { get; private set; }
    public DateTime? EInvoiceSentAtUtc { get; private set; }
    public DateTime? EInvoiceLastSyncUtc { get; private set; }

    public Guid? VatExemptionCodeId { get; private set; }
    public string? VatExemptionCode { get; private set; }
    public string? VatExemptionReason { get; private set; }

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

    public static Invoice IssueCreditNote(
        Invoice origin,
        string creditNumber,
        DateTime nowUtc,
        IEnumerable<InvoiceLine> lines,
        string? reason,
        Guid? approvedByUserId,
        Guid? returnRequestId)
    {
        if (origin is null) throw new ArgumentNullException(nameof(origin));
        if (string.IsNullOrWhiteSpace(creditNumber)) throw new ArgumentException("Credit note number is required.", nameof(creditNumber));

        var creditNote = new Invoice(
            creditNumber,
            origin.CustomerId,
            origin.CustomerNameSnapshot,
            origin.Currency,
            InvoiceType.CreditNote)
        {
            TenantId = origin.TenantId,
        };
        creditNote.IssueDate = nowUtc;
        creditNote.DueDate = nowUtc;
        creditNote.PostingDate = nowUtc.Date;
        creditNote.ExchangeRate = origin.ExchangeRate;
        creditNote.FxRateSnapshot = origin.FxRateSnapshot;
        creditNote.FxSource = origin.FxSource;
        creditNote.FxLockedAtUtc = origin.FxLockedAtUtc;
        creditNote.PaymentTermsId = origin.PaymentTermsId;
        creditNote.PaymentTermsNetDaysSnapshot = origin.PaymentTermsNetDaysSnapshot;
        creditNote.CustomerSnapshot = origin.CustomerSnapshot;
        creditNote.BillingAddressSnapshot = origin.BillingAddressSnapshot;
        creditNote.ShippingAddressSnapshot = origin.ShippingAddressSnapshot;
        creditNote.InternalNotes = reason;
        creditNote.ApprovedByUserId = approvedByUserId;
        creditNote.ReturnRequestId = returnRequestId;
        creditNote.AttachOriginInvoice(origin.Id);

        // Carry the origin's header-level charges so the credit note reverses the
        // origin exactly. Percentage-based header discount pro-rates naturally with
        // the credited lines; the absolute amounts (header discount, shipping,
        // rounding) are scaled by the credited fraction of the origin's line net so
        // a partial credit reverses only its share and a full credit reverses all.
        creditNote.HeaderDiscountPercent = origin.HeaderDiscountPercent;
        var fraction = CreditedFraction(origin, lines);
        creditNote.HeaderDiscountAmount = Math.Round(origin.HeaderDiscountAmount * fraction, 4);
        creditNote.ShippingCost = Math.Round(origin.ShippingCost * fraction, 4);
        creditNote.RoundingAdjustment = Math.Round(origin.RoundingAdjustment * fraction, 4);

        creditNote.ReplaceLines(lines);
        creditNote.Issue(creditNumber);
        return creditNote;
    }

    private static decimal CreditedFraction(Invoice origin, IEnumerable<InvoiceLine> creditLines)
    {
        var originNet = origin.Lines.Sum(l => l.LineNetAmount);
        if (originNet <= 0m) return 0m;
        var creditedNet = creditLines.Sum(l => l.LineNetAmount);
        var fraction = creditedNet / originNet;
        return fraction > 1m ? 1m : fraction;
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
        AddDomainEvent(new InvoiceIssuedEvent(
            TenantId, Id, CustomerId, OrderId, InvoiceNumber, Type, Total, Currency, now, ExchangeRate,
            TaxableTotal, TaxTotal, WithholdingTotal, ShippingCost, RoundingAdjustment));
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
            AddDomainEvent(new InvoiceVoidedEvent(TenantId, Id, CustomerId, InvoiceNumber, Total, Currency, reason, now, ExchangeRate));
        }
    }

    public void WriteOff(DateTime now, string? reason)
    {
        // Terminal + idempotent: a second write-off is a no-op (no phantom second
        // AR reversal / GL expense), mirroring Void's terminal-state guard.
        if (Status == InvoiceStatus.WrittenOff) return;
        if (!IsIssued)
        {
            throw new InvoiceStatusTransitionException(Status.ToString(), "write off");
        }
        var amount = AmountDue;
        if (amount <= 0m)
        {
            // Nothing outstanding to write off (e.g. already paid).
            throw new InvoiceStatusTransitionException(Status.ToString(), "write off");
        }
        Status = InvoiceStatus.WrittenOff;
        UpdatedAtUtc = now;
        AddDomainEvent(new InvoiceWrittenOffEvent(TenantId, Id, CustomerId, InvoiceNumber, amount, Currency, reason, now, ExchangeRate));
    }

    public void Cancel(DateTime now)
    {
        if (Status == InvoiceStatus.Cancelled) return;
        var wasIssued = IsIssued;
        Status = InvoiceStatus.Cancelled;
        CancelledAtUtc = now;
        UpdatedAtUtc = now;
        AddDomainEvent(new InvoiceCancelledEvent(TenantId, Id, CustomerId, InvoiceNumber, Total, Currency, wasIssued, now, ExchangeRate));
    }

    public void MarkAsPaid(DateTime now)
    {
        if (Status == InvoiceStatus.Void || Status == InvoiceStatus.Cancelled)
        {
            // A voided/cancelled invoice already had its AR reversed; flipping it to
            // Paid would emit a phantom InvoicePaidEvent against AR that no longer
            // exists. Reject the illegal terminal-state transition.
            throw new InvoiceStatusTransitionException(Status.ToString(), "mark as paid");
        }
        if (AmountPaid < Total)
        {
            AmountPaid = Total;
        }
        Status = InvoiceStatus.Paid;
        PaidAtUtc = now;
        UpdatedAtUtc = now;
        AddDomainEvent(new InvoicePaidEvent(TenantId, Id, CustomerId, InvoiceNumber, Total, Currency, now));
    }

    public void ApplyFxRateSnapshot(decimal rate, string source, DateTime lockedAtUtc)
    {
        if (rate <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(rate), "Exchange rate must be positive.");
        }
        if (string.IsNullOrWhiteSpace(source))
        {
            throw new ArgumentException("Source is required.", nameof(source));
        }
        FxRateSnapshot = rate;
        FxSource = source.Trim().ToUpperInvariant();
        FxLockedAtUtc = DateTime.SpecifyKind(lockedAtUtc, DateTimeKind.Utc);
        ExchangeRate = rate;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void RegisterEInvoice(string? uuid, string status, string? pdfPath)
    {
        EInvoiceUuid = uuid ?? EInvoiceUuid;
        EInvoicePdfPath = pdfPath ?? EInvoicePdfPath;
        ApplyEInvoiceStatus(status, gibStatusCode: null, rejectReason: null);
    }

    public void SetEInvoiceProfile(string profile)
    {
        EInvoiceProfile = profile;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public bool ApplyEInvoiceStatus(string status, string? gibStatusCode, string? rejectReason)
    {
        var normalized = EInvoiceStatuses.Normalize(status);
        EInvoiceLastSyncUtc = DateTime.UtcNow;

        if (EInvoiceStatuses.IsTerminal(EInvoiceStatus) &&
            !string.Equals(EInvoiceStatus, normalized, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var changed = !string.Equals(EInvoiceStatus, normalized, StringComparison.Ordinal);
        EInvoiceStatus = normalized;
        if (gibStatusCode is not null) EInvoiceGibStatusCode = gibStatusCode;
        if (rejectReason is not null) EInvoiceRejectReason = rejectReason;
        if (EInvoiceSentAtUtc is null &&
            string.Equals(normalized, EInvoiceStatuses.Submitted, StringComparison.OrdinalIgnoreCase))
        {
            EInvoiceSentAtUtc = DateTime.UtcNow;
        }

        UpdatedAtUtc = DateTime.UtcNow;
        return changed;
    }

    public void SetVatExemption(Guid? codeId, string? code, string? reason)
    {
        EnsureDraft();
        VatExemptionCodeId = codeId;
        VatExemptionCode = code;
        VatExemptionReason = reason;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
