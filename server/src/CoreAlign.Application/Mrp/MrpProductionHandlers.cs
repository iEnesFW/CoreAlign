using CoreAlign.Application.Common;
using CoreAlign.Application.Mrp.Planning;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Mrp;

public class ListPlannedProductionOrdersHandler
    : IRequestHandler<ListPlannedProductionOrdersQuery, PagedResult<PlannedProductionOrderDto>>
{
    private readonly IPlannedProductionOrderRepository _productionOrders;
    public ListPlannedProductionOrdersHandler(IPlannedProductionOrderRepository productionOrders) =>
        _productionOrders = productionOrders;

    public async Task<PagedResult<PlannedProductionOrderDto>> Handle(
        ListPlannedProductionOrdersQuery q, CancellationToken ct)
    {
        var page = q.Page < 1 ? 1 : q.Page;
        var pageSize = q.PageSize is < 1 or > 200 ? 25 : q.PageSize;
        var (items, total) = await _productionOrders.SearchAsync(
            q.PlanRunId, q.ProductId, q.Status, page, pageSize, ct);
        return new PagedResult<PlannedProductionOrderDto>
        {
            Items = items.Select(o => o.ToDto()).ToList(),
            Total = total,
            Page = page,
            PageSize = pageSize,
        };
    }
}

public class GetProductionPeggingChainHandler
    : IRequestHandler<GetProductionPeggingChainQuery, IReadOnlyList<MrpPeggingDto>>
{
    private readonly IMrpPlanRunRepository _planRuns;
    public GetProductionPeggingChainHandler(IMrpPlanRunRepository planRuns) => _planRuns = planRuns;

    public async Task<IReadOnlyList<MrpPeggingDto>> Handle(GetProductionPeggingChainQuery q, CancellationToken ct)
    {
        var run = await _planRuns.GetByIdAsync(q.PlanRunId, includeChildren: false, ct)
            ?? throw new MrpPlanRunNotFoundException(q.PlanRunId);

        var allPegs = await _planRuns.GetAllPeggingAsync(run.Id, ct);
        var chain = PeggingChainResolver.ResolveUpstream(allPegs, q.ComponentProductId);
        return chain.Select(p => p.ToDto()).ToList();
    }
}

public class GetChangeImpactHandler : IRequestHandler<GetChangeImpactQuery, ChangeImpactResultDto>
{
    private readonly IMrpPlanRunRepository _planRuns;
    private readonly IMrpPlanningService _planning;
    private readonly IMrpChangeImpactAnalyzer _analyzer;

    public GetChangeImpactHandler(
        IMrpPlanRunRepository planRuns,
        IMrpPlanningService planning,
        IMrpChangeImpactAnalyzer analyzer)
    {
        _planRuns = planRuns;
        _planning = planning;
        _analyzer = analyzer;
    }

    public async Task<ChangeImpactResultDto> Handle(GetChangeImpactQuery q, CancellationToken ct)
    {
        var run = await _planRuns.GetByIdAsync(q.PlanRunId, includeChildren: false, ct)
            ?? throw new MrpPlanRunNotFoundException(q.PlanRunId);

        var plan = await _planning.RunPreviewAsync(run.AsOfDateUtc, run.BucketKind, run.HorizonDays, ct);
        var impact = _analyzer.Trace(plan, q.SourceOrderLineId);

        var supply = impact.DownstreamSupply
            .Select(s => new ChangeImpactSupplyOrderDto(
                s.ProductId,
                s.LowLevelCode,
                s.SinkKind,
                s.Quantity,
                s.DueDateUtc,
                s.ReleaseDateUtc,
                s.DirectParentProductId))
            .ToList();

        return new ChangeImpactResultDto(run.Id, q.SourceOrderLineId, supply);
    }
}

public class FirmPlannedProductionOrderHandler
    : IRequestHandler<FirmPlannedProductionOrderCommand, PlannedProductionOrderDto>
{
    private readonly IMrpWorkbenchService _workbench;
    public FirmPlannedProductionOrderHandler(IMrpWorkbenchService workbench) => _workbench = workbench;

    public async Task<PlannedProductionOrderDto> Handle(FirmPlannedProductionOrderCommand c, CancellationToken ct)
    {
        var order = await _workbench.FirmProductionOrderAsync(
            c.PlannedProductionOrderId, c.OverrideQuantity, c.OverrideDueDateUtc, c.OperationId, ct);
        return order.ToDto();
    }
}

public class ReleasePlannedProductionOrderHandler
    : IRequestHandler<ReleasePlannedProductionOrderCommand, PlannedProductionOrderDto>
{
    private readonly IMrpWorkbenchService _workbench;
    public ReleasePlannedProductionOrderHandler(IMrpWorkbenchService workbench) => _workbench = workbench;

    public async Task<PlannedProductionOrderDto> Handle(ReleasePlannedProductionOrderCommand c, CancellationToken ct)
    {
        var order = await _workbench.ReleaseProductionOrderAsync(c.PlannedProductionOrderId, c.OperationId, ct);
        return order.ToDto();
    }
}

public class CompletePlannedProductionOrderHandler
    : IRequestHandler<CompletePlannedProductionOrderCommand, CompletePlannedProductionOrderResultDto>
{
    private readonly IMrpWorkbenchService _workbench;
    public CompletePlannedProductionOrderHandler(IMrpWorkbenchService workbench) => _workbench = workbench;

    public async Task<CompletePlannedProductionOrderResultDto> Handle(
        CompletePlannedProductionOrderCommand c, CancellationToken ct)
    {
        var result = await _workbench.CompleteProductionOrderAsync(
            c.PlannedProductionOrderId, c.OperationId, c.WarehouseId, ct);

        return new CompletePlannedProductionOrderResultDto(
            result.Order.Id,
            result.Order.ProductId,
            result.WarehouseId,
            result.ProducedQuantity,
            result.ComponentsIssued,
            result.UnitCost,
            result.TotalCost,
            result.Order.Status,
            result.AlreadyCompleted);
    }
}
