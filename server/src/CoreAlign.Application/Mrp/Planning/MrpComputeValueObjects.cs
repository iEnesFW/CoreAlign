using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Mrp.Planning;

public sealed record MrpBucket(
    DateTime StartUtc,
    decimal GrossRequirements,
    decimal ScheduledReceipts,
    decimal ProjectedOnHand,
    decimal NetRequirements,
    decimal PlannedReceipts,
    decimal PlannedReleases);

public sealed record PlannedOrderDraft(
    Guid ProductId,
    int LowLevelCode,
    decimal Quantity,
    DateTime DueDateUtc,
    DateTime ReleaseDateUtc,
    Guid? PreferredSupplierId,
    decimal EstimatedUnitCost,
    LotSizingPolicy SourcePolicy,
    Guid? PeggingParentProductId = null,
    Guid? PeggingSourceOrderLineId = null);

public sealed record ProductionOrderDraft(
    Guid ProductId,
    int LowLevelCode,
    decimal Quantity,
    DateTime DueDateUtc,
    DateTime ReleaseDateUtc,
    decimal EstimatedUnitCost,
    LotSizingPolicy SourcePolicy,
    Guid? PeggingParentProductId,
    Guid? PeggingSourceOrderLineId);

public sealed record ActionMessageDraft(
    Guid ProductId,
    MrpActionType ActionType,
    MrpActionSeverity Severity,
    decimal Quantity,
    DateTime? CurrentDateUtc,
    DateTime? SuggestedDateUtc,
    Guid? RelatedPurchaseOrderId,
    int DaysUntilStockOut,
    string Message);

public sealed record PeggingDraft(
    Guid ComponentProductId,
    decimal RequirementQuantity,
    DateTime DueDateUtc,
    string SourceKind,
    Guid? SourceParentProductId,
    Guid? SourceOrderLineId);

public sealed record MrpItemPlan(
    Guid ProductId,
    string Sku,
    string Name,
    int LowLevelCode,
    decimal OnHand,
    decimal SafetyStock,
    LotSizingPolicy Policy,
    ProcurementType ProcurementType,
    IReadOnlyList<MrpBucket> Buckets,
    IReadOnlyList<PlannedOrderDraft> PlannedOrders,
    IReadOnlyList<ProductionOrderDraft> ProductionOrders,
    IReadOnlyList<ActionMessageDraft> Actions,
    IReadOnlyList<PeggingDraft> Pegs,
    decimal Reserved,
    decimal ReorderPoint,
    Guid? PreferredSupplierId,
    int LeadTimeDays,
    AbcClass AbcClass);

public sealed record MrpPlanResult(
    DateTime AsOfUtc,
    MrpBucketKind BucketKind,
    int HorizonDays,
    int ProductsEvaluated,
    IReadOnlyList<MrpItemPlan> Items);

public enum OrderSinkKind
{
    PurchaseRequisition = 0,
    ProductionOrder = 1
}

public sealed record ChangeImpactSupplyOrder(
    Guid ProductId,
    string Sku,
    int LowLevelCode,
    OrderSinkKind SinkKind,
    decimal Quantity,
    DateTime DueDateUtc,
    DateTime ReleaseDateUtc,
    Guid? DirectParentProductId);

public sealed record ChangeImpactResult(
    Guid SourceOrderLineId,
    Guid RootProductId,
    IReadOnlyList<ChangeImpactSupplyOrder> DownstreamSupply);
