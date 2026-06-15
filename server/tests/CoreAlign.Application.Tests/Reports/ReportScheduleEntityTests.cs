using CoreAlign.Domain.Entities.Reporting;

namespace CoreAlign.Application.Tests.Reports;

public class ReportScheduleEntityTests
{
    [Fact]
    public void Ctor_throws_when_name_is_empty()
    {
        Action act = () => new ReportSchedule(
            name: "",
            reportKey: "inventory-stock-on-hand",
            customReportDefinitionId: null,
            frequency: ReportFrequency.Daily,
            cronExpression: null,
            recipientsJson: "[\"ops@example.com\"]",
            format: ReportDeliveryFormat.Pdf,
            filtersJson: "{}",
            nextRunAtUtc: DateTime.UtcNow.AddHours(1));
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Ctor_throws_when_no_report_or_definition_provided()
    {
        Action act = () => new ReportSchedule(
            name: "Daily ops",
            reportKey: string.Empty,
            customReportDefinitionId: null,
            frequency: ReportFrequency.Daily,
            cronExpression: null,
            recipientsJson: "[]",
            format: ReportDeliveryFormat.Pdf,
            filtersJson: "{}",
            nextRunAtUtc: DateTime.UtcNow);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RecordRun_advances_next_run_and_clears_error_on_success()
    {
        var schedule = NewSchedule();
        var ranAt = DateTime.UtcNow;
        var next = ReportSchedule.ComputeNextRunAtUtc(ReportFrequency.Daily, ranAt);
        schedule.RecordRun(ranAt, "Ok", null, next);

        schedule.LastRunAtUtc.Should().BeCloseTo(ranAt, TimeSpan.FromSeconds(1));
        schedule.LastRunStatus.Should().Be("Ok");
        schedule.LastRunError.Should().BeNull();
        schedule.NextRunAtUtc.Should().BeCloseTo(next, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Activate_and_Deactivate_toggle_flag()
    {
        var schedule = NewSchedule();
        schedule.Deactivate();
        schedule.IsActive.Should().BeFalse();
        schedule.Activate();
        schedule.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData(ReportFrequency.Hourly, 1.0 / 24)]
    [InlineData(ReportFrequency.Daily, 1.0)]
    [InlineData(ReportFrequency.Weekly, 7.0)]
    public void ComputeNextRunAtUtc_advances_by_expected_window(ReportFrequency freq, double approxDays)
    {
        var anchor = new DateTime(2026, 6, 1, 8, 0, 0, DateTimeKind.Utc);
        var next = ReportSchedule.ComputeNextRunAtUtc(freq, anchor);
        var diff = next - anchor;
        diff.TotalDays.Should().BeApproximately(approxDays, 0.01);
    }

    [Fact]
    public void Monthly_advance_uses_calendar_month()
    {
        var anchor = new DateTime(2026, 1, 31, 8, 0, 0, DateTimeKind.Utc);
        var next = ReportSchedule.ComputeNextRunAtUtc(ReportFrequency.Monthly, anchor);
        next.Month.Should().Be(2);
    }

    private static ReportSchedule NewSchedule() =>
        new ReportSchedule(
            name: "Daily ops",
            reportKey: "inventory-stock-on-hand",
            customReportDefinitionId: null,
            frequency: ReportFrequency.Daily,
            cronExpression: null,
            recipientsJson: "[\"ops@example.com\"]",
            format: ReportDeliveryFormat.Pdf,
            filtersJson: "{}",
            nextRunAtUtc: DateTime.UtcNow.AddHours(1));
}
