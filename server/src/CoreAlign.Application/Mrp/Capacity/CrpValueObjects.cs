namespace CoreAlign.Application.Mrp.Capacity;

public sealed record CrpWorkCenterSnapshot(
    Guid WorkCenterId,
    string Code,
    string Name,
    decimal DailyCapacityMinutes);

public sealed record CrpProductionLoad(
    Guid ProductId,
    Guid? WorkCenterId,
    decimal RunTimeMinutesPerUnit,
    decimal Quantity,
    int BucketIndex);

public sealed record CrpInput(
    IReadOnlyList<CrpProductionLoad> ProductionLoads,
    IReadOnlyList<CrpWorkCenterSnapshot> WorkCenters,
    int BucketCount,
    int DaysPerBucket);

public sealed record CrpBucketLoad(
    int BucketIndex,
    decimal LoadMinutes,
    decimal CapacityMinutes,
    bool IsOverloaded);

public sealed record CrpWorkCenterLoad(
    Guid WorkCenterId,
    string Code,
    string Name,
    decimal DailyCapacityMinutes,
    IReadOnlyList<CrpBucketLoad> Buckets);

public sealed record CrpResult(
    IReadOnlyList<CrpWorkCenterLoad> WorkCenters,
    int UnroutedProductionOrderCount);
