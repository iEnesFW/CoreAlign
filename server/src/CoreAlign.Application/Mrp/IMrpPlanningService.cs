using CoreAlign.Application.Mrp.Planning;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Mrp;

public sealed record ReleaseResult(
    Guid PlanRunId,
    IReadOnlyList<Guid> RequisitionIds,
    int PlannedOrdersReleased);

public interface IMrpPlanningService
{
    Task<MrpPlanResult> RunPreviewAsync(DateTime asOfUtc, MrpBucketKind kind, int horizonDays, CancellationToken cancellationToken = default);

    Task<MrpItemPlan?> GetItemPlanAsync(Guid productId, DateTime asOfUtc, MrpBucketKind kind, int horizonDays, CancellationToken cancellationToken = default);
}
