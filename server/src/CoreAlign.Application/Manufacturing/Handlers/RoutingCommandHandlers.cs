using CoreAlign.Application.Manufacturing.Commands;
using CoreAlign.Application.Manufacturing.DTOs;
using CoreAlign.Application.Manufacturing.Mapping;
using CoreAlign.Domain.Entities.Manufacturing;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Manufacturing.Handlers;

internal static class RoutingDtoAssembler
{
    public static async Task<ProductionRoutingDto> BuildAsync(
        ProductionRouting routing,
        IWorkCenterRepository workCenters,
        CancellationToken ct)
    {
        var wcIds = routing.Steps.Select(s => s.WorkCenterId).Distinct().ToList();
        IReadOnlyDictionary<Guid, string> names = wcIds.Count == 0
            ? new Dictionary<Guid, string>()
            : (await workCenters.GetByIdsAsync(wcIds, ct)).ToDictionary(w => w.Id, w => w.Name);
        return RoutingMapper.ToDto(routing, names);
    }
}

public class CreateProductionRoutingHandler
    : IRequestHandler<CreateProductionRoutingCommand, ProductionRoutingDto>
{
    private readonly IProductionRoutingRepository _routings;
    private readonly ITenantContext _tenant;

    public CreateProductionRoutingHandler(IProductionRoutingRepository routings, ITenantContext tenant)
    {
        _routings = routings;
        _tenant = tenant;
    }

    public async Task<ProductionRoutingDto> Handle(CreateProductionRoutingCommand c, CancellationToken ct)
    {
        var tenantId = _tenant.RequireTenantId();
        if (await _routings.CodeExistsAsync(tenantId, c.Code.Trim(), null, ct))
        {
            throw new RoutingCodeConflictException(c.Code.Trim());
        }
        var routing = new ProductionRouting(c.Code, c.Name, c.Description);
        await _routings.AddAsync(routing, ct);
        return RoutingMapper.ToDto(routing, new Dictionary<Guid, string>());
    }
}

public class UpdateProductionRoutingHandler
    : IRequestHandler<UpdateProductionRoutingCommand, ProductionRoutingDto>
{
    private readonly IProductionRoutingRepository _routings;
    private readonly IWorkCenterRepository _workCenters;
    private readonly ITenantContext _tenant;

    public UpdateProductionRoutingHandler(
        IProductionRoutingRepository routings,
        IWorkCenterRepository workCenters,
        ITenantContext tenant)
    {
        _routings = routings;
        _workCenters = workCenters;
        _tenant = tenant;
    }

    public async Task<ProductionRoutingDto> Handle(UpdateProductionRoutingCommand c, CancellationToken ct)
    {
        var tenantId = _tenant.RequireTenantId();
        var routing = await _routings.GetByIdAsync(tenantId, c.Id, ct)
            ?? throw new RoutingNotFoundException(c.Id);
        if (await _routings.CodeExistsAsync(tenantId, c.Code.Trim(), c.Id, ct))
        {
            throw new RoutingCodeConflictException(c.Code.Trim());
        }
        routing.UpdateHeader(c.Code, c.Name, c.Description);
        return await RoutingDtoAssembler.BuildAsync(routing, _workCenters, ct);
    }
}

public class SetRoutingStepsHandler : IRequestHandler<SetRoutingStepsCommand, ProductionRoutingDto>
{
    private readonly IProductionRoutingRepository _routings;
    private readonly IWorkCenterRepository _workCenters;
    private readonly ITenantContext _tenant;

    public SetRoutingStepsHandler(
        IProductionRoutingRepository routings,
        IWorkCenterRepository workCenters,
        ITenantContext tenant)
    {
        _routings = routings;
        _workCenters = workCenters;
        _tenant = tenant;
    }

    public async Task<ProductionRoutingDto> Handle(SetRoutingStepsCommand c, CancellationToken ct)
    {
        var tenantId = _tenant.RequireTenantId();
        var routing = await _routings.GetByIdAsync(tenantId, c.RoutingId, ct)
            ?? throw new RoutingNotFoundException(c.RoutingId);

        var wcIds = c.Steps.Select(s => s.WorkCenterId).Distinct().ToList();
        var activeIds = (await _workCenters.GetActiveIdsAsync(wcIds, ct)).ToHashSet();
        var missing = wcIds.FirstOrDefault(id => !activeIds.Contains(id));
        if (missing != Guid.Empty)
        {
            throw new WorkCenterNotFoundException(missing);
        }

        var oldSteps = routing.Steps.ToList();
        routing.ReplaceSteps(c.Steps.Select(ToDraft).ToList());
        var newSteps = routing.Steps.ToList();
        _routings.RemoveSteps(oldSteps);
        await _routings.AddStepsAsync(newSteps, ct);

        return await RoutingDtoAssembler.BuildAsync(routing, _workCenters, ct);
    }

    private static RoutingStepDraft ToDraft(RoutingStepInput s) =>
        new(s.StepNumber, s.WorkCenterId, s.OperationName, s.OperationType, s.SetupTimeMinutes,
            s.RunTimeMinutesPerUnit, s.RunTimeMinutesPerSqm, s.ScrapPercentage, s.Instructions, s.IsOptional);
}

