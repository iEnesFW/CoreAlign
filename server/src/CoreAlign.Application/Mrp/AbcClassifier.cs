using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Mrp;

public readonly record struct AbcUsageInput(Guid ProductId, decimal AnnualUsageValue);

public readonly record struct AbcClassificationResult(Guid ProductId, AbcClass AbcClass);

/// <summary>
/// Pure, DbContext-free ABC ranking. Ranks items by descending annual usage value and
/// assigns classes from cumulative value share: A covers up to <see cref="AThresholdShare"/>
/// (80%) of total value, B up to <see cref="BThresholdShare"/> (95%), the remainder is C.
/// Items with zero (or negative) usage are always C — they hold no value share and would
/// otherwise distort the boundary the moment any positive-value item exists.
/// </summary>
public static class AbcClassifier
{
    public const decimal AThresholdShare = 0.80m;
    public const decimal BThresholdShare = 0.95m;

    public static IReadOnlyList<AbcClassificationResult> Classify(IEnumerable<AbcUsageInput> items)
    {
        var ordered = items
            .OrderByDescending(i => i.AnnualUsageValue)
            .ThenBy(i => i.ProductId)
            .ToList();

        var results = new List<AbcClassificationResult>(ordered.Count);
        var totalValue = ordered.Sum(i => i.AnnualUsageValue > 0m ? i.AnnualUsageValue : 0m);

        if (totalValue <= 0m)
        {
            foreach (var item in ordered)
            {
                results.Add(new AbcClassificationResult(item.ProductId, AbcClass.C));
            }
            return results;
        }

        var cumulativeBefore = 0m;
        foreach (var item in ordered)
        {
            if (item.AnnualUsageValue <= 0m)
            {
                results.Add(new AbcClassificationResult(item.ProductId, AbcClass.C));
                continue;
            }

            var startShare = cumulativeBefore / totalValue;
            var abcClass = startShare < AThresholdShare
                ? AbcClass.A
                : startShare < BThresholdShare
                    ? AbcClass.B
                    : AbcClass.C;
            results.Add(new AbcClassificationResult(item.ProductId, abcClass));
            cumulativeBefore += item.AnnualUsageValue;
        }

        return results;
    }
}
