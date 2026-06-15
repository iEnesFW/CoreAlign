using CoreAlign.Application.Mrp.Planning;
using CoreAlign.Domain.Enums;
using CoreAlign.Infrastructure.Mrp.Planning;

namespace CoreAlign.Application.Tests.Mrp.Planning;

public class DemandForecasterTests
{
    private readonly DemandForecaster _sut = new();

    private static List<DemandHistoryPointSnapshot> History(Guid productId, params (int dayOffset, decimal qty)[] points) =>
        points
            .Select(p => new DemandHistoryPointSnapshot(productId, MrpPlanningTestData.AsOf.AddDays(p.dayOffset), p.qty))
            .ToList();

    [Fact]
    public void MovingAverage_divides_total_by_window_days()
    {
        var id = Guid.NewGuid();
        var product = MrpPlanningTestData.Product(id, "A");
        var history = History(id, (-10, 30m), (-20, 60m));

        var result = _sut.Forecast(product, history, windowDays: 90, ForecastModel.MovingAverage);

        result.AverageDailyDemand.Should().Be(Math.Round(90m / 90m, 4));
    }

    [Fact]
    public void ExponentialSmoothing_applies_alpha_recurrence()
    {
        var id = Guid.NewGuid();
        var product = MrpPlanningTestData.Product(id, "A");
        var history = History(id, (-2, 10m), (-1, 20m), (0, 30m));

        var result = _sut.Forecast(product, history, windowDays: 3, ForecastModel.ExponentialSmoothing);

        var f0 = 10m;
        var f1 = (0.3m * 20m) + (0.7m * f0);
        var f2 = (0.3m * 30m) + (0.7m * f1);
        result.AverageDailyDemand.Should().Be(Math.Round(f2, 4));
    }

    [Fact]
    public void ExponentialSmoothing_incorporates_most_recent_day()
    {
        var id = Guid.NewGuid();
        var product = MrpPlanningTestData.Product(id, "A");
        var flat = History(id, (-2, 5m), (-1, 5m), (0, 5m));
        var spiked = History(id, (-2, 5m), (-1, 5m), (0, 100m));

        var flatForecast = _sut.Forecast(product, flat, windowDays: 3, ForecastModel.ExponentialSmoothing);
        var spikedForecast = _sut.Forecast(product, spiked, windowDays: 3, ForecastModel.ExponentialSmoothing);

        spikedForecast.AverageDailyDemand.Should().BeGreaterThan(flatForecast.AverageDailyDemand);
    }

    [Fact]
    public void ServiceLevelTarget_zero_keeps_stored_safety_stock()
    {
        var id = Guid.NewGuid();
        var product = MrpPlanningTestData.Product(id, "A", safetyStock: 15m, serviceLevelTarget: 0m, leadTimeDays: 9);
        var history = History(id, (-1, 50m), (-2, 10m));

        var result = _sut.Forecast(product, history, windowDays: 90, ForecastModel.ExponentialSmoothing);

        result.SafetyStock.Should().Be(15m);
    }

    [Fact]
    public void ServiceLevelTarget_drives_z_sigma_sqrt_lead_time()
    {
        var id = Guid.NewGuid();
        var product = MrpPlanningTestData.Product(id, "A", safetyStock: 0m, serviceLevelTarget: 0.95m, leadTimeDays: 4);
        var history = History(id, (-1, 10m), (-2, 0m));

        var result = _sut.Forecast(product, history, windowDays: 2, ForecastModel.MovingAverage);

        var z = ZScore.ForServiceLevel(0.95m);
        var expected = z * result.DailyStandardDeviation * (decimal)Math.Sqrt(4);
        result.SafetyStock.Should().BeApproximately(expected, 0.001m);
    }

    [Fact]
    public void Computed_safety_stock_takes_max_with_stored()
    {
        var id = Guid.NewGuid();
        var product = MrpPlanningTestData.Product(id, "A", safetyStock: 9999m, serviceLevelTarget: 0.90m, leadTimeDays: 1);
        var history = History(id, (-1, 5m));

        var result = _sut.Forecast(product, history, windowDays: 30, ForecastModel.MovingAverage);

        result.SafetyStock.Should().Be(9999m);
    }

    [Fact]
    public void StandardDeviation_accounts_for_zero_demand_days_in_window()
    {
        var id = Guid.NewGuid();
        var product = MrpPlanningTestData.Product(id, "A");
        var history = History(id, (-1, 10m));

        var result = _sut.Forecast(product, history, windowDays: 10, ForecastModel.MovingAverage);

        var mean = 10m / 10m;
        var sumSquares = ((10m - mean) * (10m - mean)) + (9 * (mean * mean));
        var expected = Math.Round((decimal)Math.Sqrt((double)(sumSquares / 9)), 4);
        result.DailyStandardDeviation.Should().Be(expected);
    }

