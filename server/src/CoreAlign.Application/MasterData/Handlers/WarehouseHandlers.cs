using CoreAlign.Application.MasterData.Commands;
using CoreAlign.Application.MasterData.DTOs;
using CoreAlign.Application.MasterData.Mapping;
using CoreAlign.Application.MasterData.Queries;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.MasterData.Handlers;

public class ListWarehousesHandler : IRequestHandler<ListWarehousesQuery, IReadOnlyList<WarehouseDto>>
{
    private readonly IWarehouseRepository _repo;
    public ListWarehousesHandler(IWarehouseRepository repo) => _repo = repo;
    public async Task<IReadOnlyList<WarehouseDto>> Handle(ListWarehousesQuery q, CancellationToken ct)
        => (await _repo.ListAsync(q.IsActive, ct)).Select(MasterDataMapper.ToDto).ToList();
}

public class GetWarehouseByIdHandler : IRequestHandler<GetWarehouseByIdQuery, WarehouseDto?>
{
    private readonly IWarehouseRepository _repo;
    public GetWarehouseByIdHandler(IWarehouseRepository repo) => _repo = repo;
    public async Task<WarehouseDto?> Handle(GetWarehouseByIdQuery q, CancellationToken ct)
    {
        var e = await _repo.GetByIdAsync(q.Id, ct);
        return e is null ? null : MasterDataMapper.ToDto(e);
    }
}

public class CreateWarehouseHandler : IRequestHandler<CreateWarehouseCommand, WarehouseDto>
{
    private readonly IWarehouseRepository _repo;
    private readonly IUnitOfWork _uow;
    public CreateWarehouseHandler(IWarehouseRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }
    public async Task<WarehouseDto> Handle(CreateWarehouseCommand c, CancellationToken ct)
    {
        var entity = new Warehouse(c.Code, c.Name, c.Type, c.IsDefault);
        await _repo.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);
        return MasterDataMapper.ToDto(entity);
    }
}

public class UpdateWarehouseHandler : IRequestHandler<UpdateWarehouseCommand, WarehouseDto>
{
    private readonly IWarehouseRepository _repo;
    private readonly IUnitOfWork _uow;
    public UpdateWarehouseHandler(IWarehouseRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }
    public async Task<WarehouseDto> Handle(UpdateWarehouseCommand c, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(c.Id, ct) ?? throw new KeyNotFoundException("Warehouse not found");
        entity.Update(c.Code, c.Name, c.Type, c.AddressLine1, c.AddressLine2, c.City, c.State, c.PostalCode, c.Country, c.Phone, c.ManagerUserId, c.IsDefault, c.IsActive);
        _repo.Update(entity);
        await _uow.SaveChangesAsync(ct);
        return MasterDataMapper.ToDto(entity);
    }
}

public class DeleteWarehouseHandler : IRequestHandler<DeleteWarehouseCommand, bool>
{
    private readonly IWarehouseRepository _repo;
    private readonly IUnitOfWork _uow;
    public DeleteWarehouseHandler(IWarehouseRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }
    public async Task<bool> Handle(DeleteWarehouseCommand c, CancellationToken ct)
    {
        var e = await _repo.GetByIdAsync(c.Id, ct);
        if (e is null) return false;
        _repo.Remove(e);
        await _uow.SaveChangesAsync(ct);
        return true;
    }
}
