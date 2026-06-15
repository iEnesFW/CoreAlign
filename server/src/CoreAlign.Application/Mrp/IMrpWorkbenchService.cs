using CoreAlign.Domain.Entities.Manufacturing;
using CoreAlign.Domain.Entities.Mrp;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Mrp;

public interface IMrpWorkbenchService
{
    Task<MrpPlanRun> CommitAsync(
        DateTime asOfUtc,
        MrpBucketKind kind,
        int horizonDays,
        Guid operationId,
        MrpPlanningMode mode = MrpPlanningMode.Regenerative,
        CancellationToken cancellationToken = default);

    Task<ReleaseResult> ReleaseAsync(
        Guid planRunId,
        IReadOnlyList<Guid> plannedOrderIds,
        Guid operationId,
        CancellationToken cancellationToken = default);

    Task<PlannedProductionOrder> FirmProductionOrderAsync(
        Guid plannedProductionOrderId,
        decimal? overrideQuantity,
        DateTime? overrideDueDateUtc,
        Guid operationId,
        CancellationToken cancellationToken = default);

    Task<PlannedProductionOrder> ReleaseProductionOrderAsync(
        Guid plannedProductionOrderId,
        Guid operationId,
        CancellationToken cancellationToken = default);

    Task<CompleteProductionOrderResult> CompleteProductionOrderAsync(
        Guid plannedProductionOrderId,
        Guid operationId,
        Guid? warehouseId,
        CancellationToken cancellationToken = default);
}

public record CompleteProductionOrderResult(
    PlannedProductionOrder Order,
    Guid WarehouseId,
    decimal ProducedQuantity,
    int ComponentsIssued,
    decimal UnitCost,
    decimal TotalCost,
    bool AlreadyCompleted);
