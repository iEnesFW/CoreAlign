using CoreAlign.Application.B2B;
using CoreAlign.Application.Common;
using CoreAlign.Domain.Entities.Manufacturing;
using CoreAlign.Domain.Entities.Mrp;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Mrp;

public class RunMrpPreviewHandler : IRequestHandler<RunMrpPreviewQuery, MrpPlanResultDto>
{
    private readonly IMrpPlanningService _planning;
    private readonly IMrpPlanRunRepository _planRuns;
    private readonly IPlannedProductionOrderRepository _productionOrders;

    public RunMrpPreviewHandler(
        IMrpPlanningService planning,
        IMrpPlanRunRepository planRuns,
        IPlannedProductionOrderRepository productionOrders)
    {
        _planning = planning;
        _planRuns = planRuns;
        _productionOrders = productionOrders;
    }

    public async Task<MrpPlanResultDto> Handle(RunMrpPreviewQuery q, CancellationToken ct)
    {
        var asOf = q.AsOfDateUtc ?? DateTime.UtcNow;
        var result = await _planning.RunPreviewAsync(asOf, q.BucketKind, q.HorizonDays, ct);
        var dto = result.ToDto();

        var key = MrpPlanRun.BuildIdempotencyKey(
            DateTime.SpecifyKind((q.AsOfDateUtc ?? DateTime.UtcNow).Date, DateTimeKind.Utc),
            q.BucketKind,
            q.HorizonDays);

        var keyRun = await _planRuns.GetByIdempotencyKeyAsync(key, ct);
        if (keyRun is null)
        {
            return dto;
        }

        var run = await _planRuns.GetByIdAsync(keyRun.Id, includeChildren: true, ct) ?? keyRun;

        var buyByProduct = (run.PlannedOrders ?? Array.Empty<MrpPlannedOrder>())
            .GroupBy(o => o.ProductId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<MrpPlannedOrder>)g.ToList());

        var makeOrders = await _productionOrders.ListByRunAsync(run.Id, null, ct);
        var makeByProduct = (makeOrders ?? Array.Empty<PlannedProductionOrder>())
            .GroupBy(o => o.ProductId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<PlannedProductionOrder>)g.ToList());

        var items = dto.Items.Select(it => it with
        {
            PlannedOrders = buyByProduct.TryGetValue(it.ProductId, out var buys)
                ? buys.Select(ToBuyDraft).ToList()
                : it.PlannedOrders,
            ProductionOrders = makeByProduct.TryGetValue(it.ProductId, out var makes)
                ? makes.Select(ToMakeDraft).ToList()
                : it.ProductionOrders,
        }).ToList();

        return dto with
        {
            PlanRunId = run.Id,
            Status = run.Status,
            Items = items,
        };
    }

    private static MrpPlannedOrderDraftDto ToBuyDraft(MrpPlannedOrder o) => new(
        o.ProductId,
        o.LowLevelCode,
        o.Quantity,
        o.DueDateUtc,
        o.ReleaseDateUtc,
        o.PreferredSupplierId,
        o.EstimatedUnitCost,
        o.SourcePolicy,
        ProcurementType.Buy,
        o.Id,
        o.IsFirmed,
        o.IsReleased,
        o.ConvertedRequisitionId);

    private static MrpProductionOrderDraftDto ToMakeDraft(PlannedProductionOrder o) => new(
        o.ProductId,
        o.LowLevelCode,
        o.Quantity,
        o.DueDateUtc,
        o.ReleaseDateUtc,
        o.EstimatedUnitCost,
        o.SourcePolicy,
        o.PeggingParentProductId,
        o.PeggingSourceOrderLineId,
        o.Id,
        o.Status);
}

public class GetMrpItemPlanHandler : IRequestHandler<GetMrpItemPlanQuery, MrpItemPlanDto?>
{
    private readonly IMrpPlanningService _planning;
    public GetMrpItemPlanHandler(IMrpPlanningService planning) => _planning = planning;

    public async Task<MrpItemPlanDto?> Handle(GetMrpItemPlanQuery q, CancellationToken ct)
    {
        var asOf = q.AsOfDateUtc ?? DateTime.UtcNow;
        var item = await _planning.GetItemPlanAsync(q.ProductId, asOf, q.BucketKind, q.HorizonDays, ct);
        return item is null ? null : item.ToDto();
    }
}

public class ListMrpActionMessagesHandler : IRequestHandler<ListMrpActionMessagesQuery, PagedResult<MrpActionMessageDto>>
{
    private readonly IMrpPlanRunRepository _planRuns;
    public ListMrpActionMessagesHandler(IMrpPlanRunRepository planRuns) => _planRuns = planRuns;

    public async Task<PagedResult<MrpActionMessageDto>> Handle(ListMrpActionMessagesQuery q, CancellationToken ct)
    {
        var page = q.Page < 1 ? 1 : q.Page;
        var pageSize = q.PageSize is < 1 or > 200 ? 25 : q.PageSize;
        var (items, total) = await _planRuns.SearchActionMessagesAsync(
            q.PlanRunId, q.ActionType, q.Severity, q.SupplierId, q.IncludeDismissed, page, pageSize, ct);
        return new PagedResult<MrpActionMessageDto>
        {
            Items = items.Select(m => m.ToDto()).ToList(),
            Total = total,
            Page = page,
            PageSize = pageSize,
        };
    }
}

