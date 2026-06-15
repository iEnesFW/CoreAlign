using CoreAlign.Application.Mrp.Planning;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Tests.Mrp.Planning;

internal static class MrpPlanningTestData
{
    public static readonly DateTime AsOf = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static MrpProductSnapshot Product(
        Guid id,
        string sku,
        decimal onHand = 0m,
        decimal reserved = 0m,
        decimal safetyStock = 0m,
        decimal reorderPoint = 0m,
        decimal minStock = 0m,
        decimal maxStock = 0m,
        int leadTimeDays = 0,
        decimal? minOrderQuantity = null,
        LotSizingPolicy policy = LotSizingPolicy.LotForLot,
        decimal fixedOrderQuantity = 0m,
        decimal orderMultiple = 0m,
        decimal eoqAnnualDemand = 0m,
        decimal orderingCost = 0m,
        decimal holdingCostRate = 0m,
        decimal serviceLevelTarget = 0m,
        decimal unitCost = 0m,
        Guid? preferredSupplierId = null,
        ProcurementType procurementType = ProcurementType.Buy,
        AbcClass abcClass = AbcClass.Unclassified,
        ForecastModel forecastModel = ForecastModel.ExponentialSmoothing) =>
        new(
            id,
            sku,
            sku,
            onHand,
            reserved,
            safetyStock,
            reorderPoint,
            minStock,
            maxStock,
            leadTimeDays,
            minOrderQuantity,
            policy,
            fixedOrderQuantity,
            orderMultiple,
            eoqAnnualDemand,
            orderingCost,
            holdingCostRate,
            serviceLevelTarget,
            unitCost,
            preferredSupplierId,
            procurementType,
            abcClass,
            forecastModel);

    public static MrpPlanningSnapshot Snapshot(
        IReadOnlyList<MrpProductSnapshot> products,
        IReadOnlyList<BomEdgeSnapshot>? edges = null,
        IReadOnlyList<IndependentDemandSnapshot>? demand = null,
        IReadOnlyList<ScheduledReceiptSnapshot>? receipts = null,
        IReadOnlyList<DemandHistoryPointSnapshot>? history = null,
        DateTime? asOf = null,
        IReadOnlyList<FirmedSupplySnapshot>? firmedSupply = null) =>
        new(
            asOf ?? AsOf,
            products,
            edges ?? new List<BomEdgeSnapshot>(),
            demand ?? new List<IndependentDemandSnapshot>(),
            receipts ?? new List<ScheduledReceiptSnapshot>(),
            history ?? new List<DemandHistoryPointSnapshot>(),
            90,
            firmedSupply);
}
