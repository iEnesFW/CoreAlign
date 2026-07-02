using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Services;

namespace CoreAlign.Domain.Entities.Invoices;

public class RecurringInvoiceTemplate : TenantEntity
{
    public string Name { get; private set; } = string.Empty;
    public Guid CustomerId { get; private set; }
    public string Currency { get; private set; } = "TRY";

    public RecurrenceFrequency Frequency { get; private set; }
    public int IntervalCount { get; private set; } = 1;
    public int? AnchorDayOfMonth { get; private set; }
    public DayOfWeek? AnchorDayOfWeek { get; private set; }

    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public int? MaxOccurrences { get; private set; }

    public DateOnly NextRunDate { get; private set; }
    public DateOnly? LastRunDate { get; private set; }
    public int OccurrencesGenerated { get; private set; }

    public int DueDays { get; private set; } = 30;
    public Guid? PaymentTermsId { get; private set; }
    public decimal? HeaderDiscountPercent { get; private set; }
    public decimal? HeaderDiscountAmount { get; private set; }
    public decimal? ShippingCost { get; private set; }
    public decimal? RoundingAdjustment { get; private set; }

    public RecurringInvoiceStatus Status { get; private set; } = RecurringInvoiceStatus.Active;
    public bool AutoConfirm { get; private set; } = true;
    public string? PublicNotes { get; private set; }
    public string? InternalNotes { get; private set; }
    public Guid CreatedByUserId { get; private set; }

    public ICollection<RecurringInvoiceTemplateLine> Lines { get; private set; } = new List<RecurringInvoiceTemplateLine>();
    public ICollection<RecurringInvoiceOccurrence> Occurrences { get; private set; } = new List<RecurringInvoiceOccurrence>();

    protected RecurringInvoiceTemplate() { }

