using CoreAlign.Application.Common;
using CoreAlign.Application.GlassPlates.DTOs;
using CoreAlign.Application.GlassPlates.Mapping;
using CoreAlign.Application.GlassPlates.Queries;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.GlassPlates.Handlers;

public class ListGlassPlatesHandler : IRequestHandler<ListGlassPlatesQuery, IReadOnlyList<GlassPlateDto>>
{
    private readonly IGlassPlateRepository _plates;
    private readonly IWarehouseAccessScope _scope;
    private readonly ITenantContext _tenant;

    public ListGlassPlatesHandler(IGlassPlateRepository plates, IWarehouseAccessScope scope, ITenantContext tenant)
    {
        _plates = plates;
        _scope = scope;
        _tenant = tenant;
    }

    public async Task<IReadOnlyList<GlassPlateDto>> Handle(ListGlassPlatesQuery q, CancellationToken ct)
    {
        var tenantId = _tenant.RequireTenantId();
        var access = await _scope.GetAllowedWarehouseIdsAsync(ct);
        IReadOnlyCollection<Guid>? allowed = access.IsUnrestricted ? null : access.AllowedWarehouseIds;

        var items = await _plates.ListAsync(
            tenantId, q.ProductId, q.WarehouseId, q.StorageLocationId, q.Status, q.Kind,
            allowed, Math.Clamp(q.Take, 1, 500), ct);
        return items.Select(GlassPlateMapper.ToDto).ToList();
    }
}

public class UsablePlatesForCutHandler : IRequestHandler<UsablePlatesForCutQuery, IReadOnlyList<GlassPlateDto>>
{
    private readonly IGlassPlateRepository _plates;
    private readonly IWarehouseAccessScope _scope;
    private readonly ITenantContext _tenant;

    public UsablePlatesForCutHandler(IGlassPlateRepository plates, IWarehouseAccessScope scope, ITenantContext tenant)
    {
        _plates = plates;
        _scope = scope;
        _tenant = tenant;
    }

    public async Task<IReadOnlyList<GlassPlateDto>> Handle(UsablePlatesForCutQuery q, CancellationToken ct)
    {
        var tenantId = _tenant.RequireTenantId();
        var access = await _scope.GetAllowedWarehouseIdsAsync(ct);

        IReadOnlyCollection<Guid>? allowed;
        if (access.IsUnrestricted)
        {
            allowed = q.WarehouseId.HasValue ? new[] { q.WarehouseId.Value } : null;
        }
        else
        {
            allowed = q.WarehouseId.HasValue
                ? access.AllowedWarehouseIds.Where(id => id == q.WarehouseId.Value).ToList()
                : access.AllowedWarehouseIds;
        }

        var items = await _plates.FindUsableForCutAsync(
            tenantId, q.ProductId, q.WidthMm, q.HeightMm, allowed, Math.Clamp(q.Take, 1, 100), ct);
        return items.Select(GlassPlateMapper.ToDto).ToList();
    }
}

public class LowStockPlatesHandler : IRequestHandler<LowStockPlatesQuery, IReadOnlyList<LowStockPlateDto>>
{
    private readonly IGlassPlateRepository _plates;
    private readonly IWarehouseAccessScope _scope;
    private readonly ITenantContext _tenant;

    public LowStockPlatesHandler(IGlassPlateRepository plates, IWarehouseAccessScope scope, ITenantContext tenant)
    {
        _plates = plates;
        _scope = scope;
        _tenant = tenant;
    }

    public async Task<IReadOnlyList<LowStockPlateDto>> Handle(LowStockPlatesQuery q, CancellationToken ct)
    {
        var tenantId = _tenant.RequireTenantId();
        var access = await _scope.GetAllowedWarehouseIdsAsync(ct);
        IReadOnlyCollection<Guid>? allowed = access.IsUnrestricted ? null : access.AllowedWarehouseIds;

        var rows = await _plates.GetLowStockAsync(tenantId, allowed, ct);
        return rows
            .Select(r => new LowStockPlateDto(
                r.ProductId, r.Sku, r.ProductName, r.WarehouseId, r.WarehouseName, r.AvailableCount, r.MinPlateCount))
            .ToList();
    }
}

public class GlassPlateWhereUsedHandler : IRequestHandler<GlassPlateWhereUsedQuery, IReadOnlyList<GlassPlateConsumptionDto>>
{
    private readonly IGlassPlateConsumptionRepository _consumptions;
    private readonly ITenantContext _tenant;

    public GlassPlateWhereUsedHandler(IGlassPlateConsumptionRepository consumptions, ITenantContext tenant)
    {
        _consumptions = consumptions;
        _tenant = tenant;
    }

    public async Task<IReadOnlyList<GlassPlateConsumptionDto>> Handle(GlassPlateWhereUsedQuery q, CancellationToken ct)
    {
        var tenantId = _tenant.RequireTenantId();
        var items = await _consumptions.ListByPlateAsync(tenantId, q.PlateId, ct);
        return items.Select(GlassPlateMapper.ToDto).ToList();
    }
}
