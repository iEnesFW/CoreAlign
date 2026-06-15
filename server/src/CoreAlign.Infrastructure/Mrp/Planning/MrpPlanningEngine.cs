using CoreAlign.Application.Mrp.Planning;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Infrastructure.Mrp.Planning;

public sealed class MrpPlanningEngine : IMrpPlanningEngine
{
    private readonly ILotSizingCalculator _lotSizing;
    private readonly IDemandForecaster _forecaster;
    private readonly IActionMessageGenerator _actionGenerator;

    public MrpPlanningEngine(
        ILotSizingCalculator lotSizing,
        IDemandForecaster forecaster,
        IActionMessageGenerator actionGenerator)
    {
        _lotSizing = lotSizing;
        _forecaster = forecaster;
        _actionGenerator = actionGenerator;
    }

    public MrpPlanResult Run(MrpPlanningSnapshot snapshot, MrpBucketKind bucketKind, int horizonDays)
    {
        var asOfUtc = DateTime.SpecifyKind(snapshot.AsOfUtc, DateTimeKind.Utc);
        var calendar = new BucketCalendar(asOfUtc, bucketKind, horizonDays);

        var productById = snapshot.Products.ToDictionary(p => p.ProductId);
        var productIds = snapshot.Products.Select(p => p.ProductId).ToList();
        var lowLevelCodes = LowLevelCoder.Assign(productIds, snapshot.BomEdges);
        var childEdgesByParent = snapshot.BomEdges
            .GroupBy(e => e.ParentProductId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var historyByProduct = snapshot.DemandHistory
            .GroupBy(h => h.ProductId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<DemandHistoryPointSnapshot>)g.ToList());

        var (grossByProduct, actualDemandByProduct) = InitializeGrossRequirements(snapshot, calendar, productById);
        var scheduledByProduct = BucketScheduledReceipts(snapshot, calendar, productById);
        var (firmedReceiptsByProduct, firmedReleasesByProduct) = BucketFirmedSupply(snapshot, calendar, productById);
        var peggingByProduct = InitializePegging(snapshot, calendar);

        var planningOrder = productIds
            .OrderBy(id => lowLevelCodes.TryGetValue(id, out var code) ? code : 0)
            .ToList();

        var items = new List<MrpItemPlan>(planningOrder.Count);
        foreach (var productId in planningOrder)
        {
            var product = productById[productId];
            var lowLevelCode = lowLevelCodes.TryGetValue(productId, out var code) ? code : 0;
            var gross = grossByProduct[productId];
            var scheduled = scheduledByProduct.TryGetValue(productId, out var sched)
                ? sched
                : Array.Empty<ScheduledReceiptBucket>();
            var firmedReceipts = firmedReceiptsByProduct.TryGetValue(productId, out var fr)
                ? fr
                : new decimal[calendar.Count];
            var firmedReleases = firmedReleasesByProduct.TryGetValue(productId, out var frl)
                ? frl
                : new decimal[calendar.Count];

            var forecast = _forecaster.Forecast(
                product,
                historyByProduct.TryGetValue(productId, out var hist) ? hist : new List<DemandHistoryPointSnapshot>(),
                snapshot.DemandHistoryWindowDays,
                product.ForecastModel,
                horizonDays);

            ApplyForecastConsumption(
                gross,
                actualDemandByProduct.TryGetValue(productId, out var actual) ? actual : new decimal[calendar.Count],
                forecast.DailyForecast,
                asOfUtc,
                calendar);

            var pegs = peggingByProduct.TryGetValue(productId, out var existingPegs)
                ? existingPegs
                : new List<PeggingDraft>();

            var itemPlan = PlanItem(
                product,
                lowLevelCode,
                calendar,
                asOfUtc,
                gross,
                scheduled,
                firmedReceipts,
                firmedReleases,
                forecast,
                pegs);

            if (product.ProcurementType == ProcurementType.Make)
            {
                ExplodeToChildren(itemPlan, product, calendar, childEdgesByParent, grossByProduct, peggingByProduct);
            }

            items.Add(itemPlan);
        }

        var ordered = items.OrderBy(i => i.LowLevelCode).ThenBy(i => i.Sku, StringComparer.Ordinal).ToList();
        return new MrpPlanResult(asOfUtc, bucketKind, horizonDays, snapshot.Products.Count, ordered);
    }

