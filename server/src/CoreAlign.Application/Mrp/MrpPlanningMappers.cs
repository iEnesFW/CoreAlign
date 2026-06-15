using CoreAlign.Application.Mrp.Planning;
using CoreAlign.Domain.Entities.Manufacturing;
using CoreAlign.Domain.Entities.Mrp;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Mrp;

internal static class MrpPlanningMappers
{
    public static MrpPlanResultDto ToDto(this MrpPlanResult result)
    {
        var items = result.Items.Select(ToDto).ToList();

        var buyOrderCount = items.Sum(i => i.PlannedOrders.Count);
        var makeOrderCount = items.Sum(i => i.ProductionOrders.Count);
        var plannedOrderCount = buyOrderCount + makeOrderCount;
        var actionMessageCount = items.Sum(i => i.Actions.Count);
        var stockoutRiskCount = items.Count(i => i.Buckets.Any(b => b.ProjectedOnHand < i.SafetyStock));
        var projectedStockoutCount = items.Count(i => i.Buckets.Any(b => b.ProjectedOnHand < 0m));
        var excessSupplyCount = items.Count(i => i.Actions.Any(a => a.ActionType == MrpActionType.CancelSupply));
        var onOrderCount = items.Count(i => i.Buckets.Any(b => b.ScheduledReceipts > 0m));

        return new MrpPlanResultDto(
            null,
            MrpPlanRunStatus.Preview,
            result.AsOfUtc,
            result.BucketKind,
            result.HorizonDays,
            result.ProductsEvaluated,
            plannedOrderCount,
            actionMessageCount,
            makeOrderCount,
            buyOrderCount,
            stockoutRiskCount,
            projectedStockoutCount,
            excessSupplyCount,
            onOrderCount,
            items);
    }

    public static MrpItemPlanDto ToDto(this MrpItemPlan item) => new(
        item.ProductId,
        item.Sku,
        item.Name,
        item.LowLevelCode,
        item.OnHand,
        item.SafetyStock,
        item.Policy,
        item.ProcurementType,
        item.Buckets.Select(b => new MrpBucketDto(
            b.StartUtc,
            b.GrossRequirements,
            b.ScheduledReceipts,
            b.ProjectedOnHand,
            b.NetRequirements,
            b.PlannedReceipts,
            b.PlannedReleases)).ToList(),
        item.PlannedOrders.Select(o => new MrpPlannedOrderDraftDto(
            o.ProductId,
            o.LowLevelCode,
            o.Quantity,
            o.DueDateUtc,
            o.ReleaseDateUtc,
            o.PreferredSupplierId,
            o.EstimatedUnitCost,
            o.SourcePolicy,
            ProcurementType.Buy)).ToList(),
        item.ProductionOrders.Select(o => new MrpProductionOrderDraftDto(
            o.ProductId,
            o.LowLevelCode,
            o.Quantity,
            o.DueDateUtc,
            o.ReleaseDateUtc,
            o.EstimatedUnitCost,
            o.SourcePolicy,
            o.PeggingParentProductId,
            o.PeggingSourceOrderLineId)).ToList(),
        item.Actions.Select(a => new MrpActionMessageDraftDto(
            a.ProductId,
            a.ActionType,
            a.Severity,
            a.Quantity,
            a.CurrentDateUtc,
            a.SuggestedDateUtc,
            a.RelatedPurchaseOrderId,
            a.DaysUntilStockOut,
            a.Message)).ToList(),
        item.Pegs.Select(p => new MrpPeggingDraftDto(
            p.ComponentProductId,
            p.RequirementQuantity,
            p.DueDateUtc,
            p.SourceKind,
            p.SourceParentProductId,
            p.SourceOrderLineId)).ToList(),
        item.Reserved,
        item.ReorderPoint,
        item.PreferredSupplierId,
        item.LeadTimeDays,
        item.AbcClass);

    public static MrpPlanRunDto ToDto(this MrpPlanRun run) => new(
        run.Id,
        run.Number,
        run.Status,
        run.AsOfDateUtc,
        run.BucketKind,
        run.HorizonDays,
        run.IdempotencyKey,
        run.ProductsEvaluated,
        run.PlannedOrderCount,
        run.ActionMessageCount,
        run.CreatedByUserId,
        run.CommittedAtUtc,
        run.CreatedAtUtc,
        run.ConcurrencyToken);

    public static MrpPlannedOrderDto ToDto(this MrpPlannedOrder order) => new(
        order.Id,
        order.PlanRunId,
        order.ProductId,
        order.LowLevelCode,
        order.Quantity,
        order.DueDateUtc,
        order.ReleaseDateUtc,
        order.PreferredSupplierId,
        order.EstimatedUnitCost,
        order.SourcePolicy,
        order.IsFirmed,
        order.IsReleased,
        order.ConvertedRequisitionId,
        order.OriginalQuantity,
        order.OriginalDueDateUtc,
        order.IsQuantityOverridden,
        order.IsDueDateOverridden);

    public static MrpActionMessageDto ToDto(this MrpActionMessage message) => new(
        message.Id,
        message.PlanRunId,
        message.ProductId,
        message.ActionType,
        message.Severity,
        message.Quantity,
        message.CurrentDateUtc,
        message.SuggestedDateUtc,
        message.RelatedPurchaseOrderId,
        message.RelatedPlannedOrderId,
        message.DaysUntilStockOut,
        message.Message,
        message.IsDismissed,
        message.DismissedAtUtc);

    public static MrpPeggingDto ToDto(this MrpPegging pegging) => new(
        pegging.Id,
        pegging.PlanRunId,
        pegging.ComponentProductId,
        pegging.RequirementQuantity,
        pegging.DueDateUtc,
        pegging.SourceKind,
        pegging.SourceParentProductId,
        pegging.SourceOrderLineId);

    public static PlannedProductionOrderDto ToDto(this PlannedProductionOrder order) => new(
        order.Id,
        order.SourcePlanRunId,
        order.ProductId,
        order.LowLevelCode,
        order.Quantity,
        order.DueDateUtc,
        order.ReleaseDateUtc,
        order.EstimatedUnitCost,
        order.SourcePolicy,
        order.PeggingParentProductId,
        order.PeggingSourceOrderLineId,
        order.Status,
        order.CreatedAtUtc,
        order.UpdatedAtUtc);
}
