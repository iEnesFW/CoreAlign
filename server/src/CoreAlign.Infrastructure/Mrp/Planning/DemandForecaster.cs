using CoreAlign.Application.Mrp.Planning;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Infrastructure.Mrp.Planning;

public sealed record ForecastResult(
    decimal AverageDailyDemand,
    decimal DailyStandardDeviation,
    decimal SafetyStock,
    IReadOnlyList<decimal> DailyForecast);

public interface IDemandForecaster
{
    ForecastResult Forecast(
        MrpProductSnapshot product,
        IReadOnlyList<DemandHistoryPointSnapshot> history,
        int windowDays,
        ForecastModel model,
        int horizonDays);
}

public sealed class DemandForecaster : IDemandForecaster
{
    public const decimal DefaultSmoothingAlpha = 0.3m;
    public const decimal DefaultTrendBeta = 0.1m;
    public const decimal DefaultSeasonalGamma = 0.1m;

    public const int WeeklySeasonalPeriod = 7;
    public const int SeasonalHistoryWindowDays = 365;

    public ForecastResult Forecast(
        MrpProductSnapshot product,
        IReadOnlyList<DemandHistoryPointSnapshot> history,
        int windowDays,
        ForecastModel model)
        => Forecast(product, history, windowDays, model, windowDays);

    public ForecastResult Forecast(
        MrpProductSnapshot product,
        IReadOnlyList<DemandHistoryPointSnapshot> history,
        int windowDays,
        ForecastModel model,
        int horizonDays)
    {
        var effectiveWindow = windowDays > 0 ? windowDays : 1;
        var effectiveHorizon = horizonDays > 0 ? horizonDays : 1;

        var dailyQuantities = history
            .Where(h => h.Quantity > 0m)
            .GroupBy(h => h.DayUtc.Date)
            .Select(g => g.Sum(x => x.Quantity))
            .ToList();

        var series = BuildDailySeries(history, effectiveWindow);
        var dailyForecast = ComputeDailyForecast(series, model, effectiveHorizon);

        var averageDaily = AverageOf(dailyForecast);
        var sigma = ComputeDailyStandardDeviation(dailyQuantities, effectiveWindow);
        var safetyStock = ComputeSafetyStock(product, sigma);

        return new ForecastResult(
            Math.Round(averageDaily, 4),
            Math.Round(sigma, 4),
            Math.Round(safetyStock, 4),
            dailyForecast.Select(v => Math.Round(v, 4)).ToList());
    }

    private static decimal[] BuildDailySeries(
        IReadOnlyList<DemandHistoryPointSnapshot> history,
        int windowDays)
    {
        var perDay = new decimal[windowDays];
        if (history.Count == 0)
        {
            return perDay;
        }

        var fromDay = history.Max(h => h.DayUtc.Date).AddDays(-(windowDays - 1));
        foreach (var point in history)
        {
            if (point.Quantity <= 0m)
            {
                continue;
            }
            var index = (point.DayUtc.Date - fromDay).Days;
            if (index >= 0 && index < windowDays)
            {
                perDay[index] += point.Quantity;
            }
        }
        return perDay;
    }

    private static IReadOnlyList<decimal> ComputeDailyForecast(
        decimal[] series,
        ForecastModel model,
        int horizonDays)
    {
        return model switch
        {
            ForecastModel.MovingAverage => Flat(MovingAverageLevel(series), horizonDays),
            ForecastModel.ExponentialSmoothing => Flat(ExponentialSmoothingLevel(series), horizonDays),
            ForecastModel.HoltLinear => HoltLinearForecast(series, horizonDays),
            ForecastModel.HoltWinters => HoltWintersForecast(series, horizonDays, WeeklySeasonalPeriod),
            _ => Flat(ExponentialSmoothingLevel(series), horizonDays)
        };
    }

    private static IReadOnlyList<decimal> Flat(decimal level, int horizonDays)
    {
        var clamped = Math.Max(0m, level);
        var result = new decimal[horizonDays];
        for (var i = 0; i < horizonDays; i++)
        {
            result[i] = clamped;
        }
        return result;
    }

    private static decimal MovingAverageLevel(decimal[] series)
    {
        if (series.Length == 0)
        {
            return 0m;
        }
        return series.Sum() / series.Length;
    }

    private static decimal ExponentialSmoothingLevel(decimal[] series)
    {
        if (series.Length == 0)
        {
            return 0m;
        }

        decimal forecast = series[0];
        for (var i = 1; i < series.Length; i++)
        {
            forecast = (DefaultSmoothingAlpha * series[i]) + ((1m - DefaultSmoothingAlpha) * forecast);
        }
        return forecast;
    }

