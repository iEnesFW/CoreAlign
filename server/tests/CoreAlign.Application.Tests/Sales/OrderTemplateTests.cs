using CoreAlign.Domain.Entities.Sales;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Tests.Sales;

public class OrderTemplateTests
{
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ProductId = Guid.NewGuid();

    [Fact]
    public void Schedule_set_to_weekly_computes_next_run_one_week_out()
    {
        var template = new OrderTemplate("Weekly resupply", CustomerId, "TRY", UserId);
        template.ReplaceLines(new[] { new OrderTemplateLine(ProductId, "SKU-1", "Test", 1m, 10m) });
        var now = new DateTime(2026, 6, 2, 12, 0, 0, DateTimeKind.Utc);

        template.SetSchedule(OrderFrequency.Weekly, null, now);

        template.Frequency.Should().Be(OrderFrequency.Weekly);
        template.NextRunAtUtc.Should().Be(now.AddDays(7));
    }

    [Fact]
    public void IsDue_true_when_active_and_next_run_in_past()
    {
        var template = new OrderTemplate("Daily", CustomerId, "TRY", UserId);
        template.ReplaceLines(new[] { new OrderTemplateLine(ProductId, "SKU-1", "Test", 1m, 10m) });
        var now = DateTime.UtcNow;
        template.SetSchedule(OrderFrequency.Daily, now.AddMinutes(-5), now.AddDays(-1));

        template.IsDue(now).Should().BeTrue();
    }

    [Fact]
    public void IsDue_false_when_deactivated()
    {
        var template = new OrderTemplate("Daily", CustomerId, "TRY", UserId);
        template.ReplaceLines(new[] { new OrderTemplateLine(ProductId, "SKU-1", "Test", 1m, 10m) });
        var now = DateTime.UtcNow;
        template.SetSchedule(OrderFrequency.Daily, now.AddMinutes(-5), now.AddDays(-1));
        template.SetActive(false);

        template.IsDue(now).Should().BeFalse();
    }

    [Fact]
    public void RecordRun_advances_next_run_by_frequency()
    {
        var template = new OrderTemplate("Weekly resupply", CustomerId, "TRY", UserId);
        template.ReplaceLines(new[] { new OrderTemplateLine(ProductId, "SKU-1", "Test", 1m, 10m) });
        var seedNow = new DateTime(2026, 6, 2, 8, 0, 0, DateTimeKind.Utc);
        template.SetSchedule(OrderFrequency.Weekly, seedNow, seedNow);

        var runAt = seedNow;
        template.RecordRun(runAt);

        template.LastRunAtUtc.Should().Be(runAt);
        template.NextRunAtUtc.Should().Be(runAt.AddDays(7));
    }

    [Fact]
    public void Setting_frequency_none_clears_next_run()
    {
        var template = new OrderTemplate("Daily", CustomerId, "TRY", UserId);
        template.ReplaceLines(new[] { new OrderTemplateLine(ProductId, "SKU-1", "Test", 1m, 10m) });
        var now = DateTime.UtcNow;
        template.SetSchedule(OrderFrequency.Daily, now, now);

        template.SetSchedule(OrderFrequency.None, null, now);

        template.Frequency.Should().Be(OrderFrequency.None);
        template.NextRunAtUtc.Should().BeNull();
    }

    [Fact]
    public void IsDue_false_after_record_run_advances_next_run_past_now()
    {
        var template = new OrderTemplate("Daily", CustomerId, "TRY", UserId);
        template.ReplaceLines(new[] { new OrderTemplateLine(ProductId, "SKU-1", "Test", 1m, 10m) });
        var now = new DateTime(2026, 6, 2, 12, 0, 0, DateTimeKind.Utc);
        template.SetSchedule(OrderFrequency.Daily, now.AddMinutes(-1), now);

        template.IsDue(now).Should().BeTrue();
        template.RecordRun(now);
        template.IsDue(now).Should().BeFalse();
    }
}