public class ActivateRoutingHandler : IRequestHandler<ActivateRoutingCommand, ProductionRoutingDto>
{
    private readonly IProductionRoutingRepository _routings;
    private readonly IWorkCenterRepository _workCenters;
    private readonly ITenantContext _tenant;

    public ActivateRoutingHandler(
        IProductionRoutingRepository routings,
        IWorkCenterRepository workCenters,
        ITenantContext tenant)
    {
        _routings = routings;
        _workCenters = workCenters;
        _tenant = tenant;
    }

    public async Task<ProductionRoutingDto> Handle(ActivateRoutingCommand c, CancellationToken ct)
    {
        var tenantId = _tenant.RequireTenantId();
        var routing = await _routings.GetByIdAsync(tenantId, c.Id, ct)
            ?? throw new RoutingNotFoundException(c.Id);
        routing.Activate();
        return await RoutingDtoAssembler.BuildAsync(routing, _workCenters, ct);
    }
}

public class ArchiveRoutingHandler : IRequestHandler<ArchiveRoutingCommand, ProductionRoutingDto>
{
    private readonly IProductionRoutingRepository _routings;
    private readonly IWorkCenterRepository _workCenters;
    private readonly ITenantContext _tenant;

    public ArchiveRoutingHandler(
        IProductionRoutingRepository routings,
        IWorkCenterRepository workCenters,
        ITenantContext tenant)
    {
        _routings = routings;
        _workCenters = workCenters;
        _tenant = tenant;
    }

    public async Task<ProductionRoutingDto> Handle(ArchiveRoutingCommand c, CancellationToken ct)
    {
        var tenantId = _tenant.RequireTenantId();
        var routing = await _routings.GetByIdAsync(tenantId, c.Id, ct)
            ?? throw new RoutingNotFoundException(c.Id);
        routing.Archive();
        return await RoutingDtoAssembler.BuildAsync(routing, _workCenters, ct);
    }
}

public class RestoreRoutingToDraftHandler
    : IRequestHandler<RestoreRoutingToDraftCommand, ProductionRoutingDto>
{
    private readonly IProductionRoutingRepository _routings;
    private readonly IWorkCenterRepository _workCenters;
    private readonly ITenantContext _tenant;

    public RestoreRoutingToDraftHandler(
        IProductionRoutingRepository routings,
        IWorkCenterRepository workCenters,
        ITenantContext tenant)
    {
        _routings = routings;
        _workCenters = workCenters;
        _tenant = tenant;
    }

    public async Task<ProductionRoutingDto> Handle(RestoreRoutingToDraftCommand c, CancellationToken ct)
    {
        var tenantId = _tenant.RequireTenantId();
        var routing = await _routings.GetByIdAsync(tenantId, c.Id, ct)
            ?? throw new RoutingNotFoundException(c.Id);
        routing.RestoreToDraft();
        return await RoutingDtoAssembler.BuildAsync(routing, _workCenters, ct);
    }
}

public class DeleteProductionRoutingHandler : IRequestHandler<DeleteProductionRoutingCommand, Unit>
{
    private readonly IProductionRoutingRepository _routings;
    private readonly ITenantContext _tenant;

    public DeleteProductionRoutingHandler(IProductionRoutingRepository routings, ITenantContext tenant)
    {
        _routings = routings;
        _tenant = tenant;
    }

    public async Task<Unit> Handle(DeleteProductionRoutingCommand c, CancellationToken ct)
    {
        var tenantId = _tenant.RequireTenantId();
        var routing = await _routings.GetByIdAsync(tenantId, c.Id, ct)
            ?? throw new RoutingNotFoundException(c.Id);
        if (routing.Status != RoutingStatus.Draft)
        {
            throw new RoutingNotDeletableException();
        }
        _routings.Remove(routing);
        return Unit.Value;
    }
}

public class AssignRoutingToProductHandler : IRequestHandler<AssignRoutingToProductCommand, Unit>
{
    private readonly IProductRepository _products;
    private readonly IProductionRoutingRepository _routings;
    private readonly ITenantContext _tenant;

    public AssignRoutingToProductHandler(
        IProductRepository products,
        IProductionRoutingRepository routings,
        ITenantContext tenant)
    {
        _products = products;
        _routings = routings;
        _tenant = tenant;
    }

    public async Task<Unit> Handle(AssignRoutingToProductCommand c, CancellationToken ct)
    {
        var tenantId = _tenant.RequireTenantId();
        var product = await _products.GetByIdAsync(c.ProductId, ct)
            ?? throw new ProductNotFoundException();

        if (c.RoutingId is Guid routingId)
        {
            var routing = await _routings.GetByIdReadAsync(tenantId, routingId, ct)
                ?? throw new RoutingNotFoundException(routingId);
            if (routing.Status != RoutingStatus.Active)
            {
                throw new RoutingNotActiveException();
            }
        }

        product.AssignRouting(c.RoutingId);
        return Unit.Value;
    }
}