    private static IReadOnlyList<decimal> HoltLinearForecast(decimal[] series, int horizonDays)
    {
        if (series.Length < 2)
        {
            return Flat(ExponentialSmoothingLevel(series), horizonDays);
        }

        var level = series[0];
        var trend = series[1] - series[0];

        for (var i = 1; i < series.Length; i++)
        {
            var previousLevel = level;
            level = (DefaultSmoothingAlpha * series[i]) + ((1m - DefaultSmoothingAlpha) * (level + trend));
            trend = (DefaultTrendBeta * (level - previousLevel)) + ((1m - DefaultTrendBeta) * trend);
        }

        var result = new decimal[horizonDays];
        for (var h = 1; h <= horizonDays; h++)
        {
            result[h - 1] = Math.Max(0m, level + (h * trend));
        }
        return result;
    }

    private static IReadOnlyList<decimal> HoltWintersForecast(decimal[] series, int horizonDays, int period)
    {
        if (period <= 1 || series.Length < 2 * period)
        {
            return HoltLinearForecast(series, horizonDays);
        }

        var seasonCount = series.Length / period;
        var seasonAverages = new decimal[seasonCount];
        for (var s = 0; s < seasonCount; s++)
        {
            decimal sum = 0m;
            for (var i = 0; i < period; i++)
            {
                sum += series[(s * period) + i];
            }
            seasonAverages[s] = sum / period;
        }

        var seasonal = new decimal[period];
        for (var i = 0; i < period; i++)
        {
            decimal sum = 0m;
            for (var s = 0; s < seasonCount; s++)
            {
                sum += series[(s * period) + i] - seasonAverages[s];
            }
            seasonal[i] = sum / seasonCount;
        }

        var level = seasonAverages[0];
        var trend = (seasonAverages[seasonCount - 1] - seasonAverages[0]) / ((seasonCount - 1) * period);

        for (var t = 0; t < series.Length; t++)
        {
            var seasonalIndex = seasonal[t % period];
            var previousLevel = level;
            level = (DefaultSmoothingAlpha * (series[t] - seasonalIndex))
                + ((1m - DefaultSmoothingAlpha) * (level + trend));
            trend = (DefaultTrendBeta * (level - previousLevel)) + ((1m - DefaultTrendBeta) * trend);
            seasonal[t % period] = (DefaultSeasonalGamma * (series[t] - level))
                + ((1m - DefaultSeasonalGamma) * seasonalIndex);
        }

        var result = new decimal[horizonDays];
        for (var h = 1; h <= horizonDays; h++)
        {
            var seasonalIndex = seasonal[(series.Length + h - 1) % period];
            result[h - 1] = Math.Max(0m, level + (h * trend) + seasonalIndex);
        }
        return result;
    }

    private static decimal AverageOf(IReadOnlyList<decimal> values)
    {
        if (values.Count == 0)
        {
            return 0m;
        }
        decimal sum = 0m;
        foreach (var v in values)
        {
            sum += v;
        }
        return sum / values.Count;
    }

    private static decimal ComputeDailyStandardDeviation(IReadOnlyList<decimal> dailyQuantities, int windowDays)
    {
        if (windowDays <= 1)
        {
            return 0m;
        }

        var mean = dailyQuantities.Sum() / windowDays;
        decimal sumSquares = 0m;
        var daysWithData = dailyQuantities.Count;
        foreach (var qty in dailyQuantities)
        {
            var diff = qty - mean;
            sumSquares += diff * diff;
        }
        var zeroDays = windowDays - daysWithData;
        if (zeroDays > 0)
        {
            sumSquares += zeroDays * (mean * mean);
        }

        var variance = sumSquares / (windowDays - 1);
        return (decimal)Math.Sqrt((double)variance);
    }

    private static decimal ComputeSafetyStock(MrpProductSnapshot product, decimal sigma)
    {
        if (product.ServiceLevelTarget <= 0m)
        {
            return product.SafetyStock;
        }

        var z = ZScore.ForServiceLevel(product.ServiceLevelTarget);
        var leadTime = product.LeadTimeDays > 0 ? product.LeadTimeDays : 1;
        var computed = z * sigma * (decimal)Math.Sqrt(leadTime);
        return Math.Max(product.SafetyStock, computed);
    }
}

public static class ZScore
{
    private static readonly (decimal Level, decimal Z)[] Table =
    {
        (0.50m, 0m),
        (0.80m, 0.8416m),
        (0.85m, 1.0364m),
        (0.90m, 1.2816m),
        (0.95m, 1.6449m),
        (0.975m, 1.9600m),
        (0.99m, 2.3263m),
        (0.999m, 3.0902m)
    };

    public static decimal ForServiceLevel(decimal serviceLevel)
    {
        if (serviceLevel <= Table[0].Level)
        {
            return Table[0].Z;
        }
        var last = Table[^1];
        if (serviceLevel >= last.Level)
        {
            return last.Z;
        }

        for (var i = 1; i < Table.Length; i++)
        {
            var upper = Table[i];
            if (serviceLevel <= upper.Level)
            {
                var lower = Table[i - 1];
                var span = upper.Level - lower.Level;
                if (span <= 0m)
                {
                    return upper.Z;
                }
                var ratio = (serviceLevel - lower.Level) / span;
                return lower.Z + (ratio * (upper.Z - lower.Z));
            }
        }
        return last.Z;
    }
}
