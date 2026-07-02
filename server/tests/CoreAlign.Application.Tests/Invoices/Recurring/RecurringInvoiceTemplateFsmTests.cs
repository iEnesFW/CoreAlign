using CoreAlign.Domain.Entities.Invoices;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Application.Tests.Invoices.Recurring;

public class RecurringInvoiceTemplateFsmTests
{
    private static RecurringInvoiceTemplate Build(
        DateOnly? start = null,
        DateOnly? end = null,
        int? maxOccurrences = null)
    {
        return new RecurringInvoiceTemplate(
            name: "Monthly retainer",
            customerId: Guid.NewGuid(),
            currency: "TRY",
            createdByUserId: Guid.NewGuid(),
            frequency: RecurrenceFrequency.Monthly,
            intervalCount: 1,
            anchorDayOfMonth: 1,
            anchorDayOfWeek: null,
            startDate: start ?? new DateOnly(2026, 1, 1),
            endDate: end,
            maxOccurrences: maxOccurrences,
            dueDays: 30,
            paymentTermsId: null,
            headerDiscountPercent: null,
            headerDiscountAmount: null,
            shippingCost: null,
            roundingAdjustment: null,
            autoConfirm: true,
            publicNotes: null,
            internalNotes: null);
    }

    [Fact]
    public void New_template_is_active_with_next_run_on_start_date()
    {
        var t = Build();
        t.Status.Should().Be(RecurringInvoiceStatus.Active);
        t.NextRunDate.Should().Be(new DateOnly(2026, 1, 1));
        t.OccurrencesGenerated.Should().Be(0);
    }

    [Fact]
    public void Pause_then_resume_transitions_status()
    {
        var t = Build();
        t.Pause();
        t.Status.Should().Be(RecurringInvoiceStatus.Paused);
        t.Resume(new DateOnly(2026, 1, 1));
        t.Status.Should().Be(RecurringInvoiceStatus.Active);
    }

    [Fact]
    public void Pausing_a_non_active_template_is_rejected()
    {
        var t = Build();
        t.Pause();
        var act = () => t.Pause();
        act.Should().Throw<InvalidRecurringInvoiceTransitionException>();
    }

    [Fact]
    public void Resuming_a_non_paused_template_is_rejected()
    {
        var t = Build();
        var act = () => t.Resume(new DateOnly(2026, 1, 1));
        act.Should().Throw<InvalidRecurringInvoiceTransitionException>();
    }

    [Fact]
    public void Resume_fast_forwards_next_run_past_today_without_backfill()
    {
        var t = Build(start: new DateOnly(2026, 1, 1));
        t.Pause();
        t.Resume(new DateOnly(2026, 6, 30));
        t.NextRunDate.Should().Be(new DateOnly(2026, 7, 1));
        t.OccurrencesGenerated.Should().Be(0);
    }

    [Fact]
    public void Cancel_is_terminal_and_idempotent()
    {
        var t = Build();
        t.Cancel();
        t.Status.Should().Be(RecurringInvoiceStatus.Cancelled);
        var act = () => t.Cancel();
        act.Should().NotThrow();
        t.Status.Should().Be(RecurringInvoiceStatus.Cancelled);
    }

    [Fact]
    public void Record_occurrence_advances_cursor_and_counts()
    {
        var t = Build();
        var periodKey = t.NextRunDate;
        t.RecordOccurrence(periodKey, Guid.NewGuid(), DateTime.UtcNow);

        t.OccurrencesGenerated.Should().Be(1);
        t.LastRunDate.Should().Be(periodKey);
        t.NextRunDate.Should().Be(new DateOnly(2026, 2, 1));
        t.Status.Should().Be(RecurringInvoiceStatus.Active);
    }

    [Fact]
    public void Record_occurrence_completes_when_max_occurrences_reached()
    {
        var t = Build(maxOccurrences: 1);
        t.RecordOccurrence(t.NextRunDate, Guid.NewGuid(), DateTime.UtcNow);
        t.Status.Should().Be(RecurringInvoiceStatus.Completed);
    }

    [Fact]
    public void Record_occurrence_completes_when_next_run_passes_end_date()
    {
        var t = Build(start: new DateOnly(2026, 1, 1), end: new DateOnly(2026, 1, 15));
        t.RecordOccurrence(t.NextRunDate, Guid.NewGuid(), DateTime.UtcNow);
        t.Status.Should().Be(RecurringInvoiceStatus.Completed);
    }

    [Fact]
    public void Is_due_is_true_only_for_active_template_at_or_after_next_run()
    {
        var t = Build(start: new DateOnly(2026, 1, 1));
        t.IsDue(new DateOnly(2026, 1, 1)).Should().BeTrue();
        t.IsDue(new DateOnly(2025, 12, 31)).Should().BeFalse();
        t.Pause();
        t.IsDue(new DateOnly(2026, 1, 1)).Should().BeFalse();
    }
}
