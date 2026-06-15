using CoreAlign.Application.Mrp.Planning;
using CoreAlign.Infrastructure.Mrp.Planning;

namespace CoreAlign.Application.Tests.Mrp.Planning;

public class LowLevelCoderTests
{
    [Fact]
    public void Shared_component_gets_max_depth_across_paths()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var shared = Guid.NewGuid();
        var products = new[] { a, b, shared };
        var edges = new[]
        {
            new BomEdgeSnapshot(a, shared, 1m),
            new BomEdgeSnapshot(a, b, 1m),
            new BomEdgeSnapshot(b, shared, 1m)
        };

        var levels = LowLevelCoder.Assign(products, edges);

        levels[a].Should().Be(0);
        levels[b].Should().Be(1);
        levels[shared].Should().Be(2);
    }

    [Fact]
    public void Disconnected_products_are_level_zero()
    {
        var x = Guid.NewGuid();
        var y = Guid.NewGuid();

        var levels = LowLevelCoder.Assign(new[] { x, y }, new List<BomEdgeSnapshot>());

        levels[x].Should().Be(0);
        levels[y].Should().Be(0);
    }

    [Fact]
    public void Cycle_does_not_cause_infinite_recursion()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var edges = new[]
        {
            new BomEdgeSnapshot(a, b, 1m),
            new BomEdgeSnapshot(b, a, 1m)
        };

        var levels = LowLevelCoder.Assign(new[] { a, b }, edges);

        levels.Should().ContainKey(a);
        levels.Should().ContainKey(b);
    }
}

public class BucketCalendarTests
{
    [Fact]
    public void Day_buckets_map_dates_to_indices()
    {
        var calendar = new BucketCalendar(MrpPlanningTestData.AsOf, Domain.Enums.MrpBucketKind.Day, 10);

        calendar.Count.Should().Be(10);
        calendar.IndexFor(MrpPlanningTestData.AsOf.AddDays(3)).Should().Be(3);
        calendar.IndexFor(MrpPlanningTestData.AsOf.AddDays(-5)).Should().Be(0);
        calendar.IndexFor(MrpPlanningTestData.AsOf.AddDays(99)).Should().Be(9);
    }

    [Fact]
    public void Week_buckets_group_seven_days()
    {
        var calendar = new BucketCalendar(MrpPlanningTestData.AsOf, Domain.Enums.MrpBucketKind.Week, 28);

        calendar.Count.Should().Be(4);
        calendar.IndexFor(MrpPlanningTestData.AsOf.AddDays(8)).Should().Be(1);
        calendar.OffsetBuckets(10).Should().Be(2);
    }

    [Fact]
    public void Lead_time_offset_in_days_is_one_to_one()
    {
        var calendar = new BucketCalendar(MrpPlanningTestData.AsOf, Domain.Enums.MrpBucketKind.Day, 30);
        calendar.OffsetBuckets(5).Should().Be(5);
        calendar.OffsetBuckets(0).Should().Be(0);
    }
}
