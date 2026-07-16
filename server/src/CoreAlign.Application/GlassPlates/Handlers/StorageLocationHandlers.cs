using CoreAlign.Application.GlassPlates.Commands;
using CoreAlign.Application.GlassPlates.DTOs;
using CoreAlign.Application.GlassPlates.Mapping;
using CoreAlign.Application.GlassPlates.Queries;
using CoreAlign.Domain.Entities.GlassPlates;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.GlassPlates.Handlers;

public class CreateStorageLocationHandler : IRequestHandler<CreateStorageLocationCommand, StorageLocationDto>
{
    private readonly IStorageLocationRepository _repo;
    private readonly ITenantContext _tenant;

    public CreateStorageLocationHandler(IStorageLocationRepository repo, ITenantContext tenant)
    {
        _repo = repo;
        _tenant = tenant;
    }

    public async Task<StorageLocationDto> Handle(CreateStorageLocationCommand c, CancellationToken ct)
    {
        var tenantId = _tenant.RequireTenantId();
        if (await _repo.CodeExistsAsync(tenantId, c.WarehouseId, c.Code.Trim(), null, ct))
        {
            throw new StorageLocationCodeConflictException(c.Code);
        }

        var location = new StorageLocation(c.WarehouseId, c.Code, c.Name, c.Kind, c.ParentLocationId, c.Notes);
        await _repo.AddAsync(location, ct);
        return GlassPlateMapper.ToDto(location);
    }
}

public class UpdateStorageLocationHandler : IRequestHandler<UpdateStorageLocationCommand, StorageLocationDto>
{
    private readonly IStorageLocationRepository _repo;
    private readonly ITenantContext _tenant;

    public UpdateStorageLocationHandler(IStorageLocationRepository repo, ITenantContext tenant)
    {
        _repo = repo;
        _tenant = tenant;
    }

    public async Task<StorageLocationDto> Handle(UpdateStorageLocationCommand c, CancellationToken ct)
    {
        var tenantId = _tenant.RequireTenantId();
        var location = await _repo.GetByIdAsync(tenantId, c.Id, ct)
            ?? throw new StorageLocationNotFoundException(c.Id);

        if (await _repo.CodeExistsAsync(tenantId, location.WarehouseId, c.Code.Trim(), c.Id, ct))
        {
            throw new StorageLocationCodeConflictException(c.Code);
        }

        location.Update(c.Code, c.Name, c.Kind, c.ParentLocationId, c.IsActive, c.Notes);
        return GlassPlateMapper.ToDto(location);
    }
}

public class ListStorageLocationsHandler : IRequestHandler<ListStorageLocationsQuery, IReadOnlyList<StorageLocationDto>>
{
    private readonly IStorageLocationRepository _repo;
    private readonly ITenantContext _tenant;

    public ListStorageLocationsHandler(IStorageLocationRepository repo, ITenantContext tenant)
    {
        _repo = repo;
        _tenant = tenant;
    }

    public async Task<IReadOnlyList<StorageLocationDto>> Handle(ListStorageLocationsQuery q, CancellationToken ct)
    {
        var tenantId = _tenant.RequireTenantId();
        var items = await _repo.ListAsync(tenantId, q.WarehouseId, ct);
        return items.Select(GlassPlateMapper.ToDto).ToList();
    }
}
