using CoreAlign.Application.Manufacturing.DTOs;
using CoreAlign.Application.Manufacturing.Mapping;
using CoreAlign.Application.Manufacturing.Queries;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Manufacturing.Handlers;

public class GetProductionRoutingByIdHandler
    : IRequestHandler<GetProductionRoutingByIdQuery, ProductionRoutingDto>
{
    private readonly IProductionRoutingRepository _routings;
    private readonly IWorkCenterRepository _workCenters;
    private readonly ITenantContext _tenant;

    public GetProductionRoutingByIdHandler(
        IProductionRoutingRepository routings,
        IWorkCenterRepository workCenters,
        ITenantContext tenant)
    {
        _routings = routings;
        _workCenters = workCenters;
        _tenant = tenant;
    }

    public async Task<ProductionRoutingDto> Handle(GetProductionRoutingByIdQuery q, CancellationToken ct)
    {
        var tenantId = _tenant.RequireTenantId();
        var routing = await _routings.GetByIdReadAsync(tenantId, q.Id, ct)
            ?? throw new RoutingNotFoundException(q.Id);
        return await RoutingDtoAssembler.BuildAsync(routing, _workCenters, ct);
    }
}

public class ListProductionRoutingsHandler
    : IRequestHandler<ListProductionRoutingsQuery, IReadOnlyList<ProductionRoutingSummaryDto>>
{
    private readonly IProductionRoutingRepository _routings;
    private readonly ITenantContext _tenant;

    public ListProductionRoutingsHandler(IProductionRoutingRepository routings, ITenantContext tenant)
    {
        _routings = routings;
        _tenant = tenant;
    }

    public async Task<IReadOnlyList<ProductionRoutingSummaryDto>> Handle(
        ListProductionRoutingsQuery q, CancellationToken ct)
    {
        var tenantId = _tenant.RequireTenantId();
        var rows = await _routings.ListSummariesAsync(
            tenantId, q.Status, Math.Clamp(q.Take, 1, 500), ct);
        return rows.Select(RoutingMapper.ToSummaryDto).ToList();
    }
}
