using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Mrp;

public readonly record struct AbcPolicyDefault(
    decimal ServiceLevelTarget,
    LotSizingPolicy Policy,
    ForecastModel ForecastModel);

public static class AbcClassPolicyDefaults
{
    private static readonly IReadOnlyDictionary<AbcClass, AbcPolicyDefault> Map =
        new Dictionary<AbcClass, AbcPolicyDefault>
        {
            [AbcClass.A] = new(0.98m, LotSizingPolicy.EconomicOrderQuantity, ForecastModel.HoltWinters),
            [AbcClass.B] = new(0.95m, LotSizingPolicy.EconomicOrderQuantity, ForecastModel.ExponentialSmoothing),
            [AbcClass.C] = new(0.90m, LotSizingPolicy.MinMax, ForecastModel.MovingAverage),
            [AbcClass.Unclassified] = new(0m, LotSizingPolicy.MinMax, ForecastModel.ExponentialSmoothing),
        };

    public static AbcPolicyDefault For(AbcClass abcClass) =>
        Map.TryGetValue(abcClass, out var policy) ? policy : Map[AbcClass.Unclassified];
}
