using CoreAlign.Application.Common;
using CoreAlign.Domain.Enums;
using MediatR;

namespace CoreAlign.Application.Mrp;

public record MrpBucketDto(
    DateTime StartUtc,
    decimal GrossRequirements,
    decimal ScheduledReceipts,
    decimal ProjectedOnHand,
    decimal NetRequirements,
    decimal PlannedReceipts,
    decimal PlannedReleases);

public record MrpPlannedOrderDraftDto(
    Guid ProductId,
    int LowLevelCode,
    decimal Quantity,
    DateTime DueDateUtc,
    DateTime ReleaseDateUtc,
    Guid? PreferredSupplierId,
    decimal EstimatedUnitCost,
    LotSizingPolicy SourcePolicy,
    ProcurementType ProcurementType,
    Guid? Id = null,
    bool IsFirmed = false,
    bool IsReleased = false,
    Guid? ConvertedRequisitionId = null);

public record MrpProductionOrderDraftDto(
    Guid ProductId,
    int LowLevelCode,
    decimal Quantity,
    DateTime DueDateUtc,
    DateTime ReleaseDateUtc,
    decimal EstimatedUnitCost,
    LotSizingPolicy SourcePolicy,
    Guid? PeggingParentProductId,
    Guid? PeggingSourceOrderLineId,
    Guid? Id = null,
    PlannedProductionOrderStatus? Status = null);

public record MrpActionMessageDraftDto(
    Guid ProductId,
    MrpActionType ActionType,
    MrpActionSeverity Severity,
    decimal Quantity,
    DateTime? CurrentDateUtc,
    DateTime? SuggestedDateUtc,
    Guid? RelatedPurchaseOrderId,
    int DaysUntilStockOut,
    string Message);

public record MrpPeggingDraftDto(
    Guid ComponentProductId,
    decimal RequirementQuantity,
    DateTime DueDateUtc,
    string SourceKind,
    Guid? SourceParentProductId,
    Guid? SourceOrderLineId);

public record MrpItemPlanDto(
    Guid ProductId,
    string Sku,
    string Name,
    int LowLevelCode,
    decimal OnHand,
    decimal SafetyStock,
    LotSizingPolicy Policy,
    ProcurementType ProcurementType,
    IReadOnlyList<MrpBucketDto> Buckets,
    IReadOnlyList<MrpPlannedOrderDraftDto> PlannedOrders,
    IReadOnlyList<MrpProductionOrderDraftDto> ProductionOrders,
    IReadOnlyList<MrpActionMessageDraftDto> Actions,
    IReadOnlyList<MrpPeggingDraftDto> Pegs,
    decimal Reserved,
    decimal ReorderPoint,
    Guid? PreferredSupplierId,
    int LeadTimeDays,
    AbcClass AbcClass);

public record MrpPlanResultDto(
    Guid? PlanRunId,
    MrpPlanRunStatus Status,
    DateTime AsOfUtc,
    MrpBucketKind BucketKind,
    int HorizonDays,
    int ProductsEvaluated,
    int PlannedOrderCount,
    int ActionMessageCount,
    int MakeOrderCount,
    int BuyOrderCount,
    int StockoutRiskCount,
    int ProjectedStockoutCount,
    int ExcessSupplyCount,
    int OnOrderCount,
    IReadOnlyList<MrpItemPlanDto> Items);

public record MrpPlanRunDto(
    Guid Id,
    string Number,
    MrpPlanRunStatus Status,
    DateTime AsOfDateUtc,
    MrpBucketKind BucketKind,
    int HorizonDays,
    string IdempotencyKey,
    int ProductsEvaluated,
    int PlannedOrderCount,
    int ActionMessageCount,
    Guid CreatedByUserId,
    DateTime? CommittedAtUtc,
    DateTime CreatedAtUtc,
    long ConcurrencyToken);

public record MrpPlannedOrderDto(
    Guid Id,
    Guid PlanRunId,
    Guid ProductId,
    int LowLevelCode,
    decimal Quantity,
    DateTime DueDateUtc,
    DateTime ReleaseDateUtc,
    Guid? PreferredSupplierId,
    decimal EstimatedUnitCost,
    LotSizingPolicy SourcePolicy,
    bool IsFirmed,
    bool IsReleased,
    Guid? ConvertedRequisitionId,
    decimal? OriginalQuantity,
    DateTime? OriginalDueDateUtc,
    bool IsQuantityOverridden,
    bool IsDueDateOverridden);

public record MrpActionMessageDto(
    Guid Id,
    Guid PlanRunId,
    Guid ProductId,
    MrpActionType ActionType,
    MrpActionSeverity Severity,
    decimal Quantity,
    DateTime? CurrentDateUtc,
    DateTime? SuggestedDateUtc,
    Guid? RelatedPurchaseOrderId,
    Guid? RelatedPlannedOrderId,
    int DaysUntilStockOut,
    string Message,
    bool IsDismissed,
    DateTime? DismissedAtUtc);

public record MrpPeggingDto(
    Guid Id,
    Guid PlanRunId,
    Guid ComponentProductId,
    decimal RequirementQuantity,
    DateTime DueDateUtc,
    string SourceKind,
    Guid? SourceParentProductId,
    Guid? SourceOrderLineId);

public record ReleasePlannedOrdersResultDto(
    Guid PlanRunId,
    IReadOnlyList<Guid> RequisitionIds,
    int PlannedOrdersReleased);

public record RunMrpPreviewQuery(
    DateTime? AsOfDateUtc = null,
    MrpBucketKind BucketKind = MrpBucketKind.Day,
    int HorizonDays = 60) : IRequest<MrpPlanResultDto>;

public record GetMrpItemPlanQuery(
    Guid ProductId,
    DateTime? AsOfDateUtc = null,
    MrpBucketKind BucketKind = MrpBucketKind.Day,
    int HorizonDays = 60) : IRequest<MrpItemPlanDto?>;

public record ListMrpActionMessagesQuery(
    Guid? PlanRunId = null,
    MrpActionType? ActionType = null,
    MrpActionSeverity? Severity = null,
    Guid? SupplierId = null,
    bool IncludeDismissed = false,
    int Page = 1,
    int PageSize = 25) : IRequest<PagedResult<MrpActionMessageDto>>;

public record GetMrpPeggingQuery(
    Guid PlanRunId,
    Guid ComponentProductId) : IRequest<IReadOnlyList<MrpPeggingDto>>;

public record ListMrpPlanRunsQuery(
    int Page = 1,
    int PageSize = 25) : IRequest<PagedResult<MrpPlanRunDto>>;

public record CommitMrpPlanCommand(
    Guid OperationId,
    DateTime? AsOfDateUtc = null,
    MrpBucketKind BucketKind = MrpBucketKind.Day,
    int HorizonDays = 60,
    MrpPlanningMode Mode = MrpPlanningMode.Regenerative) : IRequest<MrpPlanRunDto>, ITransactionalRequest;

public record ReleasePlannedOrdersCommand(
    Guid PlanRunId,
    IReadOnlyList<Guid> PlannedOrderIds,
    Guid OperationId) : IRequest<ReleasePlannedOrdersResultDto>, ITransactionalRequest;

public record FirmMrpPlannedOrderCommand(
    Guid PlannedOrderId,
    Guid OperationId,
    decimal? OverrideQuantity = null,
    DateTime? OverrideDueDateUtc = null) : IRequest<MrpPlannedOrderDto>, ITransactionalRequest;

public record DismissMrpActionMessageCommand(
    Guid ActionMessageId) : IRequest<Unit>, ITransactionalRequest;
