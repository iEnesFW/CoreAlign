using CoreAlign.Application.Mrp.Planning;
using CoreAlign.Domain.Enums;
using CoreAlign.Infrastructure.Mrp.Planning;

namespace CoreAlign.Application.Tests.Mrp.Planning;

public class MrpForecastConsumptionEngineTests
{
    private static IndependentDemandSnapshot Demand(Guid productId, decimal qty, int dayOffset) =>
        new(productId, qty, MrpPlanningTestData.AsOf.AddDays(dayOffset), Guid.NewGuid());

    private static MrpPlanningEngine Engine(IDemandForecaster forecaster) =>
        new(new LotSizingCalculator(), forecaster, new ActionMessageGenerator());

    [Fact]
    public void Per_product_forecast_model_is_honoured()
    {
        var recorder = new RecordingForecaster();
        var sut = Engine(recorder);

        var id = Guid.NewGuid();
        var product = MrpPlanningTestData.Product(
            id, "A", policy: LotSizingPolicy.LotForLot, forecastModel: ForecastModel.HoltWinters);
        var snapshot = MrpPlanningTestData.Snapshot(new[] { product });

        sut.Run(snapshot, MrpBucketKind.Day, horizonDays: 10);

        recorder.ModelsByProduct[id].Should().Be(ForecastModel.HoltWinters);
    }

    [Fact]
    public void Distinct_products_use_their_own_forecast_models()
    {
        var recorder = new RecordingForecaster();
        var sut = Engine(recorder);

        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var products = new[]
        {
            MrpPlanningTestData.Product(a, "A", policy: LotSizingPolicy.LotForLot, forecastModel: ForecastModel.MovingAverage),
            MrpPlanningTestData.Product(b, "B", policy: LotSizingPolicy.LotForLot, forecastModel: ForecastModel.HoltLinear)
        };
        var snapshot = MrpPlanningTestData.Snapshot(products);

        sut.Run(snapshot, MrpBucketKind.Day, horizonDays: 10);

        recorder.ModelsByProduct[a].Should().Be(ForecastModel.MovingAverage);
        recorder.ModelsByProduct[b].Should().Be(ForecastModel.HoltLinear);
    }

    [Fact]
    public void Forecast_above_actual_in_future_bucket_inflates_gross_to_forecast()
    {
        var id = Guid.NewGuid();
        var dailyForecast = new decimal[20];
        dailyForecast[5] = 40m;
        var forecaster = new StubForecaster(dailyForecast);
        var sut = Engine(forecaster);

        var product = MrpPlanningTestData.Product(id, "A", policy: LotSizingPolicy.LotForLot, leadTimeDays: 0);
        var snapshot = MrpPlanningTestData.Snapshot(
            new[] { product },
            demand: new[] { Demand(id, 10m, 5) });

        var result = sut.Run(snapshot, MrpBucketKind.Day, horizonDays: 20);
        var item = result.Items.Single();

        item.Buckets[5].GrossRequirements.Should().Be(40m);
        item.PlannedOrders.Should().ContainSingle();
        item.PlannedOrders[0].Quantity.Should().Be(40m);
    }

    [Fact]
    public void Forecast_below_actual_is_fully_consumed_no_extra_gross()
    {
        var id = Guid.NewGuid();
        var dailyForecast = new decimal[20];
        dailyForecast[5] = 10m;
        var forecaster = new StubForecaster(dailyForecast);
        var sut = Engine(forecaster);

        var product = MrpPlanningTestData.Product(id, "A", policy: LotSizingPolicy.LotForLot, leadTimeDays: 0);
        var snapshot = MrpPlanningTestData.Snapshot(
            new[] { product },
            demand: new[] { Demand(id, 30m, 5) });

        var result = sut.Run(snapshot, MrpBucketKind.Day, horizonDays: 20);
        var item = result.Items.Single();

        item.Buckets[5].GrossRequirements.Should().Be(30m);
        item.PlannedOrders.Single().Quantity.Should().Be(30m);
    }

    [Fact]
    public void Empty_forecast_vector_leaves_gross_untouched()
    {
        var id = Guid.NewGuid();
        var forecaster = new StubForecaster(Array.Empty<decimal>());
        var sut = Engine(forecaster);

        var product = MrpPlanningTestData.Product(id, "A", policy: LotSizingPolicy.LotForLot, leadTimeDays: 0);
        var snapshot = MrpPlanningTestData.Snapshot(
            new[] { product },
            demand: new[] { Demand(id, 25m, 4) });

        var result = sut.Run(snapshot, MrpBucketKind.Day, horizonDays: 20);
        var item = result.Items.Single();

        item.Buckets[4].GrossRequirements.Should().Be(25m);
        item.PlannedOrders.Single().Quantity.Should().Be(25m);
    }

    [Fact]
    public void Weekly_buckets_aggregate_daily_forecast_over_bucket_days()
    {
        var id = Guid.NewGuid();
        var dailyForecast = new decimal[14];
        for (var i = 0; i < 7; i++)
        {
            dailyForecast[i] = 3m;
        }
        var forecaster = new StubForecaster(dailyForecast);
        var sut = Engine(forecaster);

        var product = MrpPlanningTestData.Product(id, "A", policy: LotSizingPolicy.LotForLot, leadTimeDays: 0);
        var snapshot = MrpPlanningTestData.Snapshot(new[] { product });

        var result = sut.Run(snapshot, MrpBucketKind.Week, horizonDays: 14);
        var item = result.Items.Single();

        item.Buckets[0].GrossRequirements.Should().Be(21m);
    }

    [Fact]
    public void AbcClass_flows_from_snapshot_to_item_plan()
    {
        var recorder = new RecordingForecaster();
        var sut = Engine(recorder);

        var id = Guid.NewGuid();
        var product = MrpPlanningTestData.Product(
            id, "A", policy: LotSizingPolicy.LotForLot, abcClass: AbcClass.A);
        var snapshot = MrpPlanningTestData.Snapshot(new[] { product });

        var result = sut.Run(snapshot, MrpBucketKind.Day, horizonDays: 10);

        result.Items.Single().AbcClass.Should().Be(AbcClass.A);
    }

    private sealed class RecordingForecaster : IDemandForecaster
    {
        public Dictionary<Guid, ForecastModel> ModelsByProduct { get; } = new();

        public ForecastResult Forecast(
            MrpProductSnapshot product,
            IReadOnlyList<DemandHistoryPointSnapshot> history,
            int windowDays,
            ForecastModel model,
            int horizonDays)
        {
            ModelsByProduct[product.ProductId] = model;
            return new ForecastResult(0m, 0m, 0m, Array.Empty<decimal>());
        }
    }

    private sealed class StubForecaster : IDemandForecaster
    {
        private readonly IReadOnlyList<decimal> _dailyForecast;

        public StubForecaster(IReadOnlyList<decimal> dailyForecast) => _dailyForecast = dailyForecast;

        public ForecastResult Forecast(
            MrpProductSnapshot product,
            IReadOnlyList<DemandHistoryPointSnapshot> history,
            int windowDays,
            ForecastModel model,
            int horizonDays)
            => new(0m, 0m, 0m, _dailyForecast);
    }
}