    [Fact]
    public void DailyForecast_length_equals_horizon_and_is_non_negative()
    {
        var id = Guid.NewGuid();
        var product = MrpPlanningTestData.Product(id, "A");
        var history = History(id, (-1, 10m), (-2, 5m), (-3, 7m));

        var result = _sut.Forecast(product, history, windowDays: 30, ForecastModel.ExponentialSmoothing, horizonDays: 14);

        result.DailyForecast.Should().HaveCount(14);
        result.DailyForecast.Should().OnlyContain(v => v >= 0m);
    }

    [Fact]
    public void HoltLinear_rising_series_trends_upward()
    {
        var id = Guid.NewGuid();
        var product = MrpPlanningTestData.Product(id, "A");
        var points = Enumerable.Range(0, 20)
            .Select(i => (dayOffset: -19 + i, qty: (decimal)(10 + i)))
            .ToArray();
        var history = History(id, points);

        var result = _sut.Forecast(product, history, windowDays: 20, ForecastModel.HoltLinear, horizonDays: 7);

        result.DailyForecast.Should().HaveCount(7);
        result.DailyForecast[^1].Should().BeGreaterThan(result.DailyForecast[0]);
        result.DailyForecast[0].Should().BeGreaterThan(0m);
    }

    [Fact]
    public void HoltWinters_repeats_seasonal_shape_with_period_7()
    {
        var id = Guid.NewGuid();
        var product = MrpPlanningTestData.Product(id, "A");
        var seasonShape = new decimal[] { 5m, 10m, 20m, 10m, 5m, 2m, 1m };
        var points = new List<(int dayOffset, decimal qty)>();
        var totalDays = 8 * 7;
        for (var i = 0; i < totalDays; i++)
        {
            points.Add((-(totalDays - 1) + i, seasonShape[i % 7]));
        }
        var history = History(id, points.ToArray());

        var result = _sut.Forecast(product, history, windowDays: totalDays, ForecastModel.HoltWinters, horizonDays: 14);

        result.DailyForecast.Should().HaveCount(14);
        result.DailyForecast.Should().OnlyContain(v => v >= 0m);

        var firstWeek = result.DailyForecast.Take(7).ToList();
        var secondWeek = result.DailyForecast.Skip(7).Take(7).ToList();
        for (var i = 0; i < 7; i++)
        {
            secondWeek[i].Should().BeApproximately(firstWeek[i], firstWeek[i] * 0.25m + 1m);
        }
        var peakIndex = firstWeek.IndexOf(firstWeek.Max());
        peakIndex.Should().Be(2);
    }

    [Fact]
    public void HoltWinters_short_history_falls_back_without_throwing()
    {
        var id = Guid.NewGuid();
        var product = MrpPlanningTestData.Product(id, "A");
        var history = History(id, (-1, 10m), (-2, 8m), (-3, 6m));

        var act = () => _sut.Forecast(product, history, windowDays: 5, ForecastModel.HoltWinters, horizonDays: 5);

        act.Should().NotThrow();
        var result = act();
        result.DailyForecast.Should().HaveCount(5);
        result.DailyForecast.Should().OnlyContain(v => v >= 0m);
        result.AverageDailyDemand.Should().BeGreaterThan(0m);
    }

    [Fact]
    public void HoltLinear_single_point_falls_back_to_scalar_without_throwing()
    {
        var id = Guid.NewGuid();
        var product = MrpPlanningTestData.Product(id, "A");
        var history = History(id, (0, 12m));

        var act = () => _sut.Forecast(product, history, windowDays: 1, ForecastModel.HoltLinear, horizonDays: 3);

        act.Should().NotThrow();
        var result = act();
        result.DailyForecast.Should().HaveCount(3);
        result.DailyForecast.Should().OnlyContain(v => v >= 0m);
    }
}

public class ZScoreTests
{
    [Theory]
    [InlineData(0.90, 1.2816)]
    [InlineData(0.95, 1.6449)]
    [InlineData(0.99, 2.3263)]
    public void ForServiceLevel_returns_table_values(double level, double expected)
    {
        ZScore.ForServiceLevel((decimal)level).Should().Be((decimal)expected);
    }

    [Fact]
    public void ForServiceLevel_interpolates_between_table_entries()
    {
        var z = ZScore.ForServiceLevel(0.925m);
        z.Should().BeGreaterThan(1.2816m).And.BeLessThan(1.6449m);
    }

    [Fact]
    public void ForServiceLevel_clamps_at_extremes()
    {
        ZScore.ForServiceLevel(0.10m).Should().Be(0m);
        ZScore.ForServiceLevel(0.9999m).Should().Be(3.0902m);
    }
}
