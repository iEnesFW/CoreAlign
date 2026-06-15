using CoreAlign.Application.B2B;
using CoreAlign.Application.Inventory.Services;
using CoreAlign.Application.Mrp;
using CoreAlign.Application.Mrp.Planning;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.Manufacturing;
using CoreAlign.Domain.Entities.Mrp;
using CoreAlign.Domain.Entities.Purchasing;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Infrastructure.Mrp;

public sealed class MrpPlanningService : IMrpPlanningService, IMrpWorkbenchService
{
    private const string PlanRunSequencePrefix = "MRP";
    private const int PlanRunSequenceWidth = 5;

    private readonly IMrpPlanningDataLoader _dataLoader;
    private readonly IMrpPlanningEngine _engine;
    private readonly IMrpPlanRunRepository _planRuns;
    private readonly IPlannedProductionOrderRepository _productionOrders;
    private readonly IPurchaseRequisitionRepository _requisitions;
    private readonly IProductRepository _products;
    private readonly IDocumentSequenceRepository _sequences;
    private readonly IWarehouseRepository _warehouses;
    private readonly IProductionExecutionService _productionExecution;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<MrpPlanningService> _logger;

    public MrpPlanningService(
        IMrpPlanningDataLoader dataLoader,
        IMrpPlanningEngine engine,
        IMrpPlanRunRepository planRuns,
        IPlannedProductionOrderRepository productionOrders,
        IPurchaseRequisitionRepository requisitions,
        IProductRepository products,
        IDocumentSequenceRepository sequences,
        IWarehouseRepository warehouses,
        IProductionExecutionService productionExecution,
        ICurrentUserAccessor currentUser,
        IUnitOfWork uow,
        ILogger<MrpPlanningService> logger)
    {
        _dataLoader = dataLoader;
        _engine = engine;
        _planRuns = planRuns;
        _productionOrders = productionOrders;
        _requisitions = requisitions;
        _products = products;
        _sequences = sequences;
        _warehouses = warehouses;
        _productionExecution = productionExecution;
        _currentUser = currentUser;
        _uow = uow;
        _logger = logger;
    }

    public async Task<MrpPlanResult> RunPreviewAsync(DateTime asOfUtc, MrpBucketKind kind, int horizonDays, CancellationToken cancellationToken = default)
    {
        var snapshot = await _dataLoader.LoadAsync(asOfUtc, horizonDays, cancellationToken);
        return _engine.Run(snapshot, kind, horizonDays);
    }

    public async Task<MrpItemPlan?> GetItemPlanAsync(Guid productId, DateTime asOfUtc, MrpBucketKind kind, int horizonDays, CancellationToken cancellationToken = default)
    {
        var result = await RunPreviewAsync(asOfUtc, kind, horizonDays, cancellationToken);
        return result.Items.FirstOrDefault(i => i.ProductId == productId);
    }

    public async Task<MrpPlanRun> CommitAsync(DateTime asOfUtc, MrpBucketKind kind, int horizonDays, Guid operationId, MrpPlanningMode mode = MrpPlanningMode.Regenerative, CancellationToken cancellationToken = default)
    {
        var asOfDate = DateTime.SpecifyKind(asOfUtc.Date, DateTimeKind.Utc);
        var idempotencyKey = MrpPlanRun.BuildIdempotencyKey(asOfDate, kind, horizonDays);

        var existing = await _planRuns.GetByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            _logger.LogInformation(
                "MRP commit for key {IdempotencyKey} already exists ({PlanRunId}); returning existing run (idempotent replay).",
                idempotencyKey, existing.Id);
            return existing;
        }

        // NOTE: MrpPlanningMode.NetChange is accepted but currently behaves as Regenerative.
        // A correct incremental (net-change) plan requires a persistent "current plan" the
        // engine can diff against and update in place; the run-scoped, append-only model here
        // cannot express that without superseding/degrading baselines. Deferred to T5+ — see
        // ERP-MRP-008 in docs/mrp-blockers.md. The earlier skip-if-unchanged shortcut was
        // removed because it compared across mismatched bucket/horizon runs and false-skipped
        // on firm-override-mutated quantities (T3 adversarial review A1/A2/A3).
        var result = await RunPreviewAsync(asOfDate, kind, horizonDays, cancellationToken);

