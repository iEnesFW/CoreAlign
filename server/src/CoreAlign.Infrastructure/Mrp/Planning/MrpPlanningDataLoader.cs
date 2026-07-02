using CoreAlign.Application.Mrp;
using CoreAlign.Application.Mrp.Planning;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.Manufacturing;
using CoreAlign.Domain.Entities.Mrp;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Mrp.Planning;

public sealed class MrpPlanningDataLoader : IMrpPlanningDataLoader
{
    // Seasonal models (Holt-Winters) need at least two full seasonal cycles of daily
    // history to fit. A weekly period requires >= 14 days, but a year of data lets the
    // smoother stabilise its level/trend/seasonal components across realistic demand
    // cycles. We load a single 365-day window for every product rather than branching
    // per AbcClass/model: the longer read is cheap (one grouped query) and non-seasonal
    // level models (MA/SES) are unaffected because they collapse the window to a scalar.
    public const int DemandHistoryWindowDays = 365;

    private readonly CoreAlignDbContext _db;
    private readonly IStockItemRepository _stockItems;
    private readonly IProductComponentRepository _components;

    public MrpPlanningDataLoader(
        CoreAlignDbContext db,
        IStockItemRepository stockItems,
        IProductComponentRepository components)
    {
        _db = db;
        _stockItems = stockItems;
        _components = components;
    }

    public async Task<MrpPlanningSnapshot> LoadAsync(DateTime asOfUtc, int horizonDays, CancellationToken cancellationToken = default)
    {
        var anchorUtc = DateTime.SpecifyKind(asOfUtc, DateTimeKind.Utc);

        var products = await _db.Products.AsNoTracking()
            .Where(p => p.IsStockTracked && p.Status == ProductStatus.Active)
            .ToListAsync(cancellationToken);

        var productIds = products.Select(p => p.Id).ToList();
        if (productIds.Count == 0)
        {
            return Empty(anchorUtc);
        }

        var stockMap = await _stockItems.SumOnHandAndReservedByProductsAsync(productIds, warehouseId: null, cancellationToken);

        var supplierIds = products
            .Where(p => p.PreferredSupplierId.HasValue)
            .Select(p => p.PreferredSupplierId!.Value)
            .Distinct()
            .ToList();
        var vendorLeadDays = supplierIds.Count == 0
            ? new Dictionary<Guid, int>()
            : await _db.Vendors.AsNoTracking()
                .Where(v => supplierIds.Contains(v.Id) && v.DefaultLeadTimeDays > 0)
                .Select(v => new { v.Id, v.DefaultLeadTimeDays })
                .ToDictionaryAsync(v => v.Id, v => v.DefaultLeadTimeDays, cancellationToken);
        var bomTree = await _components.GetTreeForProductsAsync(productIds, cancellationToken);

        var bomEdges = bomTree
            .SelectMany(parent => parent.Value.Select(child =>
                new BomEdgeSnapshot(parent.Key, child.ComponentId, child.Quantity)))
            .ToList();

        var productSnapshots = products
            .Select(p =>
            {
                var stock = stockMap.TryGetValue(p.Id, out var s) ? s : (OnHand: 0m, Reserved: 0m);
                var unitCost = p.LastPurchaseCost > 0m ? p.LastPurchaseCost : p.StandardCost;
                var forecastModel = AbcClassPolicyDefaults.For(p.AbcClass).ForecastModel;
                var effectiveLeadTimeDays =
                    p.PreferredSupplierId.HasValue
                    && vendorLeadDays.TryGetValue(p.PreferredSupplierId.Value, out var supplierLead)
                    && supplierLead > 0
                        ? supplierLead
                        : p.LeadTimeDays;
                return new MrpProductSnapshot(
                    p.Id,
                    p.Sku,
                    p.Name,
                    stock.OnHand,
                    stock.Reserved,
                    p.SafetyStock,
                    p.ReorderPoint,
                    p.MinStock,
                    p.MaxStock,
                    effectiveLeadTimeDays,
                    p.MinOrderQuantity,
                    p.LotSizingPolicy,
                    p.FixedOrderQuantity,
                    p.OrderMultiple,
                    p.EoqAnnualDemand,
                    p.OrderingCost,
                    p.HoldingCostRate,
                    p.ServiceLevelTarget,
                    unitCost,
                    p.PreferredSupplierId,
                    p.ProcurementType,
                    p.AbcClass,
                    forecastModel);
            })
            .ToList();

        var independentDemand = await LoadIndependentDemandAsync(productIds, anchorUtc, cancellationToken);
        var scheduledReceipts = await LoadScheduledReceiptsAsync(productIds, anchorUtc, cancellationToken);
        var firmedSupply = await LoadFirmedSupplyAsync(productIds, anchorUtc, cancellationToken);
        var demandHistory = await LoadDemandHistoryAsync(productIds, anchorUtc, cancellationToken);

        return new MrpPlanningSnapshot(
            anchorUtc,
            productSnapshots,
            bomEdges,
            independentDemand,
            scheduledReceipts,
            demandHistory,
            DemandHistoryWindowDays,
            firmedSupply);
    }

