namespace CoreAlign.Application.Mrp.Capacity;

/// <summary>
/// Rough-Cut Capacity Planning (RCCP) — infinite-capacity LOAD vs CAPACITY.
/// Pure, deterministic transform: no DbContext, no clock, no I/O. For each work
/// center it sums production-order load (quantity * run-time-minutes-per-unit) into
/// the order's due-date bucket and compares it against bucket capacity
/// (daily-capacity-minutes * days-in-bucket). Production loads whose product has no
/// (matching active) work center are counted as unrouted and excluded entirely.
/// </summary>
public sealed class CrpCalculator : ICrpCalculator
{
    public CrpResult Compute(CrpInput input)
    {
        var bucketCount = input.BucketCount < 1 ? 1 : input.BucketCount;
        var daysPerBucket = input.DaysPerBucket < 1 ? 1 : input.DaysPerBucket;

        var workCenterIds = input.WorkCenters
            .Select(w => w.WorkCenterId)
            .ToHashSet();

        var loadByCenterBucket = new Dictionary<(Guid WorkCenterId, int BucketIndex), decimal>();
        var unroutedCount = 0;

        foreach (var load in input.ProductionLoads)
        {
            if (load.WorkCenterId is not { } workCenterId || !workCenterIds.Contains(workCenterId))
            {
                unroutedCount++;
                continue;
            }

            var bucketIndex = ClampBucket(load.BucketIndex, bucketCount);
            var minutes = load.Quantity * load.RunTimeMinutesPerUnit;
            var key = (workCenterId, bucketIndex);
            loadByCenterBucket[key] = loadByCenterBucket.GetValueOrDefault(key) + minutes;
        }

        var workCenters = input.WorkCenters
            .OrderBy(w => w.Code, StringComparer.Ordinal)
            .ThenBy(w => w.WorkCenterId)
            .Select(w => BuildWorkCenterLoad(w, loadByCenterBucket, bucketCount, daysPerBucket))
            .ToList();

        return new CrpResult(workCenters, unroutedCount);
    }

    private static CrpWorkCenterLoad BuildWorkCenterLoad(
        CrpWorkCenterSnapshot workCenter,
        IReadOnlyDictionary<(Guid WorkCenterId, int BucketIndex), decimal> loadByCenterBucket,
        int bucketCount,
        int daysPerBucket)
    {
        var capacityPerBucket = workCenter.DailyCapacityMinutes * daysPerBucket;

        var buckets = new List<CrpBucketLoad>(bucketCount);
        for (var bucketIndex = 0; bucketIndex < bucketCount; bucketIndex++)
        {
            var loadMinutes = loadByCenterBucket.GetValueOrDefault((workCenter.WorkCenterId, bucketIndex));
            buckets.Add(new CrpBucketLoad(
                bucketIndex,
                loadMinutes,
                capacityPerBucket,
                loadMinutes > capacityPerBucket));
        }

        return new CrpWorkCenterLoad(
            workCenter.WorkCenterId,
            workCenter.Code,
            workCenter.Name,
            workCenter.DailyCapacityMinutes,
            buckets);
    }

    private static int ClampBucket(int bucketIndex, int bucketCount)
    {
        if (bucketIndex < 0)
        {
            return 0;
        }
        return bucketIndex >= bucketCount ? bucketCount - 1 : bucketIndex;
    }
}