        // Lazily provision the plan-run sequence and SAVE it before consuming a value.
        // The auto-generate MRP path (MRP-BUG-1) consumed without an intervening save,
        // 500-ing on a fresh tenant; the manual requisition path saves between
        // EnsureExists and Consume. We follow the manual path's safe ordering here.
        await _sequences.EnsureExistsAsync(DocumentSequenceType.MrpPlanRunNumber, PlanRunSequencePrefix, PlanRunSequenceWidth, asOfDate.Year, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        var number = await _sequences.ConsumeAsync(DocumentSequenceType.MrpPlanRunNumber, asOfDate, cancellationToken);

        var run = new MrpPlanRun(number, asOfDate, kind, horizonDays, _currentUser.UserId ?? Guid.Empty);
        var productionOrders = new List<PlannedProductionOrder>();

        foreach (var item in result.Items)
        {
            foreach (var draft in item.PlannedOrders)
            {
                run.AddPlannedOrder(new MrpPlannedOrder(
                    draft.ProductId,
                    draft.LowLevelCode,
                    draft.Quantity,
                    draft.DueDateUtc,
                    draft.ReleaseDateUtc,
                    draft.PreferredSupplierId,
                    draft.EstimatedUnitCost,
                    draft.SourcePolicy));
            }

            foreach (var draft in item.ProductionOrders)
            {
                productionOrders.Add(new PlannedProductionOrder(
                    run.Id,
                    draft.ProductId,
                    draft.LowLevelCode,
                    draft.Quantity,
                    draft.DueDateUtc,
                    draft.ReleaseDateUtc,
                    draft.EstimatedUnitCost,
                    draft.SourcePolicy,
                    draft.PeggingParentProductId,
                    draft.PeggingSourceOrderLineId));
            }

            foreach (var action in item.Actions)
            {
                run.AddActionMessage(new MrpActionMessage(
                    action.ProductId,
                    action.ActionType,
                    action.Severity,
                    action.Quantity,
                    action.CurrentDateUtc,
                    action.SuggestedDateUtc,
                    action.RelatedPurchaseOrderId,
                    relatedPlannedOrderId: null,
                    action.DaysUntilStockOut,
                    action.Message));
            }

            foreach (var peg in item.Pegs)
            {
                run.AddPegging(new MrpPegging(
                    peg.ComponentProductId,
                    peg.RequirementQuantity,
                    peg.DueDateUtc,
                    peg.SourceKind,
                    peg.SourceParentProductId,
                    peg.SourceOrderLineId));
            }
        }

        await CarryForwardFirmedOrdersAsync(run, productionOrders, cancellationToken);

        run.SetSummary(result.ProductsEvaluated);
        await _planRuns.AddAsync(run, cancellationToken);
        await _productionOrders.AddRangeAsync(productionOrders, cancellationToken);

        _logger.LogInformation(
            "MRP plan run {Number} committed: {Products} products, {PlannedOrders} planned orders, {ProductionOrders} production orders, {Actions} actions.",
            run.Number, run.ProductsEvaluated, run.PlannedOrderCount, productionOrders.Count, run.ActionMessageCount);

        return run;
    }