    private async Task<IReadOnlyList<FirmedSupplySnapshot>> LoadFirmedSupplyAsync(
        IReadOnlyList<Guid> productIds,
        DateTime anchorUtc,
        CancellationToken cancellationToken)
    {
        // Firmed supply is scoped to the SINGLE latest committed run, not every historical
        // run. Each commit carries firmed-but-unreleased orders forward into the new run
        // (MrpPlanningService.CommitAsync), so the latest run holds the complete live firmed
        // set. Loading across all runs would count a firmed order once per surviving run —
        // silent over-supply that grows with re-plan frequency (T3 review finding #4).
        var latestRunId = await _db.Set<MrpPlanRun>().AsNoTracking()
            .Where(r => r.Status == MrpPlanRunStatus.Committed)
            .OrderByDescending(r => r.AsOfDateUtc)
            .ThenByDescending(r => r.CreatedAtUtc)
            .Select(r => (Guid?)r.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestRunId is null)
        {
            return new List<FirmedSupplySnapshot>();
        }

        var firmedBuy = await _db.Set<MrpPlannedOrder>().AsNoTracking()
            .Where(o => o.PlanRunId == latestRunId && productIds.Contains(o.ProductId) && o.IsFirmed && !o.IsReleased)
            .Select(o => new { o.Id, o.ProductId, o.Quantity, o.DueDateUtc, o.ReleaseDateUtc })
            .ToListAsync(cancellationToken);

        var firmedMake = await _db.Set<PlannedProductionOrder>().AsNoTracking()
            .Where(o => o.SourcePlanRunId == latestRunId && productIds.Contains(o.ProductId) && o.Status == PlannedProductionOrderStatus.Firm)
            .Select(o => new { o.Id, o.ProductId, o.Quantity, o.DueDateUtc, o.ReleaseDateUtc })
            .ToListAsync(cancellationToken);

        var supply = new List<FirmedSupplySnapshot>(firmedBuy.Count + firmedMake.Count);
        foreach (var o in firmedBuy)
        {
            supply.Add(new FirmedSupplySnapshot(
                o.ProductId,
                o.Quantity,
                ClampToAnchor(o.DueDateUtc, anchorUtc),
                ClampToAnchor(o.ReleaseDateUtc, anchorUtc),
                ProcurementType.Buy,
                o.Id));
        }
        foreach (var o in firmedMake)
        {
            supply.Add(new FirmedSupplySnapshot(
                o.ProductId,
                o.Quantity,
                ClampToAnchor(o.DueDateUtc, anchorUtc),
                ClampToAnchor(o.ReleaseDateUtc, anchorUtc),
                ProcurementType.Make,
                o.Id));
        }
        return supply;
    }