    private MrpItemPlan PlanItem(
        MrpProductSnapshot product,
        int lowLevelCode,
        BucketCalendar calendar,
        DateTime asOfUtc,
        decimal[] gross,
        IReadOnlyList<ScheduledReceiptBucket> scheduled,
        decimal[] firmedReceipts,
        decimal[] firmedReleases,
        ForecastResult forecast,
        IReadOnlyList<PeggingDraft> pegs)
    {
        var bucketCount = calendar.Count;
        var scheduledReceipts = new decimal[bucketCount];
        foreach (var receipt in scheduled)
        {
            scheduledReceipts[receipt.BucketIndex] += receipt.Quantity;
        }
        for (var t = 0; t < bucketCount; t++)
        {
            scheduledReceipts[t] += firmedReceipts[t];
        }

        var projectedOnHand = new decimal[bucketCount];
        var netRequirements = new decimal[bucketCount];
        var plannedReceipts = new decimal[bucketCount];
        var plannedReleases = new decimal[bucketCount];
        var effectiveSafetyStock = forecast.SafetyStock;
        var leadTimeOffset = calendar.OffsetBuckets(product.LeadTimeDays);

        var (peggingParentProductId, peggingSourceOrderLineId) = ResolvePeggingParent(pegs);

        var plannedOrders = new List<PlannedOrderDraft>();
        var productionOrders = new List<ProductionOrderDraft>();
        var openingOnHand = product.OnHand;
        var previousOnHand = openingOnHand;

        for (var t = 0; t < bucketCount; t++)
        {
            var beforeReceipt = previousOnHand + scheduledReceipts[t] - gross[t];
            var shortfall = effectiveSafetyStock - beforeReceipt;
            if (shortfall > 0m)
            {
                var upcoming = BuildUpcomingNetRequirements(t, gross, scheduledReceipts, effectiveSafetyStock);
                var lotQty = _lotSizing.Calculate(product, shortfall, beforeReceipt, forecast.AverageDailyDemand, upcoming);
                if (lotQty > 0m)
                {
                    netRequirements[t] = Math.Round(shortfall, 4);
                    plannedReceipts[t] = lotQty;

                    var releaseIndex = t - leadTimeOffset;
                    if (releaseIndex < 0)
                    {
                        releaseIndex = 0;
                    }
                    plannedReleases[releaseIndex] += lotQty;

                    var dueDateUtc = calendar.StartOf(t);
                    var releaseDateUtc = DateTime.SpecifyKind(dueDateUtc.AddDays(-product.LeadTimeDays), DateTimeKind.Utc);
                    var unitCost = product.UnitCost;

                    if (product.ProcurementType == ProcurementType.Make)
                    {
                        productionOrders.Add(new ProductionOrderDraft(
                            product.ProductId,
                            lowLevelCode,
                            lotQty,
                            dueDateUtc,
                            releaseDateUtc,
                            unitCost,
                            product.LotSizingPolicy,
                            peggingParentProductId,
                            peggingSourceOrderLineId));
                    }
                    else
                    {
                        plannedOrders.Add(new PlannedOrderDraft(
                            product.ProductId,
                            lowLevelCode,
                            lotQty,
                            dueDateUtc,
                            releaseDateUtc,
                            product.PreferredSupplierId,
                            unitCost,
                            product.LotSizingPolicy,
                            peggingParentProductId,
                            peggingSourceOrderLineId));
                    }

                    previousOnHand = beforeReceipt + lotQty;
                    projectedOnHand[t] = Math.Round(previousOnHand, 4);
                    continue;
                }
            }

            previousOnHand = beforeReceipt;
            projectedOnHand[t] = Math.Round(previousOnHand, 4);
        }

        for (var t = 0; t < bucketCount; t++)
        {
            plannedReleases[t] += firmedReleases[t];
        }

        var buckets = new List<MrpBucket>(bucketCount);
        for (var t = 0; t < bucketCount; t++)
        {
            buckets.Add(new MrpBucket(
                calendar.StartOf(t),
                Math.Round(gross[t], 4),
                Math.Round(scheduledReceipts[t], 4),
                projectedOnHand[t],
                netRequirements[t],
                Math.Round(plannedReceipts[t], 4),
                Math.Round(plannedReleases[t], 4)));
        }

        var actionContext = new ActionGenerationContext(
            product,
            asOfUtc,
            buckets,
            calendar.Starts,
            plannedOrders,
            scheduled,
            effectiveSafetyStock,
            forecast.AverageDailyDemand);
        var actions = _actionGenerator.Generate(actionContext);

        return new MrpItemPlan(
            product.ProductId,
            product.Sku,
            product.Name,
            lowLevelCode,
            product.OnHand,
            Math.Round(effectiveSafetyStock, 4),
            product.LotSizingPolicy,
            product.ProcurementType,
            buckets,
            plannedOrders,
            productionOrders,
            actions,
            pegs,
            product.Reserved,
            product.ReorderPoint,
            product.PreferredSupplierId,
            product.LeadTimeDays,
            product.AbcClass);
    }