public class ListMrpPlanRunsHandler : IRequestHandler<ListMrpPlanRunsQuery, PagedResult<MrpPlanRunDto>>
{
    private readonly IMrpPlanRunRepository _planRuns;
    public ListMrpPlanRunsHandler(IMrpPlanRunRepository planRuns) => _planRuns = planRuns;

    public async Task<PagedResult<MrpPlanRunDto>> Handle(ListMrpPlanRunsQuery q, CancellationToken ct)
    {
        var page = q.Page < 1 ? 1 : q.Page;
        var pageSize = q.PageSize is < 1 or > 200 ? 25 : q.PageSize;
        var (items, total) = await _planRuns.SearchPlanRunsAsync(page, pageSize, ct);
        return new PagedResult<MrpPlanRunDto>
        {
            Items = items.Select(r => r.ToDto()).ToList(),
            Total = total,
            Page = page,
            PageSize = pageSize,
        };
    }
}

public class GetMrpPeggingHandler : IRequestHandler<GetMrpPeggingQuery, IReadOnlyList<MrpPeggingDto>>
{
    private readonly IMrpPlanRunRepository _planRuns;
    public GetMrpPeggingHandler(IMrpPlanRunRepository planRuns) => _planRuns = planRuns;

    public async Task<IReadOnlyList<MrpPeggingDto>> Handle(GetMrpPeggingQuery q, CancellationToken ct)
    {
        var run = await _planRuns.GetByIdAsync(q.PlanRunId, includeChildren: false, ct)
            ?? throw new MrpPlanRunNotFoundException(q.PlanRunId);
        var pegs = await _planRuns.GetPeggingAsync(run.Id, q.ComponentProductId, ct);
        return pegs.Select(p => p.ToDto()).ToList();
    }
}

public class CommitMrpPlanHandler : IRequestHandler<CommitMrpPlanCommand, MrpPlanRunDto>
{
    private readonly IMrpWorkbenchService _workbench;
    public CommitMrpPlanHandler(IMrpWorkbenchService workbench) => _workbench = workbench;

    public async Task<MrpPlanRunDto> Handle(CommitMrpPlanCommand c, CancellationToken ct)
    {
        var asOf = c.AsOfDateUtc ?? DateTime.UtcNow;
        var run = await _workbench.CommitAsync(asOf, c.BucketKind, c.HorizonDays, c.OperationId, c.Mode, ct);
        return run.ToDto();
    }
}

public class ReleasePlannedOrdersHandler : IRequestHandler<ReleasePlannedOrdersCommand, ReleasePlannedOrdersResultDto>
{
    private readonly IMrpWorkbenchService _workbench;
    private readonly IMrpPlanRunRepository _planRuns;

    public ReleasePlannedOrdersHandler(IMrpWorkbenchService workbench, IMrpPlanRunRepository planRuns)
    {
        _workbench = workbench;
        _planRuns = planRuns;
    }

    public async Task<ReleasePlannedOrdersResultDto> Handle(ReleasePlannedOrdersCommand c, CancellationToken ct)
    {
        _ = await _planRuns.GetByIdAsync(c.PlanRunId, includeChildren: false, ct)
            ?? throw new MrpPlanRunNotFoundException(c.PlanRunId);

        var result = await _workbench.ReleaseAsync(c.PlanRunId, c.PlannedOrderIds, c.OperationId, ct);
        return new ReleasePlannedOrdersResultDto(result.PlanRunId, result.RequisitionIds, result.PlannedOrdersReleased);
    }
}

public class FirmMrpPlannedOrderHandler : IRequestHandler<FirmMrpPlannedOrderCommand, MrpPlannedOrderDto>
{
    private readonly IMrpPlanRunRepository _planRuns;
    public FirmMrpPlannedOrderHandler(IMrpPlanRunRepository planRuns) => _planRuns = planRuns;

    public async Task<MrpPlannedOrderDto> Handle(FirmMrpPlannedOrderCommand c, CancellationToken ct)
    {
        var order = await _planRuns.GetPlannedOrderByIdAsync(c.PlannedOrderId, ct)
            ?? throw new MrpPlannedOrderNotFoundException(c.PlannedOrderId);
        order.Firm(c.OverrideQuantity, c.OverrideDueDateUtc);
        return order.ToDto();
    }
}

public class DismissMrpActionMessageHandler : IRequestHandler<DismissMrpActionMessageCommand, Unit>
{
    private readonly IMrpPlanRunRepository _planRuns;
    private readonly ICurrentUserAccessor _currentUser;

    public DismissMrpActionMessageHandler(IMrpPlanRunRepository planRuns, ICurrentUserAccessor currentUser)
    {
        _planRuns = planRuns;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(DismissMrpActionMessageCommand c, CancellationToken ct)
    {
        var message = await _planRuns.GetActionMessageByIdAsync(c.ActionMessageId, ct)
            ?? throw new MrpActionMessageNotFoundException(c.ActionMessageId);
        message.Dismiss(_currentUser.UserId);
        return Unit.Value;
    }
}
