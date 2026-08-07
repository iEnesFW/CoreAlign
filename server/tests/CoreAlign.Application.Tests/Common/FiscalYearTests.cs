using CoreAlign.Domain.Entities;

namespace CoreAlign.Application.Tests.Common;

public class FiscalYearTests
{
    [Fact]
    public void A_calendar_year_tenant_gets_january_to_january()
    {
        var range = FiscalYear.For(2026, 1);

        range.StartUtc.Should().Be(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        range.EndExclusiveUtc.Should().Be(new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    // WHY the label is the STARTING calendar year: a year opening in October 2026 spends nine of
    // its twelve months in 2027, but every Turkish ledger calls it 2026.
    [Fact]
    public void An_october_start_year_is_labelled_by_the_year_it_opens_in()
    {
        var range = FiscalYear.For(2026, 10);

        range.StartUtc.Should().Be(new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc));
        range.EndExclusiveUtc.Should().Be(new DateTime(2027, 10, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    [Theory]
    [InlineData("2026-01-01", 1, 2026)]
    [InlineData("2026-12-31", 1, 2026)]
    [InlineData("2026-09-30", 10, 2025)]
    [InlineData("2026-10-01", 10, 2026)]
    [InlineData("2027-09-30", 10, 2026)]
    [InlineData("2027-10-01", 10, 2027)]
    public void A_date_falls_in_the_year_that_opened_before_it(string date, int startMonth, int expected)
    {
        var instant = DateTime.SpecifyKind(
            DateTime.Parse(date, System.Globalization.CultureInfo.InvariantCulture),
            DateTimeKind.Utc);

        FiscalYear.YearOf(instant, startMonth).Should().Be(expected);
    }

    [Fact]
    public void The_boundary_is_half_open_so_no_date_lands_in_two_years()
    {
        var current = FiscalYear.For(2026, 4);
        var next = FiscalYear.For(2027, 4);
        var boundary = new DateTime(2027, 4, 1, 0, 0, 0, DateTimeKind.Utc);

        current.Contains(boundary).Should().BeFalse();
        next.Contains(boundary).Should().BeTrue();
        current.EndExclusiveUtc.Should().Be(next.StartUtc);
    }

    // WHY 0 is a real input: tenants.fiscal_year_start_month carried a DB default of 0 for its
    // whole life, so a row written outside EF can hold it.
    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    [InlineData(-3)]
    public void An_impossible_start_month_falls_back_to_the_calendar_year(int bogus)
    {
        FiscalYear.NormalizeStartMonth(bogus).Should().Be(1);
        FiscalYear.For(2026, bogus).StartUtc.Should().Be(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        FiscalYear.YearOf(new DateTime(2026, 7, 5, 0, 0, 0, DateTimeKind.Utc), bogus).Should().Be(2026);
    }

    [Fact]
    public void A_non_utc_instant_is_read_as_utc_rather_than_shifted()
    {
        var unspecified = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Unspecified);

        FiscalYear.YearOf(unspecified, 10).Should().Be(2026);
    }

    [Fact]
    public void Containing_returns_the_window_that_holds_the_instant()
    {
        var range = FiscalYear.Containing(new DateTime(2027, 2, 14, 0, 0, 0, DateTimeKind.Utc), 10);

        range.Year.Should().Be(2026);
        range.Contains(new DateTime(2027, 2, 14, 0, 0, 0, DateTimeKind.Utc)).Should().BeTrue();
    }
}
