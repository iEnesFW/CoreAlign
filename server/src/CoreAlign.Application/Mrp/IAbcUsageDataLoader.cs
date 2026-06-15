using CoreAlign.Domain.Entities;

namespace CoreAlign.Application.Mrp;

public sealed record AbcProductUsage(Product Product, decimal AnnualUsageValue);

public interface IAbcUsageDataLoader
{
    /// <summary>
    /// Loads active, stock-tracked products for the current tenant together with their
    /// annual usage value: avgDailyDemand * unitCost * 365, where avgDailyDemand is the
    /// shipped order-line quantity over the trailing 365 days divided by 365, and unitCost
    /// is LastPurchaseCost (when positive) else StandardCost. Returns TRACKED Product
    /// entities so callers can mutate and persist them within the request transaction.
    /// </summary>
    Task<IReadOnlyList<AbcProductUsage>> LoadAsync(DateTime asOfUtc, CancellationToken cancellationToken = default);
}