    private static (Guid? ParentProductId, Guid? SourceOrderLineId) ResolvePeggingParent(
        IReadOnlyList<PeggingDraft> pegs)
    {
        if (pegs.Count == 0)
        {
            return (null, null);
        }
        var dominant = pegs
            .OrderByDescending(p => p.RequirementQuantity)
            .First();
        return (dominant.SourceParentProductId, dominant.SourceOrderLineId);
    }

    private static decimal[] BuildUpcomingNetRequirements(
        int fromBucket,
        decimal[] gross,
        decimal[] scheduledReceipts,
        decimal safetyStock)
    {
        var upcoming = new List<decimal>();
        for (var t = fromBucket + 1; t < gross.Length; t++)
        {
            var net = gross[t] - scheduledReceipts[t] + safetyStock;
            upcoming.Add(Math.Max(0m, net));
        }
        return upcoming.ToArray();
    }

    private static void ExplodeToChildren(
        MrpItemPlan parentPlan,
        MrpProductSnapshot parentProduct,
        BucketCalendar calendar,
        IReadOnlyDictionary<Guid, List<BomEdgeSnapshot>> childEdgesByParent,
        IReadOnlyDictionary<Guid, decimal[]> grossByProduct,
        Dictionary<Guid, List<PeggingDraft>> peggingByProduct)
    {
        if (!childEdgesByParent.TryGetValue(parentProduct.ProductId, out var edges))
        {
            return;
        }

        for (var t = 0; t < parentPlan.Buckets.Count; t++)
        {
            var release = parentPlan.Buckets[t].PlannedReleases;
            if (release <= 0m)
            {
                continue;
            }

            var releaseDate = calendar.StartOf(t);
            foreach (var edge in edges)
            {
                if (!grossByProduct.TryGetValue(edge.ComponentProductId, out var childGross))
                {
                    continue;
                }
                var childQty = release * edge.QuantityPer;
                childGross[t] += childQty;

                if (!peggingByProduct.TryGetValue(edge.ComponentProductId, out var pegs))
                {
                    pegs = new List<PeggingDraft>();
                    peggingByProduct[edge.ComponentProductId] = pegs;
                }
                pegs.Add(new PeggingDraft(
                    edge.ComponentProductId,
                    Math.Round(childQty, 4),
                    releaseDate,
                    "ProductionOrder",
                    parentProduct.ProductId,
                    null));
            }
        }
    }

    private static (Dictionary<Guid, decimal[]> Gross, Dictionary<Guid, decimal[]> ActualDemand) InitializeGrossRequirements(
        MrpPlanningSnapshot snapshot,
        BucketCalendar calendar,
        IReadOnlyDictionary<Guid, MrpProductSnapshot> productById)
    {
        var gross = new Dictionary<Guid, decimal[]>(productById.Count);
        var actualDemand = new Dictionary<Guid, decimal[]>(productById.Count);
        foreach (var productId in productById.Keys)
        {
            gross[productId] = new decimal[calendar.Count];
            actualDemand[productId] = new decimal[calendar.Count];
        }

        foreach (var demand in snapshot.IndependentDemand)
        {
            if (!gross.TryGetValue(demand.ProductId, out var buckets))
            {
                continue;
            }
            var index = calendar.IndexFor(demand.DueDateUtc);
            buckets[index] += demand.Quantity;
            actualDemand[demand.ProductId][index] += demand.Quantity;
        }

        return (gross, actualDemand);
    }

    // Standard forecast consumption: for each bucket the demand the engine plans for is
    // max(actual independent demand, forecast). We never add forecast on top of actual —
    // only the UNCONSUMED portion (forecast above what real orders already cover) becomes
    // extra gross. Products with no usable history forecast ~0, so nothing is added.
    private static void ApplyForecastConsumption(
        decimal[] gross,
        decimal[] actualDemand,
        IReadOnlyList<decimal> dailyForecast,
        DateTime asOfUtc,
        BucketCalendar calendar)
    {
        if (dailyForecast.Count == 0)
        {
            return;
        }

        var forecastByBucket = BucketDailyForecast(dailyForecast, asOfUtc, calendar);
        for (var t = 0; t < gross.Length; t++)
        {
            var unconsumed = forecastByBucket[t] - actualDemand[t];
            if (unconsumed > 0m)
            {
                gross[t] += unconsumed;
            }
        }
    }

