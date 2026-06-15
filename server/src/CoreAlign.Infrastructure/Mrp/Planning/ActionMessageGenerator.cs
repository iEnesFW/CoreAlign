using CoreAlign.Application.Mrp.Planning;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Infrastructure.Mrp.Planning;

public sealed record ScheduledReceiptBucket(
    Guid PurchaseOrderId,
    decimal Quantity,
    DateTime ExpectedDateUtc,
    int BucketIndex);

public sealed record ActionGenerationContext(
    MrpProductSnapshot Product,
    DateTime AsOfUtc,
    IReadOnlyList<MrpBucket> Buckets,
    IReadOnlyList<DateTime> BucketStartsUtc,
    IReadOnlyList<PlannedOrderDraft> PlannedOrders,
    IReadOnlyList<ScheduledReceiptBucket> ScheduledReceipts,
    decimal EffectiveSafetyStock,
    decimal AverageDailyDemand);

public interface IActionMessageGenerator
{
    IReadOnlyList<ActionMessageDraft> Generate(ActionGenerationContext context);
}

public sealed class ActionMessageGenerator : IActionMessageGenerator
{
    public IReadOnlyList<ActionMessageDraft> Generate(ActionGenerationContext context)
    {
        var messages = new List<ActionMessageDraft>();
        var daysUntilStockOut = ComputeDaysUntilStockOut(context);

        AddReleaseAndExpedite(context, daysUntilStockOut, messages);
        AddProjectionExceptions(context, daysUntilStockOut, messages);
        AddRescheduleAndCancel(context, daysUntilStockOut, messages);

        return messages;
    }

    private static void AddReleaseAndExpedite(
        ActionGenerationContext context,
        int daysUntilStockOut,
        List<ActionMessageDraft> messages)
    {
        foreach (var planned in context.PlannedOrders)
        {
            if (planned.ReleaseDateUtc < context.AsOfUtc)
            {
                messages.Add(new ActionMessageDraft(
                    context.Product.ProductId,
                    MrpActionType.Expedite,
                    MrpActionSeverity.Critical,
                    planned.Quantity,
                    planned.ReleaseDateUtc,
                    context.AsOfUtc,
                    null,
                    daysUntilStockOut,
                    $"Expedite {planned.Quantity:0.####} {context.Product.Sku}: release date {planned.ReleaseDateUtc:yyyy-MM-dd} is inside lead time."));
            }
            else
            {
                messages.Add(new ActionMessageDraft(
                    context.Product.ProductId,
                    MrpActionType.Release,
                    MrpActionSeverity.Warning,
                    planned.Quantity,
                    planned.ReleaseDateUtc,
                    planned.DueDateUtc,
                    null,
                    daysUntilStockOut,
                    $"Release {planned.Quantity:0.####} {context.Product.Sku} by {planned.ReleaseDateUtc:yyyy-MM-dd} to cover demand on {planned.DueDateUtc:yyyy-MM-dd}."));
            }
        }
    }

    private static void AddProjectionExceptions(
        ActionGenerationContext context,
        int daysUntilStockOut,
        List<ActionMessageDraft> messages)
    {
        var stockoutRaised = false;
        var belowSafetyRaised = false;

        for (var t = 0; t < context.Buckets.Count; t++)
        {
            var bucket = context.Buckets[t];
            if (!stockoutRaised && bucket.ProjectedOnHand < 0m)
            {
                stockoutRaised = true;
                messages.Add(new ActionMessageDraft(
                    context.Product.ProductId,
                    MrpActionType.ProjectedStockout,
                    MrpActionSeverity.Critical,
                    Math.Abs(bucket.ProjectedOnHand),
                    null,
                    bucket.StartUtc,
                    null,
                    daysUntilStockOut,
                    $"Projected stockout for {context.Product.Sku} on {bucket.StartUtc:yyyy-MM-dd} (shortfall {Math.Abs(bucket.ProjectedOnHand):0.####})."));
            }
            else if (!belowSafetyRaised && context.EffectiveSafetyStock > 0m && bucket.ProjectedOnHand < context.EffectiveSafetyStock && bucket.ProjectedOnHand >= 0m)
            {
                belowSafetyRaised = true;
                messages.Add(new ActionMessageDraft(
                    context.Product.ProductId,
                    MrpActionType.BelowSafetyStock,
                    MrpActionSeverity.Warning,
                    context.EffectiveSafetyStock - bucket.ProjectedOnHand,
                    null,
                    bucket.StartUtc,
                    null,
                    daysUntilStockOut,
                    $"Projected on-hand for {context.Product.Sku} dips below safety stock on {bucket.StartUtc:yyyy-MM-dd}."));
            }
        }
    }

    private static void AddRescheduleAndCancel(
        ActionGenerationContext context,
        int daysUntilStockOut,
        List<ActionMessageDraft> messages)
    {
        var firstRequirementBucket = FirstRequirementBucket(context);

        foreach (var receipt in context.ScheduledReceipts)
        {
            if (firstRequirementBucket is null)
            {
                messages.Add(new ActionMessageDraft(
                    context.Product.ProductId,
                    MrpActionType.CancelSupply,
                    MrpActionSeverity.Info,
                    receipt.Quantity,
                    receipt.ExpectedDateUtc,
                    null,
                    receipt.PurchaseOrderId,
                    daysUntilStockOut,
                    $"Open PO receipt of {receipt.Quantity:0.####} {context.Product.Sku} is not consumed within the horizon; consider cancelling."));
                continue;
            }

            var neededIndex = firstRequirementBucket.Value;
            if (receipt.BucketIndex > neededIndex)
            {
                messages.Add(new ActionMessageDraft(
                    context.Product.ProductId,
                    MrpActionType.RescheduleIn,
                    MrpActionSeverity.Warning,
                    receipt.Quantity,
                    receipt.ExpectedDateUtc,
                    context.BucketStartsUtc[neededIndex],
                    receipt.PurchaseOrderId,
                    daysUntilStockOut,
                    $"Pull in PO receipt of {receipt.Quantity:0.####} {context.Product.Sku} from {receipt.ExpectedDateUtc:yyyy-MM-dd} to {context.BucketStartsUtc[neededIndex]:yyyy-MM-dd}."));
            }
            else if (receipt.BucketIndex < neededIndex)
            {
                messages.Add(new ActionMessageDraft(
                    context.Product.ProductId,
                    MrpActionType.RescheduleOut,
                    MrpActionSeverity.Info,
                    receipt.Quantity,
                    receipt.ExpectedDateUtc,
                    context.BucketStartsUtc[neededIndex],
                    receipt.PurchaseOrderId,
                    daysUntilStockOut,
                    $"Push out PO receipt of {receipt.Quantity:0.####} {context.Product.Sku} from {receipt.ExpectedDateUtc:yyyy-MM-dd} to {context.BucketStartsUtc[neededIndex]:yyyy-MM-dd}."));
            }
        }
    }

    private static int? FirstRequirementBucket(ActionGenerationContext context)
    {
        for (var t = 0; t < context.Buckets.Count; t++)
        {
            if (context.Buckets[t].GrossRequirements > 0m)
            {
                return t;
            }
        }
        return null;
    }

    private static int ComputeDaysUntilStockOut(ActionGenerationContext context)
    {
        for (var t = 0; t < context.Buckets.Count; t++)
        {
            if (context.Buckets[t].ProjectedOnHand < 0m)
            {
                return Math.Max(0, (context.BucketStartsUtc[t].Date - context.AsOfUtc.Date).Days);
            }
        }
        return int.MaxValue;
    }
}
