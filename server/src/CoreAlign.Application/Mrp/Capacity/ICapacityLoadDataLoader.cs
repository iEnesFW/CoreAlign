using CoreAlign.Application.Mrp.Planning;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Mrp.Capacity;

public sealed record CapacityLoadContext(
    CrpInput Input,
    IReadOnlyList<DateTime> BucketStarts);

public interface ICapacityLoadDataLoader
{
    Task<CapacityLoadContext> BuildAsync(
        MrpPlanResult plan,
        DateTime asOfUtc,
        MrpBucketKind bucketKind,
        int horizonDays,
        CancellationToken cancellationToken = default);
}