    private async Task<IReadOnlyList<IndependentDemandSnapshot>> LoadIndependentDemandAsync(
        IReadOnlyList<Guid> productIds,
        DateTime anchorUtc,
        CancellationToken cancellationToken)
    {
        var rows = await _db.OrderLines.AsNoTracking()
            .Where(l => productIds.Contains(l.ProductId)
                && l.Status != OrderLineStatus.Cancelled
                && l.Status != OrderLineStatus.Shipped
                && l.Status != OrderLineStatus.Invoiced
                && l.QuantityAllocated > l.QuantityShipped)
            .Select(l => new
            {
                l.Id,
                l.ProductId,
                Quantity = l.QuantityAllocated - l.QuantityShipped,
                DueDate = l.Order.RequestedDeliveryDate ?? l.Order.DueDate
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new IndependentDemandSnapshot(
                r.ProductId,
                r.Quantity,
                NormalizeDue(r.DueDate, anchorUtc),
                r.Id))
            .ToList();
    }

    private async Task<IReadOnlyList<ScheduledReceiptSnapshot>> LoadScheduledReceiptsAsync(
        IReadOnlyList<Guid> productIds,
        DateTime anchorUtc,
        CancellationToken cancellationToken)
    {
        var rows = await _db.PurchaseOrderLines.AsNoTracking()
            .Where(l => productIds.Contains(l.ProductId)
                && l.PurchaseOrder.Status != PurchaseOrderStatus.Cancelled
                && l.PurchaseOrder.Status != PurchaseOrderStatus.Closed
                && l.Quantity > l.QuantityReceived)
            .Select(l => new
            {
                l.ProductId,
                l.PurchaseOrderId,
                Quantity = l.Quantity - l.QuantityReceived,
                ExpectedDate = l.PurchaseOrder.ExpectedDate,
                l.PurchaseOrder.OrderDate
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new ScheduledReceiptSnapshot(
                r.ProductId,
                r.Quantity,
                NormalizeExpected(r.ExpectedDate, r.OrderDate, anchorUtc),
                r.PurchaseOrderId))
            .ToList();
    }

    private async Task<IReadOnlyList<DemandHistoryPointSnapshot>> LoadDemandHistoryAsync(
        IReadOnlyList<Guid> productIds,
        DateTime anchorUtc,
        CancellationToken cancellationToken)
    {
        var fromUtc = anchorUtc.AddDays(-DemandHistoryWindowDays);
        var rows = await _db.OrderLines.AsNoTracking()
            .Where(l => productIds.Contains(l.ProductId)
                && l.QuantityShipped > 0m
                && l.Order.OrderDate >= fromUtc)
            .GroupBy(l => new { l.ProductId, Day = l.Order.OrderDate.Date })
            .Select(g => new { g.Key.ProductId, g.Key.Day, Qty = g.Sum(x => x.QuantityShipped) })
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new DemandHistoryPointSnapshot(
                r.ProductId,
                DateTime.SpecifyKind(r.Day, DateTimeKind.Utc),
                r.Qty))
            .ToList();
    }

    private static DateTime ClampToAnchor(DateTime dateUtc, DateTime anchorUtc)
    {
        var utc = DateTime.SpecifyKind(dateUtc, DateTimeKind.Utc);
        return utc < anchorUtc ? anchorUtc : utc;
    }

    private static DateTime NormalizeDue(DateTime? dueDate, DateTime anchorUtc)
    {
        if (dueDate is not { } value)
        {
            return anchorUtc;
        }
        var utc = DateTime.SpecifyKind(value, DateTimeKind.Utc);
        return utc < anchorUtc ? anchorUtc : utc;
    }

    private static DateTime NormalizeExpected(DateTime? expected, DateTime orderDate, DateTime anchorUtc)
    {
        if (expected is { } value)
        {
            var utc = DateTime.SpecifyKind(value, DateTimeKind.Utc);
            return utc < anchorUtc ? anchorUtc : utc;
        }
        return anchorUtc;
    }

    private static MrpPlanningSnapshot Empty(DateTime anchorUtc) =>
        new(
            anchorUtc,
            new List<MrpProductSnapshot>(),
            new List<BomEdgeSnapshot>(),
            new List<IndependentDemandSnapshot>(),
            new List<ScheduledReceiptSnapshot>(),
            new List<DemandHistoryPointSnapshot>(),
            DemandHistoryWindowDays);
}