    // Firmed-but-unreleased orders are commitments that must survive the next regeneration.
    // The engine already netted the new plan against them (loaded as fixed supply, scoped to
    // the latest run), so the fresh result holds only the incremental shortfall. We clone the
    // prior latest run's firmed orders into THIS run so the latest run always carries the full
    // live firmed set — keeping LoadFirmedSupplyAsync's single-run scope correct and preventing
    // the duplicate-order regeneration that dropping them would cause (T3 review finding #4).
    private async Task CarryForwardFirmedOrdersAsync(
        MrpPlanRun run,
        List<PlannedProductionOrder> productionOrders,
        CancellationToken cancellationToken)
    {
        var (latest, _) = await _planRuns.SearchPlanRunsAsync(page: 1, pageSize: 1, cancellationToken);
        if (latest.Count == 0)
        {
            return;
        }

        var priorRun = await _planRuns.GetByIdAsync(latest[0].Id, includeChildren: true, cancellationToken);
        if (priorRun is not null)
        {
            foreach (var firmed in priorRun.PlannedOrders.Where(o => o.IsFirmed && !o.IsReleased))
            {
                run.AddPlannedOrder(firmed.CloneFirmedForRun());
            }
        }

        var (priorProduction, _) = await _productionOrders.SearchAsync(
            latest[0].Id, productId: null, status: PlannedProductionOrderStatus.Firm, page: 1, pageSize: int.MaxValue, cancellationToken);
        foreach (var firmed in priorProduction)
        {
            productionOrders.Add(firmed.CloneFirmForRun(run.Id));
        }
    }

