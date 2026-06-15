using System.Text.Json;
using CoreAlign.Application.B2B;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Application.Mrp;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.Purchasing;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Events;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Infrastructure.Mrp;

public sealed class MrpService : IMrpService
{
    public const decimal DemandSafetyFactor = 1.2m;
    public const int DefaultForecastWindowDays = 90;
    public const int DefaultProjectionDaysAhead = 30;

    private readonly CoreAlignDbContext _db;
    private readonly IStockItemRepository _stockItems;
    private readonly IPurchaseRequisitionRepository _requisitions;
    private readonly IDocumentSequenceRepository _sequences;
    private readonly ITenantContext _tenant;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IOutboxRepository _outbox;
    private readonly IOutboxSignal _outboxSignal;
    private readonly ILogger<MrpService> _logger;

    public MrpService(
        CoreAlignDbContext db,
        IStockItemRepository stockItems,
        IPurchaseRequisitionRepository requisitions,
        IDocumentSequenceRepository sequences,
        ITenantContext tenant,
        ICurrentUserAccessor currentUser,
        IOutboxRepository outbox,
        IOutboxSignal outboxSignal,
        ILogger<MrpService> logger)
    {
        _db = db;
        _stockItems = stockItems;
        _requisitions = requisitions;
        _sequences = sequences;
        _tenant = tenant;
        _currentUser = currentUser;
        _outbox = outbox;
        _outboxSignal = outboxSignal;
        _logger = logger;
    }

    public async Task<DemandForecastDto?> CalculateDemandForecastAsync(Guid productId, int windowDays = DefaultForecastWindowDays, CancellationToken cancellationToken = default)
    {
        if (windowDays <= 0) windowDays = DefaultForecastWindowDays;
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);
        if (product is null) return null;

        var fromUtc = DateTime.UtcNow.AddDays(-windowDays);
        var demandGroups = await _db.OrderLines
            .Where(l => l.ProductId == productId && l.QuantityShipped > 0m && l.UpdatedAtUtc >= fromUtc)
            .GroupBy(l => l.UpdatedAtUtc.Date)
            .Select(g => new { Day = g.Key, Qty = g.Sum(x => x.QuantityShipped) })
            .ToListAsync(cancellationToken);

        var total = demandGroups.Sum(g => g.Qty);
        var avgDaily = windowDays > 0 ? Math.Round(total / windowDays, 4) : 0m;
        decimal? peak = demandGroups.Count > 0 ? demandGroups.Max(g => g.Qty) : null;

