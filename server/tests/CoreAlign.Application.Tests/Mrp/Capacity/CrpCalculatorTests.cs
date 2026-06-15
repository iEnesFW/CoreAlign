using CoreAlign.Application.Mrp.Capacity;

namespace CoreAlign.Application.Tests.Mrp.Capacity;

public class CrpCalculatorTests
{
    private readonly CrpCalculator _sut = new();

    private static CrpWorkCenterSnapshot WorkCenter(Guid id, decimal dailyCapacityMinutes, string code = "WC1") =>
        new(id, code, $"Work Center {code}", dailyCapacityMinutes);

    private static CrpProductionLoad Load(
        Guid? workCenterId,
        decimal runtime,
        decimal qty,
        int bucketIndex,
        Guid? productId = null) =>
        new(productId ?? Guid.NewGuid(), workCenterId, runtime, qty, bucketIndex);

    private static CrpInput Input(
        IReadOnlyList<CrpProductionLoad> loads,
        IReadOnlyList<CrpWorkCenterSnapshot> workCenters,
        int bucketCount,
        int daysPerBucket) =>
        new(loads, workCenters, bucketCount, daysPerBucket);

    [Fact]
    public void Load_is_quantity_times_runtime_summed_per_bucket()
    {
        var wc = Guid.NewGuid();

        var input = Input(
            new[]
            {
                Load(wc, runtime: 30m, qty: 4m, bucketIndex: 0),
                Load(wc, runtime: 30m, qty: 6m, bucketIndex: 0),
                Load(wc, runtime: 10m, qty: 2m, bucketIndex: 1),
            },
            new[] { WorkCenter(wc, dailyCapacityMinutes: 10_000m) },
            bucketCount: 3,
            daysPerBucket: 1);

        var result = _sut.Compute(input);

        var buckets = result.WorkCenters.Single().Buckets;
        buckets[0].LoadMinutes.Should().Be(300m); // (4*30) + (6*30)
        buckets[1].LoadMinutes.Should().Be(20m);  // 2*10
        buckets[2].LoadMinutes.Should().Be(0m);
    }

    [Fact]
    public void Capacity_per_day_bucket_is_daily_capacity_times_one()
    {
        var wc = Guid.NewGuid();

        var input = Input(
            Array.Empty<CrpProductionLoad>(),
            new[] { WorkCenter(wc, dailyCapacityMinutes: 480m) },
            bucketCount: 2,
            daysPerBucket: 1);

        var result = _sut.Compute(input);

        result.WorkCenters.Single().Buckets.Should().OnlyContain(b => b.CapacityMinutes == 480m);
    }

    [Fact]
    public void Week_bucket_capacity_is_daily_capacity_times_seven()
    {
        var wc = Guid.NewGuid();

        var input = Input(
            Array.Empty<CrpProductionLoad>(),
            new[] { WorkCenter(wc, dailyCapacityMinutes: 480m) },
            bucketCount: 2,
            daysPerBucket: 7);

        var result = _sut.Compute(input);

        result.WorkCenters.Single().Buckets.Should().OnlyContain(b => b.CapacityMinutes == 3_360m); // 480 * 7
    }

    [Fact]
    public void Overload_flagged_when_load_exceeds_capacity()
    {
        var wc = Guid.NewGuid();

        var input = Input(
            new[]
            {
                Load(wc, runtime: 30m, qty: 20m, bucketIndex: 0), // 600 min > 480
                Load(wc, runtime: 30m, qty: 10m, bucketIndex: 1), // 300 min < 480
            },
            new[] { WorkCenter(wc, dailyCapacityMinutes: 480m) },
            bucketCount: 2,
            daysPerBucket: 1);

        var result = _sut.Compute(input);

        var buckets = result.WorkCenters.Single().Buckets;
        buckets[0].LoadMinutes.Should().Be(600m);
        buckets[0].IsOverloaded.Should().BeTrue();
        buckets[1].LoadMinutes.Should().Be(300m);
        buckets[1].IsOverloaded.Should().BeFalse();
    }

