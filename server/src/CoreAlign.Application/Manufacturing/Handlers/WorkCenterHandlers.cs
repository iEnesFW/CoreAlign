using CoreAlign.Application.Manufacturing.Commands;
using CoreAlign.Application.Manufacturing.DTOs;
using CoreAlign.Application.Manufacturing.Mapping;
using CoreAlign.Application.Manufacturing.Queries;
using CoreAlign.Domain.Entities.Manufacturing;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Manufacturing.Handlers;

public class CreateWorkCenterHandler : IRequestHandler<CreateWorkCenterCommand, WorkCenterDto>
{
    private readonly IWorkCenterRepository _workCenters;
    private readonly ITenantContext _tenant;

    public CreateWorkCenterHandler(IWorkCenterRepository workCenters, ITenantContext tenant)
    {
        _workCenters = workCenters;
        _tenant = tenant;
    }

    public async Task<WorkCenterDto> Handle(CreateWorkCenterCommand c, CancellationToken ct)
    {
        var tenantId = _tenant.RequireTenantId();
        if (await _workCenters.CodeExistsAsync(tenantId, c.Code.Trim(), null, ct))
        {
            throw new WorkCenterCodeConflictException(c.Code.Trim());
        }
        var workCenter = new WorkCenter(c.Code.Trim(), c.Name.Trim(), c.DailyCapacityMinutes);
        await _workCenters.AddAsync(workCenter, ct);
        return RoutingMapper.ToDto(workCenter);
    }
}

public class UpdateWorkCenterHandler : IRequestHandler<UpdateWorkCenterCommand, WorkCenterDto>
{
    private readonly IWorkCenterRepository _workCenters;
    private readonly ITenantContext _tenant;

    public UpdateWorkCenterHandler(IWorkCenterRepository workCenters, ITenantContext tenant)
    {
        _workCenters = workCenters;
        _tenant = tenant;
    }

    public async Task<WorkCenterDto> Handle(UpdateWorkCenterCommand c, CancellationToken ct)
    {
        var tenantId = _tenant.RequireTenantId();
        var workCenter = await _workCenters.GetByIdAsync(tenantId, c.Id, ct)
            ?? throw new WorkCenterNotFoundException(c.Id);
        if (await _workCenters.CodeExistsAsync(tenantId, c.Code.Trim(), c.Id, ct))
        {
            throw new WorkCenterCodeConflictException(c.Code.Trim());
        }
        workCenter.Update(c.Code.Trim(), c.Name.Trim(), c.DailyCapacityMinutes, c.IsActive);
        return RoutingMapper.ToDto(workCenter);
    }
}

public class GetWorkCenterByIdHandler : IRequestHandler<GetWorkCenterByIdQuery, WorkCenterDto>
{
    private readonly IWorkCenterRepository _workCenters;
    private readonly ITenantContext _tenant;

    public GetWorkCenterByIdHandler(IWorkCenterRepository workCenters, ITenantContext tenant)
    {
        _workCenters = workCenters;
        _tenant = tenant;
    }

    public async Task<WorkCenterDto> Handle(GetWorkCenterByIdQuery q, CancellationToken ct)
    {
        var tenantId = _tenant.RequireTenantId();
        var workCenter = await _workCenters.GetByIdAsync(tenantId, q.Id, ct)
            ?? throw new WorkCenterNotFoundException(q.Id);
        return RoutingMapper.ToDto(workCenter);
    }
}

public class ListWorkCentersHandler : IRequestHandler<ListWorkCentersQuery, IReadOnlyList<WorkCenterDto>>
{
    private readonly IWorkCenterRepository _workCenters;
    private readonly ITenantContext _tenant;

    public ListWorkCentersHandler(IWorkCenterRepository workCenters, ITenantContext tenant)
    {
        _workCenters = workCenters;
        _tenant = tenant;
    }

    public async Task<IReadOnlyList<WorkCenterDto>> Handle(ListWorkCentersQuery q, CancellationToken ct)
    {
        var tenantId = _tenant.RequireTenantId();
        var items = await _workCenters.ListAsync(tenantId, q.IncludeInactive, ct);
        return items.Select(RoutingMapper.ToDto).ToList();
    }
}
