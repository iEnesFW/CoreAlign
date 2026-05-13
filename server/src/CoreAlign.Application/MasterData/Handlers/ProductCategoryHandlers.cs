using CoreAlign.Application.MasterData.Commands;
using CoreAlign.Application.MasterData.DTOs;
using CoreAlign.Application.MasterData.Mapping;
using CoreAlign.Application.MasterData.Queries;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.MasterData.Handlers;

public class ListProductCategoriesHandler : IRequestHandler<ListProductCategoriesQuery, IReadOnlyList<ProductCategoryDto>>
{
    private readonly IProductCategoryRepository _repo;
    public ListProductCategoriesHandler(IProductCategoryRepository repo) => _repo = repo;
    public async Task<IReadOnlyList<ProductCategoryDto>> Handle(ListProductCategoriesQuery q, CancellationToken ct)
        => (await _repo.ListAsync(q.IsActive, ct)).Select(MasterDataMapper.ToDto).ToList();
}

public class GetProductCategoryByIdHandler : IRequestHandler<GetProductCategoryByIdQuery, ProductCategoryDto?>
{
    private readonly IProductCategoryRepository _repo;
    public GetProductCategoryByIdHandler(IProductCategoryRepository repo) => _repo = repo;
    public async Task<ProductCategoryDto?> Handle(GetProductCategoryByIdQuery q, CancellationToken ct)
    {
        var e = await _repo.GetByIdAsync(q.Id, ct);
        return e is null ? null : MasterDataMapper.ToDto(e);
    }
}

public class CreateProductCategoryHandler : IRequestHandler<CreateProductCategoryCommand, ProductCategoryDto>
{
    private readonly IProductCategoryRepository _repo;
    private readonly IUnitOfWork _uow;
    public CreateProductCategoryHandler(IProductCategoryRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }
    public async Task<ProductCategoryDto> Handle(CreateProductCategoryCommand c, CancellationToken ct)
    {
        var entity = new ProductCategory(c.Code, c.Name, c.ParentCategoryId, c.Description);
        await _repo.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);
        return MasterDataMapper.ToDto(entity);
    }
}

public class UpdateProductCategoryHandler : IRequestHandler<UpdateProductCategoryCommand, ProductCategoryDto>
{
    private readonly IProductCategoryRepository _repo;
    private readonly IUnitOfWork _uow;
    public UpdateProductCategoryHandler(IProductCategoryRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }
    public async Task<ProductCategoryDto> Handle(UpdateProductCategoryCommand c, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(c.Id, ct) ?? throw new KeyNotFoundException("Category not found");
        entity.Update(c.Code, c.Name, c.ParentCategoryId, c.Description, c.IsActive);
        _repo.Update(entity);
        await _uow.SaveChangesAsync(ct);
        return MasterDataMapper.ToDto(entity);
    }
}

public class DeleteProductCategoryHandler : IRequestHandler<DeleteProductCategoryCommand, bool>
{
    private readonly IProductCategoryRepository _repo;
    private readonly IUnitOfWork _uow;
    public DeleteProductCategoryHandler(IProductCategoryRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }
    public async Task<bool> Handle(DeleteProductCategoryCommand c, CancellationToken ct)
    {
        var e = await _repo.GetByIdAsync(c.Id, ct);
        if (e is null) return false;
        _repo.Remove(e);
        await _uow.SaveChangesAsync(ct);
        return true;
    }
}