    [Fact]
    public void Load_exactly_equal_to_capacity_is_not_overloaded()
    {
        var wc = Guid.NewGuid();

        var input = Input(
            new[] { Load(wc, runtime: 48m, qty: 10m, bucketIndex: 0) }, // 480 == capacity
            new[] { WorkCenter(wc, dailyCapacityMinutes: 480m) },
            bucketCount: 1,
            daysPerBucket: 1);

        var result = _sut.Compute(input);

        var bucket = result.WorkCenters.Single().Buckets.Single();
        bucket.LoadMinutes.Should().Be(480m);
        bucket.IsOverloaded.Should().BeFalse();
    }

    [Fact]
    public void Unrouted_load_with_null_work_center_is_excluded_and_counted()
    {
        var wc = Guid.NewGuid();

        var input = Input(
            new[]
            {
                Load(wc, runtime: 30m, qty: 5m, bucketIndex: 0),
                Load(workCenterId: null, runtime: 30m, qty: 99m, bucketIndex: 0),
            },
            new[] { WorkCenter(wc, dailyCapacityMinutes: 10_000m) },
            bucketCount: 1,
            daysPerBucket: 1);

        var result = _sut.Compute(input);

        result.UnroutedProductionOrderCount.Should().Be(1);
        result.WorkCenters.Single().Buckets.Single().LoadMinutes.Should().Be(150m); // only the routed 5*30
    }

    [Fact]
    public void Load_for_unknown_work_center_is_treated_as_unrouted()
    {
        var known = Guid.NewGuid();
        var unknown = Guid.NewGuid();

        var input = Input(
            new[] { Load(unknown, runtime: 30m, qty: 5m, bucketIndex: 0) },
            new[] { WorkCenter(known, dailyCapacityMinutes: 480m) },
            bucketCount: 1,
            daysPerBucket: 1);

        var result = _sut.Compute(input);

        result.UnroutedProductionOrderCount.Should().Be(1);
        result.WorkCenters.Single().Buckets.Single().LoadMinutes.Should().Be(0m);
    }

    [Fact]
    public void Loads_are_attributed_to_their_own_work_center()
    {
        var wcA = Guid.NewGuid();
        var wcB = Guid.NewGuid();

        var input = Input(
            new[]
            {
                Load(wcA, runtime: 10m, qty: 3m, bucketIndex: 0),
                Load(wcB, runtime: 20m, qty: 4m, bucketIndex: 0),
            },
            new[]
            {
                WorkCenter(wcA, dailyCapacityMinutes: 10_000m, code: "A"),
                WorkCenter(wcB, dailyCapacityMinutes: 10_000m, code: "B"),
            },
            bucketCount: 1,
            daysPerBucket: 1);

        var result = _sut.Compute(input);

        result.WorkCenters.Single(w => w.WorkCenterId == wcA).Buckets.Single().LoadMinutes.Should().Be(30m);
        result.WorkCenters.Single(w => w.WorkCenterId == wcB).Buckets.Single().LoadMinutes.Should().Be(80m);
    }

    [Fact]
    public void Every_work_center_emits_a_bucket_per_period()
    {
        var wc = Guid.NewGuid();

        var input = Input(
            Array.Empty<CrpProductionLoad>(),
            new[] { WorkCenter(wc, dailyCapacityMinutes: 480m) },
            bucketCount: 5,
            daysPerBucket: 1);

        var result = _sut.Compute(input);

        result.WorkCenters.Single().Buckets.Should().HaveCount(5);
    }

    [Fact]
    public void Output_is_deterministic_and_ordered_by_work_center_code()
    {
        var wcZ = Guid.NewGuid();
        var wcA = Guid.NewGuid();

        var input = Input(
            Array.Empty<CrpProductionLoad>(),
            new[]
            {
                WorkCenter(wcZ, dailyCapacityMinutes: 480m, code: "ZZZ"),
                WorkCenter(wcA, dailyCapacityMinutes: 480m, code: "AAA"),
            },
            bucketCount: 1,
            daysPerBucket: 1);

        var first = _sut.Compute(input).WorkCenters.Select(w => w.Code).ToList();
        var second = _sut.Compute(input).WorkCenters.Select(w => w.Code).ToList();

        first.Should().Equal("AAA", "ZZZ");
        second.Should().Equal(first);
    }
}
