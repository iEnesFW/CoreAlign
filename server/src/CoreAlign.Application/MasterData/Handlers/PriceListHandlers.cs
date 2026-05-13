using CoreAlign.Application.MasterData.Commands;
using CoreAlign.Application.MasterData.DTOs;
using CoreAlign.Application.MasterData.Mapping;
using CoreAlign.Application.MasterData.Queries;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.MasterData.Handlers;

public class ListPriceListsHandler : IRequestHandler<ListPriceListsQuery, IReadOnlyList<PriceListDto>>
{
    private readonly IPriceListRepository _repo;
    public ListPriceListsHandler(IPriceListRepository repo) => _repo = repo;
    public async Task<IReadOnlyList<PriceListDto>> Handle(ListPriceListsQuery q, CancellationToken ct)
        => (await _repo.ListAsync(q.IsActive, ct)).Select(MasterDataMapper.ToDto).ToList();
}

public class GetPriceListByIdHandler : IRequestHandler<GetPriceListByIdQuery, PriceListDto?>
{
    private readonly IPriceListRepository _repo;
    public GetPriceListByIdHandler(IPriceListRepository repo) => _repo = repo;
    public async Task<PriceListDto?> Handle(GetPriceListByIdQuery q, CancellationToken ct)
    {
        var e = await _repo.GetByIdAsync(q.Id, ct);
        return e is null ? null : MasterDataMapper.ToDto(e);
    }
}

public class CreatePriceListHandler : IRequestHandler<CreatePriceListCommand, PriceListDto>
{
    private readonly IPriceListRepository _repo;
    private readonly IUnitOfWork _uow;
    public CreatePriceListHandler(IPriceListRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }
    public async Task<PriceListDto> Handle(CreatePriceListCommand c, CancellationToken ct)
    {
        var entity = new PriceList(c.Code, c.Name, c.Currency, c.IsTaxInclusive, c.ValidFromUtc, c.ValidUntilUtc, c.IsDefault, c.Description);
        await _repo.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);
        return MasterDataMapper.ToDto(entity);
    }
}

public class UpdatePriceListHandler : IRequestHandler<UpdatePriceListCommand, PriceListDto>
{
    private readonly IPriceListRepository _repo;
    private readonly IUnitOfWork _uow;
    public UpdatePriceListHandler(IPriceListRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }
    public async Task<PriceListDto> Handle(UpdatePriceListCommand c, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(c.Id, ct) ?? throw new KeyNotFoundException("PriceList not found");
        entity.Update(c.Code, c.Name, c.Currency, c.IsTaxInclusive, c.ValidFromUtc, c.ValidUntilUtc, c.IsDefault, c.Description, c.IsActive);
        _repo.Update(entity);
        await _uow.SaveChangesAsync(ct);
        return MasterDataMapper.ToDto(entity);
    }
}

public class DeletePriceListHandler : IRequestHandler<DeletePriceListCommand, bool>
{
    private readonly IPriceListRepository _repo;
    private readonly IUnitOfWork _uow;
    public DeletePriceListHandler(IPriceListRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }
    public async Task<bool> Handle(DeletePriceListCommand c, CancellationToken ct)
    {
        var e = await _repo.GetByIdAsync(c.Id, ct);
        if (e is null) return false;
        _repo.Remove(e);
        await _uow.SaveChangesAsync(ct);
        return true;
    }
}
