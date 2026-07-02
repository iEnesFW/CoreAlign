using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Services;

namespace CoreAlign.Application.Tests.Invoices.Recurring;

public class RecurrenceScheduleTests
{
    [Fact]
    public void Monthly_anchor_31_clamps_to_february_end_in_non_leap_year()
    {
        var next = RecurrenceSchedule.ComputeNext(
            RecurrenceFrequency.Monthly, 1, anchorDayOfMonth: 31, anchorDayOfWeek: null,
            from: new DateOnly(2026, 1, 31));

        next.Should().Be(new DateOnly(2026, 2, 28));
    }

    [Fact]
    public void Monthly_anchor_31_reasserts_to_31_after_a_short_month()
    {
        var next = RecurrenceSchedule.ComputeNext(
            RecurrenceFrequency.Monthly, 1, anchorDayOfMonth: 31, anchorDayOfWeek: null,
            from: new DateOnly(2026, 2, 28));

        next.Should().Be(new DateOnly(2026, 3, 31));
    }

    [Fact]
    public void Monthly_anchor_29_lands_on_feb_29_in_a_leap_year()
    {
        var next = RecurrenceSchedule.ComputeNext(
            RecurrenceFrequency.Monthly, 1, anchorDayOfMonth: 29, anchorDayOfWeek: null,
            from: new DateOnly(2024, 1, 29));

        next.Should().Be(new DateOnly(2024, 2, 29));
    }

    [Fact]
    public void Monthly_without_anchor_preserves_day_then_clamps()
    {
        var afterJan = RecurrenceSchedule.ComputeNext(
            RecurrenceFrequency.Monthly, 1, anchorDayOfMonth: null, anchorDayOfWeek: null,
            from: new DateOnly(2026, 1, 15));
        afterJan.Should().Be(new DateOnly(2026, 2, 15));

        var afterJan31 = RecurrenceSchedule.ComputeNext(
            RecurrenceFrequency.Monthly, 1, anchorDayOfMonth: null, anchorDayOfWeek: null,
            from: new DateOnly(2026, 1, 31));
        afterJan31.Should().Be(new DateOnly(2026, 2, 28));
    }

    [Fact]
    public void Monthly_interval_2_advances_two_months()
    {
        var next = RecurrenceSchedule.ComputeNext(
            RecurrenceFrequency.Monthly, 2, anchorDayOfMonth: 10, anchorDayOfWeek: null,
            from: new DateOnly(2026, 1, 10));

        next.Should().Be(new DateOnly(2026, 3, 10));
    }

    [Fact]
    public void Quarterly_anchor_31_from_november_clamps_february()
    {
        var next = RecurrenceSchedule.ComputeNext(
            RecurrenceFrequency.Quarterly, 1, anchorDayOfMonth: 31, anchorDayOfWeek: null,
            from: new DateOnly(2025, 11, 30));

        next.Should().Be(new DateOnly(2026, 2, 28));
    }

    [Fact]
    public void Yearly_advances_twelve_months_with_anchor()
    {
        var next = RecurrenceSchedule.ComputeNext(
            RecurrenceFrequency.Yearly, 1, anchorDayOfMonth: 29, anchorDayOfWeek: null,
            from: new DateOnly(2024, 2, 29));

        next.Should().Be(new DateOnly(2025, 2, 28));
    }

    [Fact]
    public void Weekly_interval_2_adds_fourteen_days()
    {
        var next = RecurrenceSchedule.ComputeNext(
            RecurrenceFrequency.Weekly, 2, anchorDayOfMonth: null, anchorDayOfWeek: null,
            from: new DateOnly(2026, 6, 1));

        next.Should().Be(new DateOnly(2026, 6, 15));
    }

    [Fact]
    public void Weekly_snaps_forward_to_anchor_day_of_week()
    {
        var from = new DateOnly(2026, 6, 1);
        from.DayOfWeek.Should().Be(DayOfWeek.Monday);

        var next = RecurrenceSchedule.ComputeNext(
            RecurrenceFrequency.Weekly, 1, anchorDayOfMonth: null, anchorDayOfWeek: DayOfWeek.Friday,
            from: from);

        next.DayOfWeek.Should().Be(DayOfWeek.Friday);
        next.Should().Be(new DateOnly(2026, 6, 12));
    }
}
