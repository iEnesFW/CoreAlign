using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Domain.Entities;

public class Quote : TenantEntity
{
    public string QuoteNumber { get; private set; } = string.Empty;
    public QuoteStatus Status { get; private set; } = QuoteStatus.Draft;

    public Guid CustomerId { get; private set; }
    public Guid? BillingAddressId { get; private set; }
    public Guid? ShippingAddressId { get; private set; }

    public CustomerSnapshot? CustomerSnapshot { get; private set; }
    public AddressSnapshot? BillingAddressSnapshot { get; private set; }
    public AddressSnapshot? ShippingAddressSnapshot { get; private set; }

    public DateTime QuoteDate { get; private set; } = DateTime.UtcNow;
    public DateTime ValidUntilUtc { get; private set; } = DateTime.UtcNow.AddDays(30);
    public DateTime? SentAtUtc { get; private set; }
    public DateTime? AcceptedAtUtc { get; private set; }
    public DateTime? RejectedAtUtc { get; private set; }
    public DateTime? ExpiredAtUtc { get; private set; }
    public DateTime? ConvertedAtUtc { get; private set; }

    public string Currency { get; private set; } = "TRY";
    public decimal ExchangeRate { get; private set; } = 1m;

    public Guid? PaymentTermsId { get; private set; }
    public int? PaymentTermsNetDaysSnapshot { get; private set; }
    public Guid? PriceListId { get; private set; }
    public Guid? SalesRepUserId { get; private set; }

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

    public Guid? ConvertedOrderId { get; private set; }
    public string? RejectionReason { get; private set; }
    public string? InternalNotes { get; private set; }
    public string? CustomerNotes { get; private set; }
    public string? PublicNotes { get; private set; }
    public string? TermsAndConditions { get; private set; }
    public string? Notes { get; private set; }

    public Customer Customer { get; set; } = null!;
    public ICollection<QuoteLine> Lines { get; private set; } = new List<QuoteLine>();

    public bool IsDraft => Status == QuoteStatus.Draft;
    public bool IsEditable => Status == QuoteStatus.Draft;
    public bool IsTerminal =>
        Status == QuoteStatus.Accepted
        || Status == QuoteStatus.Rejected
        || Status == QuoteStatus.Expired;

    protected Quote() { }

    public Quote(
        string quoteNumber,
        Guid customerId,
        DateTime quoteDate,
        DateTime validUntilUtc,
        string currency,
        string? notes = null)
    {
        QuoteNumber = quoteNumber;
        CustomerId = customerId;
        QuoteDate = quoteDate;
        ValidUntilUtc = validUntilUtc;
        Currency = currency;
        Notes = notes;
    }

    public void UpdateHeader(
        string quoteNumber,
        Guid customerId,
        DateTime quoteDate,
        DateTime validUntilUtc,
        string currency,
        string? notes)
    {
        EnsureDraft();
        QuoteNumber = quoteNumber;
        CustomerId = customerId;
        QuoteDate = quoteDate;
        ValidUntilUtc = validUntilUtc;
        Currency = currency;
        Notes = notes;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateDetails(
        Guid? billingAddressId,
        Guid? shippingAddressId,
        Guid? paymentTermsId,
        Guid? priceListId,
        decimal exchangeRate,
        decimal shippingCost,
        decimal headerDiscountPercent,
        decimal headerDiscountAmount,
        Guid? salesRepUserId,
        string? internalNotes,
        string? customerNotes,
        string? publicNotes,
        string? termsAndConditions,
        decimal roundingAdjustment = 0m)
    {
        EnsureDraft();
        BillingAddressId = billingAddressId;
        ShippingAddressId = shippingAddressId;
        PaymentTermsId = paymentTermsId;
        PriceListId = priceListId;
        ExchangeRate = exchangeRate > 0 ? exchangeRate : 1m;
        ShippingCost = shippingCost;
        HeaderDiscountPercent = headerDiscountPercent;
        HeaderDiscountAmount = headerDiscountAmount;
        SalesRepUserId = salesRepUserId;
        InternalNotes = internalNotes;
        CustomerNotes = customerNotes;
        PublicNotes = publicNotes;
        TermsAndConditions = termsAndConditions;
        RoundingAdjustment = roundingAdjustment;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void ApplySnapshots(
        CustomerSnapshot customerSnapshot,
        AddressSnapshot? billingAddressSnapshot,
        AddressSnapshot? shippingAddressSnapshot,
        int? paymentTermsNetDays)
    {
        CustomerSnapshot = customerSnapshot;
        BillingAddressSnapshot = billingAddressSnapshot;
        ShippingAddressSnapshot = shippingAddressSnapshot;
        PaymentTermsNetDaysSnapshot = paymentTermsNetDays;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void ReplaceLines(IEnumerable<QuoteLine> newLines)
    {
        EnsureDraft();
        Lines.Clear();
        foreach (var line in newLines)
        {
            Lines.Add(line);
        }
        Recalculate();
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
        TaxTotal = Math.Round(Lines.Sum(l => l.LineTaxAmount), 4);
        WithholdingTotal = Math.Round(Lines.Sum(l => l.LineWithholdingAmount), 4);
        Total = Math.Round(TaxableTotal + TaxTotal - WithholdingTotal + ShippingCost + RoundingAdjustment, 4);
    }

    public void MarkSent()
    {
        if (Status != QuoteStatus.Draft)
        {
            throw new InvalidQuoteStatusTransitionException(Status.ToString(), QuoteStatus.Sent.ToString());
        }
        if (Lines.Count == 0)
        {
            throw new InvalidQuoteLineException("Cannot send a quote with no lines.");
        }
        Status = QuoteStatus.Sent;
        SentAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = SentAtUtc.Value;
    }

    public void Accept()
    {
        if (Status != QuoteStatus.Sent)
        {
            throw new InvalidQuoteStatusTransitionException(Status.ToString(), QuoteStatus.Accepted.ToString());
        }
        Status = QuoteStatus.Accepted;
        AcceptedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = AcceptedAtUtc.Value;
    }

    public void Reject(string? reason)
    {
        if (Status != QuoteStatus.Sent && Status != QuoteStatus.Draft)
        {
            throw new InvalidQuoteStatusTransitionException(Status.ToString(), QuoteStatus.Rejected.ToString());
        }
        Status = QuoteStatus.Rejected;
        RejectionReason = reason;
        RejectedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = RejectedAtUtc.Value;
    }

    public void Expire(DateTime nowUtc)
    {
        if (Status != QuoteStatus.Sent)
        {
            throw new InvalidQuoteStatusTransitionException(Status.ToString(), QuoteStatus.Expired.ToString());
        }
        Status = QuoteStatus.Expired;
        ExpiredAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
    }

    public void AttachConvertedOrder(Guid orderId)
    {
        if (Status != QuoteStatus.Accepted)
        {
            throw new InvalidQuoteStatusTransitionException(Status.ToString(), "Convert");
        }
        if (ConvertedOrderId.HasValue)
        {
            throw new QuoteAlreadyConvertedException();
        }
        ConvertedOrderId = orderId;
        ConvertedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = ConvertedAtUtc.Value;
    }

    private void EnsureDraft()
    {
        if (Status != QuoteStatus.Draft)
        {
            throw new QuoteImmutableException(Status.ToString());
        }
    }
}
