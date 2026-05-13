using CoreAlign.Application.MasterData.Commands;
using CoreAlign.Application.MasterData.DTOs;
using CoreAlign.Application.MasterData.Mapping;
using CoreAlign.Application.MasterData.Queries;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.MasterData.Handlers;

public class ListBrandsHandler : IRequestHandler<ListBrandsQuery, IReadOnlyList<BrandDto>>
{
    private readonly IBrandRepository _repo;
    public ListBrandsHandler(IBrandRepository repo) => _repo = repo;
    public async Task<IReadOnlyList<BrandDto>> Handle(ListBrandsQuery q, CancellationToken ct)
        => (await _repo.ListAsync(q.IsActive, ct)).Select(MasterDataMapper.ToDto).ToList();
}

public class GetBrandByIdHandler : IRequestHandler<GetBrandByIdQuery, BrandDto?>
{
    private readonly IBrandRepository _repo;
    public GetBrandByIdHandler(IBrandRepository repo) => _repo = repo;
    public async Task<BrandDto?> Handle(GetBrandByIdQuery q, CancellationToken ct)
    {
        var b = await _repo.GetByIdAsync(q.Id, ct);
        return b is null ? null : MasterDataMapper.ToDto(b);
    }
}

public class CreateBrandHandler : IRequestHandler<CreateBrandCommand, BrandDto>
{
    private readonly IBrandRepository _repo;
    private readonly IUnitOfWork _uow;
    public CreateBrandHandler(IBrandRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }
    public async Task<BrandDto> Handle(CreateBrandCommand c, CancellationToken ct)
    {
        var entity = new Brand(c.Code, c.Name, c.Description);
        await _repo.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);
        return MasterDataMapper.ToDto(entity);
    }
}

public class UpdateBrandHandler : IRequestHandler<UpdateBrandCommand, BrandDto>
{
    private readonly IBrandRepository _repo;
    private readonly IUnitOfWork _uow;
    public UpdateBrandHandler(IBrandRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }
    public async Task<BrandDto> Handle(UpdateBrandCommand c, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(c.Id, ct) ?? throw new KeyNotFoundException("Brand not found");
        entity.Update(c.Code, c.Name, c.Description, c.IsActive);
        _repo.Update(entity);
        await _uow.SaveChangesAsync(ct);
        return MasterDataMapper.ToDto(entity);
    }
}

public class DeleteBrandHandler : IRequestHandler<DeleteBrandCommand, bool>
{
    private readonly IBrandRepository _repo;
    private readonly IUnitOfWork _uow;
    public DeleteBrandHandler(IBrandRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }
    public async Task<bool> Handle(DeleteBrandCommand c, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(c.Id, ct);
        if (entity is null) return false;
        _repo.Remove(entity);
        await _uow.SaveChangesAsync(ct);
        return true;
    }
}
