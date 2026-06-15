using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Mrp.Planning;

public interface IMrpPlanningEngine
{
    MrpPlanResult Run(MrpPlanningSnapshot snapshot, MrpBucketKind bucketKind, int horizonDays);
}