    public async Task<ReleaseResult> ReleaseAsync(Guid planRunId, IReadOnlyList<Guid> plannedOrderIds, Guid operationId, CancellationToken cancellationToken = default)
    {
        var orders = await _planRuns.GetPlannedOrdersAsync(planRunId, plannedOrderIds, cancellationToken);
        var releasable = orders.Where(o => !o.IsReleased).ToList();
        if (releasable.Count == 0)
        {
            return new ReleaseResult(planRunId, Array.Empty<Guid>(), 0);
        }

        var productIds = releasable.Select(o => o.ProductId).Distinct().ToList();
        var products = await _products.GetByIdsAsync(productIds, cancellationToken);

        var now = DateTime.UtcNow;
        await _sequences.EnsureExistsAsync(DocumentSequenceType.PurchaseRequisitionNumber, "PR", 5, now.Year, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        var requisitionIds = new List<Guid>();
        var released = 0;

        foreach (var group in releasable.GroupBy(o => o.PreferredSupplierId))
        {
            var orderedLines = group
                .Where(o => products.ContainsKey(o.ProductId))
                .Select(o => (Order: o, Product: products[o.ProductId]))
                .ToList();

            if (orderedLines.Count == 0)
            {
                continue;
            }

            var number = await _sequences.ConsumeAsync(DocumentSequenceType.PurchaseRequisitionNumber, now, cancellationToken);
            var requisition = new PurchaseRequisition(
                number,
                _currentUser.UserId ?? Guid.Empty,
                PurchaseRequisitionReason.MRPSuggestion,
                notes: $"Released from MRP plan run {planRunId} on {now:yyyy-MM-dd} UTC.");

            var lines = orderedLines
                .Select(x => new PurchaseRequisitionLine(
                    x.Product.Id,
                    x.Product.Sku,
                    x.Product.Name,
                    x.Order.Quantity,
                    x.Order.EstimatedUnitCost,
                    x.Order.PreferredSupplierId,
                    x.Order.DueDateUtc,
                    notes: $"MRP planned order {x.Order.Id} (release {x.Order.ReleaseDateUtc:yyyy-MM-dd})."))
                .ToList();

            requisition.ReplaceLines(lines);
            await _requisitions.AddAsync(requisition, cancellationToken);
            requisitionIds.Add(requisition.Id);

            foreach (var (order, _) in orderedLines)
            {
                order.MarkReleased(requisition.Id);
                released++;
            }
        }

        _logger.LogInformation(
            "MRP release for plan run {PlanRunId} created {Requisitions} requisitions from {Released} planned orders.",
            planRunId, requisitionIds.Count, released);

        return new ReleaseResult(planRunId, requisitionIds, released);
    }

    public async Task<PlannedProductionOrder> FirmProductionOrderAsync(
        Guid plannedProductionOrderId,
        decimal? overrideQuantity,
        DateTime? overrideDueDateUtc,
        Guid operationId,
        CancellationToken cancellationToken = default)
    {
        var order = await _productionOrders.GetByIdAsync(plannedProductionOrderId, cancellationToken)
            ?? throw new PlannedProductionOrderNotFoundException(plannedProductionOrderId);
        order.Firm(overrideQuantity, overrideDueDateUtc);
        return order;
    }

    public async Task<PlannedProductionOrder> ReleaseProductionOrderAsync(
        Guid plannedProductionOrderId,
        Guid operationId,
        CancellationToken cancellationToken = default)
    {
        var order = await _productionOrders.GetByIdAsync(plannedProductionOrderId, cancellationToken)
            ?? throw new PlannedProductionOrderNotFoundException(plannedProductionOrderId);
        order.Release();
        return order;
    }

    public async Task<CompleteProductionOrderResult> CompleteProductionOrderAsync(
        Guid plannedProductionOrderId,
        Guid operationId,
        Guid? warehouseId,
        CancellationToken cancellationToken = default)
    {
        var order = await _productionOrders.GetByIdAsync(plannedProductionOrderId, cancellationToken)
            ?? throw new PlannedProductionOrderNotFoundException(plannedProductionOrderId);

        // Idempotent replay: an already-completed (Closed) order must not re-issue
        // components or re-receive the assembly. Return the prior outcome as a no-op.
        if (order.Status == PlannedProductionOrderStatus.Closed)
        {
            _logger.LogInformation(
                "Production order {OrderId} already completed (operation {OperationId}); returning no-op (idempotent replay).",
                order.Id, operationId);
            return new CompleteProductionOrderResult(
                order,
                order.ProducedWarehouseId ?? Guid.Empty,
                order.Quantity,
                ComponentsIssued: 0,
                UnitCost: 0m,
                TotalCost: 0m,
                AlreadyCompleted: true);
        }

        // Validate the FSM BEFORE moving any stock: only a Released order may complete.
        // Guarding here (not just inside order.Complete) ensures a Planned/Firm order is
        // rejected with NO component issue / assembly receipt attempted at all.
        if (order.Status != PlannedProductionOrderStatus.Released)
        {
            throw new InvalidPlannedProductionOrderTransitionException(
                order.Status.ToString(), PlannedProductionOrderStatus.Closed.ToString());
        }

        var warehouse = await ResolveWarehouseAsync(warehouseId, cancellationToken);

        var execution = await _productionExecution.ExecuteAsync(
            order.ProductId,
            warehouse.Id,
            order.Quantity,
            reference: $"Production order {order.Id}",
            cancellationToken);

        order.Complete(warehouse.Id);

        _logger.LogInformation(
            "Production order {OrderId} completed: produced {Quantity} of {ProductId} into warehouse {WarehouseId}, issued {Components} components at unit cost {UnitCost} (operation {OperationId}).",
            order.Id, execution.ProducedQuantity, order.ProductId, warehouse.Id, execution.ComponentsIssued, execution.UnitCost, operationId);

        return new CompleteProductionOrderResult(
            order,
            warehouse.Id,
            execution.ProducedQuantity,
            execution.ComponentsIssued,
            execution.UnitCost,
            execution.TotalCost,
            AlreadyCompleted: false);
    }

    private async Task<Warehouse> ResolveWarehouseAsync(Guid? warehouseId, CancellationToken cancellationToken)
    {
        if (warehouseId is { } explicitId)
        {
            return await _warehouses.GetByIdAsync(explicitId, cancellationToken)
                ?? throw new WarehouseNotFoundException(explicitId);
        }

        var defaultWarehouse = await _warehouses.GetDefaultAsync(cancellationToken);
        if (defaultWarehouse is not null)
        {
            return defaultWarehouse;
        }

        var candidates = await _warehouses.ListAsync(isActive: true, cancellationToken);
        var fallback = candidates.FirstOrDefault(w => w.Type == WarehouseType.Main)
            ?? candidates.FirstOrDefault();
        return fallback ?? throw new WarehouseNotFoundException();
    }
}
