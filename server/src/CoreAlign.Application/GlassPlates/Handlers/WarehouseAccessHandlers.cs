using CoreAlign.Application.GlassPlates.Commands;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.GlassPlates.Handlers;

public class AssignUserWarehousesHandler : IRequestHandler<AssignUserWarehousesCommand, IReadOnlyList<Guid>>
{
    private readonly IUserWarehouseAccessRepository _access;
    private readonly ITenantContext _tenant;

    public AssignUserWarehousesHandler(IUserWarehouseAccessRepository access, ITenantContext tenant)
    {
        _access = access;
        _tenant = tenant;
    }

    public async Task<IReadOnlyList<Guid>> Handle(AssignUserWarehousesCommand c, CancellationToken ct)
    {
        var tenantId = _tenant.RequireTenantId();
        var existing = await _access.ListByUserAsync(tenantId, c.UserId, ct);
        var existingIds = existing.Select(a => a.WarehouseId).ToHashSet();
        var desired = c.WarehouseIds.Distinct().ToHashSet();

        // WHY: diff (not remove-all-then-re-add) so a kept grant never collides with the
        // (tenant, user, warehouse) unique index within one SaveChanges.
        var toRemove = existing.Where(a => !desired.Contains(a.WarehouseId)).ToList();
        if (toRemove.Count > 0)
        {
            _access.RemoveRange(toRemove);
        }

        foreach (var warehouseId in desired.Where(w => !existingIds.Contains(w)))
        {
            await _access.AddAsync(new UserWarehouseAccess(c.UserId, warehouseId, c.GrantedByUserId), ct);
        }

        return desired.ToList();
    }
}

public class GetUserWarehouseAccessHandler : IRequestHandler<GetUserWarehouseAccessQuery, IReadOnlyList<Guid>>
{
    private readonly IUserWarehouseAccessRepository _access;
    private readonly ITenantContext _tenant;

    public GetUserWarehouseAccessHandler(IUserWarehouseAccessRepository access, ITenantContext tenant)
    {
        _access = access;
        _tenant = tenant;
    }

    public async Task<IReadOnlyList<Guid>> Handle(GetUserWarehouseAccessQuery q, CancellationToken ct)
    {
        var tenantId = _tenant.RequireTenantId();
        return await _access.GetWarehouseIdsByUserAsync(tenantId, q.UserId, ct);
    }
}

public class SetGlassPlateTrackingHandler : IRequestHandler<SetGlassPlateTrackingCommand, Guid>
{
    private readonly IProductRepository _products;

    public SetGlassPlateTrackingHandler(IProductRepository products) => _products = products;

    public async Task<Guid> Handle(SetGlassPlateTrackingCommand c, CancellationToken ct)
    {
        var product = await _products.GetByIdAsync(c.ProductId, ct)
            ?? throw new ProductNotFoundException();

        product.SetPlateTracking(
            c.IsPlateTracked,
            c.MinRemnantAreaMm2,
            c.MinRemnantWidthMm,
            c.MinRemnantHeightMm,
            c.MinPlateCount,
            c.StandardWidthMm,
            c.StandardHeightMm);

        return product.Id;
    }
}
