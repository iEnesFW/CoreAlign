using CoreAlign.Application.MasterData.Commands;
using CoreAlign.Application.MasterData.DTOs;
using CoreAlign.Application.MasterData.Mapping;
using CoreAlign.Application.MasterData.Queries;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.MasterData.Handlers;

public class ListUnitsOfMeasureHandler : IRequestHandler<ListUnitsOfMeasureQuery, IReadOnlyList<UnitOfMeasureDto>>
{
    private readonly IUnitOfMeasureRepository _repo;
    public ListUnitsOfMeasureHandler(IUnitOfMeasureRepository repo) => _repo = repo;
    public async Task<IReadOnlyList<UnitOfMeasureDto>> Handle(ListUnitsOfMeasureQuery q, CancellationToken ct)
        => (await _repo.ListAsync(q.IsActive, ct)).Select(MasterDataMapper.ToDto).ToList();
}

public class GetUnitOfMeasureByIdHandler : IRequestHandler<GetUnitOfMeasureByIdQuery, UnitOfMeasureDto?>
{
    private readonly IUnitOfMeasureRepository _repo;
    public GetUnitOfMeasureByIdHandler(IUnitOfMeasureRepository repo) => _repo = repo;
    public async Task<UnitOfMeasureDto?> Handle(GetUnitOfMeasureByIdQuery q, CancellationToken ct)
    {
        var e = await _repo.GetByIdAsync(q.Id, ct);
        return e is null ? null : MasterDataMapper.ToDto(e);
    }
}

public class CreateUnitOfMeasureHandler : IRequestHandler<CreateUnitOfMeasureCommand, UnitOfMeasureDto>
{
    private readonly IUnitOfMeasureRepository _repo;
    private readonly IUnitOfWork _uow;
    public CreateUnitOfMeasureHandler(IUnitOfMeasureRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }
    public async Task<UnitOfMeasureDto> Handle(CreateUnitOfMeasureCommand c, CancellationToken ct)
    {
        var entity = new UnitOfMeasure(c.Code, c.Name, c.Symbol, c.BaseUomId, c.ConversionFactor, c.DecimalPlaces);
        await _repo.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);
        return MasterDataMapper.ToDto(entity);
    }
}

public class UpdateUnitOfMeasureHandler : IRequestHandler<UpdateUnitOfMeasureCommand, UnitOfMeasureDto>
{
    private readonly IUnitOfMeasureRepository _repo;
    private readonly IUnitOfWork _uow;
    public UpdateUnitOfMeasureHandler(IUnitOfMeasureRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }
    public async Task<UnitOfMeasureDto> Handle(UpdateUnitOfMeasureCommand c, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(c.Id, ct) ?? throw new KeyNotFoundException("UoM not found");
        entity.Update(c.Code, c.Name, c.Symbol, c.BaseUomId, c.ConversionFactor, c.DecimalPlaces, c.IsActive);
        _repo.Update(entity);
        await _uow.SaveChangesAsync(ct);
        return MasterDataMapper.ToDto(entity);
    }
}

public class DeleteUnitOfMeasureHandler : IRequestHandler<DeleteUnitOfMeasureCommand, bool>
{
    private readonly IUnitOfMeasureRepository _repo;
    private readonly IUnitOfWork _uow;
    public DeleteUnitOfMeasureHandler(IUnitOfMeasureRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }
    public async Task<bool> Handle(DeleteUnitOfMeasureCommand c, CancellationToken ct)
    {
        var e = await _repo.GetByIdAsync(c.Id, ct);
        if (e is null) return false;
        _repo.Remove(e);
        await _uow.SaveChangesAsync(ct);
        return true;
    }
}