        return new DemandForecastDto(product.Id, product.Sku, product.Name, windowDays, total, avgDaily, peak, DateTime.UtcNow);
    }

    public async Task<ReorderPointDto?> CalculateReorderPointAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);
        if (product is null) return null;

        var forecast = await CalculateDemandForecastAsync(productId, DefaultForecastWindowDays, cancellationToken);
        var avgDaily = forecast?.AverageDailyDemand ?? 0m;
        var computed = Math.Round(product.SafetyStock + (product.LeadTimeDays * avgDaily * DemandSafetyFactor), 4);

        return new ReorderPointDto(
            product.Id,
            product.Sku,
            product.Name,
            product.SafetyStock,
            product.LeadTimeDays,
            avgDaily,
            computed,
            product.ReorderPoint);
    }

    public async Task<StockProjectionDto?> ProjectStockBalanceAsync(Guid productId, int daysAhead = DefaultProjectionDaysAhead, CancellationToken cancellationToken = default)
    {
        if (daysAhead <= 0) daysAhead = DefaultProjectionDaysAhead;
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);
        if (product is null) return null;

        var onHand = await _stockItems.SumOnHandAsync(productId, cancellationToken);
        var reserved = await _stockItems.SumReservedAsync(productId, cancellationToken);

        var onOrder = await _db.PurchaseOrderLines
            .Where(l => l.ProductId == productId
                && l.PurchaseOrder.Status != PurchaseOrderStatus.Cancelled
                && l.PurchaseOrder.Status != PurchaseOrderStatus.Closed
                && l.Quantity > l.QuantityReceived)
            .SumAsync(l => (decimal?)(l.Quantity - l.QuantityReceived), cancellationToken) ?? 0m;

        var committed = await _db.OrderLines
            .Where(l => l.ProductId == productId
                && l.Status != OrderLineStatus.Cancelled
                && l.Status != OrderLineStatus.Shipped
                && l.Status != OrderLineStatus.Invoiced
                && l.QuantityAllocated > l.QuantityShipped)
            .SumAsync(l => (decimal?)(l.QuantityAllocated - l.QuantityShipped), cancellationToken) ?? 0m;

        var forecast = await CalculateDemandForecastAsync(productId, DefaultForecastWindowDays, cancellationToken);
        var avgDaily = forecast?.AverageDailyDemand ?? 0m;

        var reorderPoint = product.ReorderPoint > 0m
            ? product.ReorderPoint
            : Math.Round(product.SafetyStock + (product.LeadTimeDays * avgDaily * DemandSafetyFactor), 4);

        var today = DateTime.UtcNow.Date;
        var points = new List<StockProjectionPoint>(daysAhead + 1);
        var available = onHand - reserved + onOrder;
        var running = available;
        points.Add(new StockProjectionPoint(today, Math.Round(running, 4), 0m, onOrder, committed));
        for (var d = 1; d <= daysAhead; d++)
        {
            running -= avgDaily;
            points.Add(new StockProjectionPoint(today.AddDays(d), Math.Round(running, 4), avgDaily, 0m, 0m));
        }

        var shouldReorder = available < reorderPoint;
        var maxStockTarget = product.MaxStock > 0m ? product.MaxStock : reorderPoint * 2m;
        var suggestedQty = shouldReorder
            ? Math.Max(0m, Math.Round(maxStockTarget - available, 4))
            : 0m;

        return new StockProjectionDto(
            product.Id,
            product.Sku,
            product.Name,
            onHand,
            reserved,
            onOrder,
            committed,
            reorderPoint,
            daysAhead,
            points,
            shouldReorder,
            suggestedQty);
    }

    public async Task<MrpSuggestionResultDto> GenerateRequisitionSuggestionsAsync(DateTime asOfDateUtc, CancellationToken cancellationToken = default)
    {
        var products = await _db.Products
            .Where(p => p.IsStockTracked && p.Status == ProductStatus.Active)
            .ToListAsync(cancellationToken);

        var candidates = await BuildCandidatesAsync(products, cancellationToken);
        if (candidates.Count == 0)
        {
            return new MrpSuggestionResultDto(products.Count, 0, 0, Array.Empty<Guid>(), asOfDateUtc);
        }

        var grouped = candidates.GroupBy(c => c.PreferredSupplierId).ToList();
        var requisitionIds = new List<Guid>();
        var totalLines = 0;

        // MRP-BUG-1: the sequence row must be persisted BEFORE the first Consume reads it.
        // On a fresh tenant (no pre-seeded PurchaseRequisitionNumber sequence) EnsureExists
        // only stages an Added entity; without an intervening save Consume queries a row that
        // is not yet in the DB and 500s. The manual requisition path already saves here.
        await _sequences.EnsureExistsAsync(DocumentSequenceType.PurchaseRequisitionNumber, "PR", 5, asOfDateUtc.Year, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        foreach (var group in grouped)
        {
            var number = await _sequences.ConsumeAsync(DocumentSequenceType.PurchaseRequisitionNumber, asOfDateUtc, cancellationToken);
            var requesterId = _currentUser.UserId ?? Guid.Empty;
            var requisition = new PurchaseRequisition(
                number,
                requesterId,
                PurchaseRequisitionReason.MRPSuggestion,
                notes: $"Auto-generated by MRP run on {asOfDateUtc:yyyy-MM-dd} UTC.");

            var lines = group
                .Where(c => c.SuggestedOrderQuantity > 0m)
                .Select(c =>
                {
                    var product = products.First(p => p.Id == c.ProductId);
                    var expectedDelivery = product.LeadTimeDays > 0
                        ? (DateTime?)asOfDateUtc.AddDays(product.LeadTimeDays)
                        : null;
                    var unitCost = product.LastPurchaseCost > 0m ? product.LastPurchaseCost : product.StandardCost;
                    return new PurchaseRequisitionLine(
                        product.Id,
                        product.Sku,
                        product.Name,
                        c.SuggestedOrderQuantity,
                        unitCost,
                        c.PreferredSupplierId,
                        expectedDelivery,
                        notes: $"MRP suggested reorder. Projected available {c.ProjectedAvailable}; ROP {c.ReorderPoint}.");
                })
                .ToList();

            if (lines.Count == 0) continue;
            requisition.ReplaceLines(lines);
            await _requisitions.AddAsync(requisition, cancellationToken);
            requisitionIds.Add(requisition.Id);
            totalLines += lines.Count;
        }

        if (requisitionIds.Count > 0)
        {
            var evt = new MrpSuggestionsCreatedEvent(
                _tenant.CurrentTenantId ?? Guid.Empty,
                requisitionIds.Count,
                totalLines,
                asOfDateUtc,
                requisitionIds,
                DateTime.UtcNow);
            var msg = new OutboxMessage(MrpSuggestionsCreatedOutboxHandler.MessageTypeKey, JsonSerializer.Serialize(evt));
            await _outbox.AddAsync(msg, cancellationToken);
            _outboxSignal.MarkPending();
        }

        _logger.LogInformation(
            "MRP run created {ReqCount} requisitions with {LineCount} lines from {Candidates} candidates.",
            requisitionIds.Count, totalLines, candidates.Count);

        return new MrpSuggestionResultDto(candidates.Count, requisitionIds.Count, totalLines, requisitionIds, asOfDateUtc);
    }

    public async Task<MrpDashboardDto> GetDashboardAsync(int topN, CancellationToken cancellationToken = default)
    {
        if (topN <= 0) topN = 20;
        var products = await _db.Products
            .Where(p => p.IsStockTracked && p.Status == ProductStatus.Active)
            .ToListAsync(cancellationToken);

        var candidates = await BuildCandidatesAsync(products, cancellationToken);

        var pending = await _db.PurchaseRequisitions
            .Where(r => r.Status == PurchaseRequisitionStatus.Submitted || r.Status == PurchaseRequisitionStatus.Approved)
            .CountAsync(cancellationToken);
        var openPos = await _db.PurchaseOrders
            .Where(o => o.Status == PurchaseOrderStatus.Submitted
                || o.Status == PurchaseOrderStatus.Approved
                || o.Status == PurchaseOrderStatus.PartiallyReceived)
            .CountAsync(cancellationToken);

        var top = candidates
            .OrderBy(c => c.DaysUntilStockOut)
            .ThenByDescending(c => c.ReorderPoint - c.ProjectedAvailable)
            .Take(topN)
            .ToList();

        return new MrpDashboardDto(products.Count, candidates.Count, pending, openPos, top, DateTime.UtcNow);
    }

    private async Task<List<MrpReorderCandidateDto>> BuildCandidatesAsync(List<Product> products, CancellationToken cancellationToken)
    {
        if (products.Count == 0) return new List<MrpReorderCandidateDto>();

        var productIds = products.Select(p => p.Id).ToList();
        var batch = await LoadCandidateBatchAsync(productIds, cancellationToken);

        var candidates = new List<MrpReorderCandidateDto>(products.Count);
        foreach (var product in products)
        {
            var stock = batch.StockMap.TryGetValue(product.Id, out var s) ? s : (OnHand: 0m, Reserved: 0m);
            var onOrder = batch.OnOrderMap.TryGetValue(product.Id, out var oo) ? oo : 0m;
            var committed = batch.CommittedMap.TryGetValue(product.Id, out var co) ? co : 0m;
            var demand = batch.DemandTotalMap.TryGetValue(product.Id, out var d) ? d : 0m;

            var avgDaily = batch.WindowDays > 0 ? Math.Round(demand / batch.WindowDays, 4) : 0m;

            var reorderPoint = product.ReorderPoint > 0m
                ? product.ReorderPoint
                : Math.Round(product.SafetyStock + (product.LeadTimeDays * avgDaily * DemandSafetyFactor), 4);

            var available = stock.OnHand - stock.Reserved + onOrder;
            var shouldReorder = available < reorderPoint;
            if (!shouldReorder) continue;

            var maxStockTarget = product.MaxStock > 0m ? product.MaxStock : reorderPoint * 2m;
            var suggestedQty = Math.Max(0m, Math.Round(maxStockTarget - available, 4));

            var daysUntilOut = avgDaily > 0m ? (int)Math.Max(0, Math.Floor(available / avgDaily)) : int.MaxValue;

            candidates.Add(new MrpReorderCandidateDto(
                product.Id,
                product.Sku,
                product.Name,
                stock.OnHand,
                stock.Reserved,
                onOrder,
                committed,
                Math.Round(available, 4),
                reorderPoint,
                suggestedQty,
                product.PreferredSupplierId,
                product.LeadTimeDays,
                daysUntilOut == int.MaxValue ? 9999 : daysUntilOut));
        }
        return candidates;
    }

    private sealed record CandidateBatchData(
        IReadOnlyDictionary<Guid, (decimal OnHand, decimal Reserved)> StockMap,
        IReadOnlyDictionary<Guid, decimal> OnOrderMap,
        IReadOnlyDictionary<Guid, decimal> CommittedMap,
        IReadOnlyDictionary<Guid, decimal> DemandTotalMap,
        int WindowDays);

    private async Task<CandidateBatchData> LoadCandidateBatchAsync(IReadOnlyList<Guid> productIds, CancellationToken ct)
    {
        var stockMap = await _stockItems
            .SumOnHandAndReservedByProductsAsync(productIds, warehouseId: null, ct);

        var onOrderRows = await _db.PurchaseOrderLines.AsNoTracking()
            .Where(l => productIds.Contains(l.ProductId)
                && l.PurchaseOrder.Status != PurchaseOrderStatus.Cancelled
                && l.PurchaseOrder.Status != PurchaseOrderStatus.Closed
                && l.Quantity > l.QuantityReceived)
            .GroupBy(l => l.ProductId)
            .Select(g => new { ProductId = g.Key, Qty = g.Sum(x => x.Quantity - x.QuantityReceived) })
            .ToListAsync(ct);
        var onOrderMap = onOrderRows.ToDictionary(r => r.ProductId, r => r.Qty);

        var committedRows = await _db.OrderLines.AsNoTracking()
            .Where(l => productIds.Contains(l.ProductId)
                && l.Status != OrderLineStatus.Cancelled
                && l.Status != OrderLineStatus.Shipped
                && l.Status != OrderLineStatus.Invoiced
                && l.QuantityAllocated > l.QuantityShipped)
            .GroupBy(l => l.ProductId)
            .Select(g => new { ProductId = g.Key, Qty = g.Sum(x => x.QuantityAllocated - x.QuantityShipped) })
            .ToListAsync(ct);
        var committedMap = committedRows.ToDictionary(r => r.ProductId, r => r.Qty);

        var windowDays = DefaultForecastWindowDays;
        var fromUtc = DateTime.UtcNow.AddDays(-windowDays);
        var demandRows = await _db.OrderLines.AsNoTracking()
            .Where(l => productIds.Contains(l.ProductId)
                && l.QuantityShipped > 0m
                && l.UpdatedAtUtc >= fromUtc)
            .GroupBy(l => l.ProductId)
            .Select(g => new { ProductId = g.Key, Total = g.Sum(x => x.QuantityShipped) })
            .ToListAsync(ct);
        var demandTotalMap = demandRows.ToDictionary(r => r.ProductId, r => r.Total);

        return new CandidateBatchData(stockMap, onOrderMap, committedMap, demandTotalMap, windowDays);
    }
}