    private static decimal[] BucketDailyForecast(
        IReadOnlyList<decimal> dailyForecast,
        DateTime asOfUtc,
        BucketCalendar calendar)
    {
        var anchor = DateTime.SpecifyKind(asOfUtc.Date, DateTimeKind.Utc);
        var byBucket = new decimal[calendar.Count];
        for (var day = 0; day < dailyForecast.Count; day++)
        {
            var index = calendar.IndexFor(anchor.AddDays(day));
            byBucket[index] += dailyForecast[day];
        }
        return byBucket;
    }

    private static Dictionary<Guid, List<PeggingDraft>> InitializePegging(
        MrpPlanningSnapshot snapshot,
        BucketCalendar calendar)
    {
        var pegging = new Dictionary<Guid, List<PeggingDraft>>();
        foreach (var demand in snapshot.IndependentDemand)
        {
            if (demand.Quantity <= 0m)
            {
                continue;
            }
            if (!pegging.TryGetValue(demand.ProductId, out var pegs))
            {
                pegs = new List<PeggingDraft>();
                pegging[demand.ProductId] = pegs;
            }
            var index = calendar.IndexFor(demand.DueDateUtc);
            pegs.Add(new PeggingDraft(
                demand.ProductId,
                demand.Quantity,
                calendar.StartOf(index),
                "SalesOrder",
                null,
                demand.OrderLineId));
        }
        return pegging;
    }

    private static (Dictionary<Guid, decimal[]> Receipts, Dictionary<Guid, decimal[]> Releases) BucketFirmedSupply(
        MrpPlanningSnapshot snapshot,
        BucketCalendar calendar,
        IReadOnlyDictionary<Guid, MrpProductSnapshot> productById)
    {
        var receipts = new Dictionary<Guid, decimal[]>();
        var releases = new Dictionary<Guid, decimal[]>();
        if (snapshot.FirmedSupply is null)
        {
            return (receipts, releases);
        }

        foreach (var supply in snapshot.FirmedSupply)
        {
            if (!productById.ContainsKey(supply.ProductId) || supply.Quantity <= 0m)
            {
                continue;
            }

            if (!receipts.TryGetValue(supply.ProductId, out var receiptBuckets))
            {
                receiptBuckets = new decimal[calendar.Count];
                receipts[supply.ProductId] = receiptBuckets;
            }
            receiptBuckets[calendar.IndexFor(supply.DueDateUtc)] += supply.Quantity;

            if (supply.ProcurementType == ProcurementType.Make)
            {
                if (!releases.TryGetValue(supply.ProductId, out var releaseBuckets))
                {
                    releaseBuckets = new decimal[calendar.Count];
                    releases[supply.ProductId] = releaseBuckets;
                }
                releaseBuckets[calendar.IndexFor(supply.ReleaseDateUtc)] += supply.Quantity;
            }
        }

        return (receipts, releases);
    }

    private static Dictionary<Guid, IReadOnlyList<ScheduledReceiptBucket>> BucketScheduledReceipts(
        MrpPlanningSnapshot snapshot,
        BucketCalendar calendar,
        IReadOnlyDictionary<Guid, MrpProductSnapshot> productById)
    {
        var result = new Dictionary<Guid, List<ScheduledReceiptBucket>>();
        foreach (var receipt in snapshot.ScheduledReceipts)
        {
            if (!productById.ContainsKey(receipt.ProductId) || receipt.Quantity <= 0m)
            {
                continue;
            }
            var index = calendar.IndexFor(receipt.ExpectedDateUtc);
            if (!result.TryGetValue(receipt.ProductId, out var list))
            {
                list = new List<ScheduledReceiptBucket>();
                result[receipt.ProductId] = list;
            }
            list.Add(new ScheduledReceiptBucket(receipt.PurchaseOrderId, receipt.Quantity, receipt.ExpectedDateUtc, index));
        }

        return result.ToDictionary(kvp => kvp.Key, kvp => (IReadOnlyList<ScheduledReceiptBucket>)kvp.Value);
    }
}
