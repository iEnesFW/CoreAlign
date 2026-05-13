using CoreAlign.Application.MasterData.Commands;
using CoreAlign.Application.MasterData.DTOs;
using CoreAlign.Application.MasterData.Mapping;
using CoreAlign.Application.MasterData.Queries;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.MasterData.Handlers;

public class ListCustomerGroupsHandler : IRequestHandler<ListCustomerGroupsQuery, IReadOnlyList<CustomerGroupDto>>
{
    private readonly ICustomerGroupRepository _repo;
    public ListCustomerGroupsHandler(ICustomerGroupRepository repo) => _repo = repo;
    public async Task<IReadOnlyList<CustomerGroupDto>> Handle(ListCustomerGroupsQuery q, CancellationToken ct)
        => (await _repo.ListAsync(q.IsActive, ct)).Select(MasterDataMapper.ToDto).ToList();
}

public class GetCustomerGroupByIdHandler : IRequestHandler<GetCustomerGroupByIdQuery, CustomerGroupDto?>
{
    private readonly ICustomerGroupRepository _repo;
    public GetCustomerGroupByIdHandler(ICustomerGroupRepository repo) => _repo = repo;
    public async Task<CustomerGroupDto?> Handle(GetCustomerGroupByIdQuery q, CancellationToken ct)
    {
        var e = await _repo.GetByIdAsync(q.Id, ct);
        return e is null ? null : MasterDataMapper.ToDto(e);
    }
}

public class CreateCustomerGroupHandler : IRequestHandler<CreateCustomerGroupCommand, CustomerGroupDto>
{
    private readonly ICustomerGroupRepository _repo;
    private readonly IUnitOfWork _uow;
    public CreateCustomerGroupHandler(ICustomerGroupRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }
    public async Task<CustomerGroupDto> Handle(CreateCustomerGroupCommand c, CancellationToken ct)
    {
        var entity = new CustomerGroup(c.Code, c.Name, c.Description);
        entity.Update(c.Code, c.Name, c.Description, c.DefaultPriceListId, c.DefaultDiscountPercent, true);
        await _repo.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);
        return MasterDataMapper.ToDto(entity);
    }
}

public class UpdateCustomerGroupHandler : IRequestHandler<UpdateCustomerGroupCommand, CustomerGroupDto>
{
    private readonly ICustomerGroupRepository _repo;
    private readonly IUnitOfWork _uow;
    public UpdateCustomerGroupHandler(ICustomerGroupRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }
    public async Task<CustomerGroupDto> Handle(UpdateCustomerGroupCommand c, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(c.Id, ct) ?? throw new KeyNotFoundException("CustomerGroup not found");
        entity.Update(c.Code, c.Name, c.Description, c.DefaultPriceListId, c.DefaultDiscountPercent, c.IsActive);
        _repo.Update(entity);
        await _uow.SaveChangesAsync(ct);
        return MasterDataMapper.ToDto(entity);
    }
}

public class DeleteCustomerGroupHandler : IRequestHandler<DeleteCustomerGroupCommand, bool>
{
    private readonly ICustomerGroupRepository _repo;
    private readonly IUnitOfWork _uow;
    public DeleteCustomerGroupHandler(ICustomerGroupRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }
    public async Task<bool> Handle(DeleteCustomerGroupCommand c, CancellationToken ct)
    {
        var e = await _repo.GetByIdAsync(c.Id, ct);
        if (e is null) return false;
        _repo.Remove(e);
        await _uow.SaveChangesAsync(ct);
        return true;
    }
}
