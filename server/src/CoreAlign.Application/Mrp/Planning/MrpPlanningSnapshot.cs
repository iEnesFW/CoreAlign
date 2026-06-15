using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Mrp.Planning;

public sealed record MrpProductSnapshot(
    Guid ProductId,
    string Sku,
    string Name,
    decimal OnHand,
    decimal Reserved,
    decimal SafetyStock,
    decimal ReorderPoint,
    decimal MinStock,
    decimal MaxStock,
    int LeadTimeDays,
    decimal? MinOrderQuantity,
    LotSizingPolicy LotSizingPolicy,
    decimal FixedOrderQuantity,
    decimal OrderMultiple,
    decimal EoqAnnualDemand,
    decimal OrderingCost,
    decimal HoldingCostRate,
    decimal ServiceLevelTarget,
    decimal UnitCost,
    Guid? PreferredSupplierId,
    ProcurementType ProcurementType,
    AbcClass AbcClass = AbcClass.Unclassified,
    ForecastModel ForecastModel = ForecastModel.ExponentialSmoothing);

public sealed record BomEdgeSnapshot(
    Guid ParentProductId,
    Guid ComponentProductId,
    decimal QuantityPer);

public sealed record IndependentDemandSnapshot(
    Guid ProductId,
    decimal Quantity,
    DateTime DueDateUtc,
    Guid? OrderLineId);

public sealed record ScheduledReceiptSnapshot(
    Guid ProductId,
    decimal Quantity,
    DateTime ExpectedDateUtc,
    Guid PurchaseOrderId);

public sealed record FirmedSupplySnapshot(
    Guid ProductId,
    decimal Quantity,
    DateTime DueDateUtc,
    DateTime ReleaseDateUtc,
    ProcurementType ProcurementType,
    Guid SourceId);

public sealed record DemandHistoryPointSnapshot(
    Guid ProductId,
    DateTime DayUtc,
    decimal Quantity);

public sealed record MrpPlanningSnapshot(
    DateTime AsOfUtc,
    IReadOnlyList<MrpProductSnapshot> Products,
    IReadOnlyList<BomEdgeSnapshot> BomEdges,
    IReadOnlyList<IndependentDemandSnapshot> IndependentDemand,
    IReadOnlyList<ScheduledReceiptSnapshot> ScheduledReceipts,
    IReadOnlyList<DemandHistoryPointSnapshot> DemandHistory,
    int DemandHistoryWindowDays,
    IReadOnlyList<FirmedSupplySnapshot>? FirmedSupply = null);