    public RecurringInvoiceTemplate(
        string name,
        Guid customerId,
        string currency,
        Guid createdByUserId,
        RecurrenceFrequency frequency,
        int intervalCount,
        int? anchorDayOfMonth,
        DayOfWeek? anchorDayOfWeek,
        DateOnly startDate,
        DateOnly? endDate,
        int? maxOccurrences,
        int dueDays,
        Guid? paymentTermsId,
        decimal? headerDiscountPercent,
        decimal? headerDiscountAmount,
        decimal? shippingCost,
        decimal? roundingAdjustment,
        bool autoConfirm,
        string? publicNotes,
        string? internalNotes)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));
        if (customerId == Guid.Empty) throw new ArgumentException("Customer id is required.", nameof(customerId));
        if (string.IsNullOrWhiteSpace(currency)) throw new ArgumentException("Currency is required.", nameof(currency));
        if (createdByUserId == Guid.Empty) throw new ArgumentException("Created by user id is required.", nameof(createdByUserId));
        if (intervalCount < 1) throw new ArgumentOutOfRangeException(nameof(intervalCount), "Interval must be at least 1.");
        if (endDate.HasValue && endDate.Value < startDate) throw new ArgumentException("End date cannot precede start date.", nameof(endDate));
        if (anchorDayOfMonth is < 1 or > 31) throw new ArgumentOutOfRangeException(nameof(anchorDayOfMonth), "Anchor day of month must be between 1 and 31.");
        if (maxOccurrences is < 1) throw new ArgumentOutOfRangeException(nameof(maxOccurrences), "Max occurrences must be at least 1.");

        Name = name.Trim();
        CustomerId = customerId;
        Currency = currency;
        CreatedByUserId = createdByUserId;
        Frequency = frequency;
        IntervalCount = intervalCount;
        AnchorDayOfMonth = anchorDayOfMonth;
        AnchorDayOfWeek = anchorDayOfWeek;
        StartDate = startDate;
        EndDate = endDate;
        MaxOccurrences = maxOccurrences;
        NextRunDate = startDate;
        DueDays = dueDays < 0 ? 0 : dueDays;
        PaymentTermsId = paymentTermsId;
        HeaderDiscountPercent = headerDiscountPercent;
        HeaderDiscountAmount = headerDiscountAmount;
        ShippingCost = shippingCost;
        RoundingAdjustment = roundingAdjustment;
        AutoConfirm = autoConfirm;
        PublicNotes = publicNotes;
        InternalNotes = internalNotes;
        Status = RecurringInvoiceStatus.Active;
    }

    public void UpdateDetails(
        string name,
        Guid customerId,
        string currency,
        RecurrenceFrequency frequency,
        int intervalCount,
        int? anchorDayOfMonth,
        DayOfWeek? anchorDayOfWeek,
        DateOnly startDate,
        DateOnly? endDate,
        int? maxOccurrences,
        int dueDays,
        Guid? paymentTermsId,
        decimal? headerDiscountPercent,
        decimal? headerDiscountAmount,
        decimal? shippingCost,
        decimal? roundingAdjustment,
        bool autoConfirm,
        string? publicNotes,
        string? internalNotes)
    {
        if (Status is RecurringInvoiceStatus.Cancelled or RecurringInvoiceStatus.Completed)
            throw new InvalidRecurringInvoiceTransitionException("A cancelled or completed template cannot be edited.");
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));
        if (customerId == Guid.Empty) throw new ArgumentException("Customer id is required.", nameof(customerId));
        if (string.IsNullOrWhiteSpace(currency)) throw new ArgumentException("Currency is required.", nameof(currency));
        if (intervalCount < 1) throw new ArgumentOutOfRangeException(nameof(intervalCount), "Interval must be at least 1.");
        if (endDate.HasValue && endDate.Value < startDate) throw new ArgumentException("End date cannot precede start date.", nameof(endDate));
        if (anchorDayOfMonth is < 1 or > 31) throw new ArgumentOutOfRangeException(nameof(anchorDayOfMonth), "Anchor day of month must be between 1 and 31.");
        if (maxOccurrences is < 1) throw new ArgumentOutOfRangeException(nameof(maxOccurrences), "Max occurrences must be at least 1.");

        Name = name.Trim();
        CustomerId = customerId;
        Currency = currency;
        Frequency = frequency;
        IntervalCount = intervalCount;
        AnchorDayOfMonth = anchorDayOfMonth;
        AnchorDayOfWeek = anchorDayOfWeek;
        StartDate = startDate;
        EndDate = endDate;
        MaxOccurrences = maxOccurrences;
        DueDays = dueDays < 0 ? 0 : dueDays;
        PaymentTermsId = paymentTermsId;
        HeaderDiscountPercent = headerDiscountPercent;
        HeaderDiscountAmount = headerDiscountAmount;
        ShippingCost = shippingCost;
        RoundingAdjustment = roundingAdjustment;
        AutoConfirm = autoConfirm;
        PublicNotes = publicNotes;
        InternalNotes = internalNotes;

        if (OccurrencesGenerated == 0)
        {
            NextRunDate = startDate;
        }
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void ReplaceLines(IEnumerable<RecurringInvoiceTemplateLine> newLines)
    {
        Lines.Clear();
        var index = 1;
        foreach (var line in newLines)
        {
            line.AttachTo(this, index++);
            Lines.Add(line);
        }
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public bool IsDue(DateOnly today) => Status == RecurringInvoiceStatus.Active && NextRunDate <= today;

    public void Pause()
    {
        if (Status != RecurringInvoiceStatus.Active)
            throw new InvalidRecurringInvoiceTransitionException("Only an active template can be paused.");
        Status = RecurringInvoiceStatus.Paused;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Resume(DateOnly today)
    {
        if (Status != RecurringInvoiceStatus.Paused)
            throw new InvalidRecurringInvoiceTransitionException("Only a paused template can be resumed.");
        Status = RecurringInvoiceStatus.Active;
        var guard = 0;
        while (NextRunDate < today && guard++ < 1000)
        {
            NextRunDate = Advance(NextRunDate);
        }
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status == RecurringInvoiceStatus.Cancelled) return;
        if (Status == RecurringInvoiceStatus.Completed)
            throw new InvalidRecurringInvoiceTransitionException("A completed template cannot be cancelled.");
        Status = RecurringInvoiceStatus.Cancelled;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void RecordOccurrence(DateOnly periodKey, Guid generatedInvoiceId, DateTime nowUtc)
    {
        var occurrence = new RecurringInvoiceOccurrence(periodKey, generatedInvoiceId, nowUtc);
        occurrence.AttachTo(this);
        Occurrences.Add(occurrence);

        OccurrencesGenerated++;
        LastRunDate = periodKey;
        NextRunDate = Advance(periodKey);

        var reachedMax = MaxOccurrences.HasValue && OccurrencesGenerated >= MaxOccurrences.Value;
        var pastEnd = EndDate.HasValue && NextRunDate > EndDate.Value;
        if (reachedMax || pastEnd)
        {
            Status = RecurringInvoiceStatus.Completed;
        }
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private DateOnly Advance(DateOnly from) =>
        RecurrenceSchedule.ComputeNext(Frequency, IntervalCount, AnchorDayOfMonth, AnchorDayOfWeek, from);
}
