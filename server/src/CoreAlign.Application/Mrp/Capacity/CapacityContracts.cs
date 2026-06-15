using CoreAlign.Domain.Enums;
using MediatR;

namespace CoreAlign.Application.Mrp.Capacity;

public record MrpCapacityBucketLoadDto(
    DateTime StartUtc,
    decimal LoadMinutes,
    decimal CapacityMinutes,
    bool IsOverloaded);

public record MrpCapacityWorkCenterDto(
    Guid WorkCenterId,
    string Code,
    string Name,
    decimal DailyCapacityMinutes,
    IReadOnlyList<MrpCapacityBucketLoadDto> Buckets);

public record MrpCapacityLoadResultDto(
    DateTime AsOfUtc,
    MrpBucketKind BucketKind,
    int HorizonDays,
    IReadOnlyList<DateTime> BucketStarts,
    IReadOnlyList<MrpCapacityWorkCenterDto> WorkCenters,
    int UnroutedProductionOrderCount);

public record GetMrpCapacityLoadQuery(
    DateTime? AsOfDateUtc = null,
    MrpBucketKind BucketKind = MrpBucketKind.Day,
    int HorizonDays = 60) : IRequest<MrpCapacityLoadResultDto>;
