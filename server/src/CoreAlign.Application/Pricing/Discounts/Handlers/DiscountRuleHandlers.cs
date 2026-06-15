using CoreAlign.Application.Pricing.Common;
using CoreAlign.Application.Pricing.Discounts.Commands;
using CoreAlign.Application.Pricing.Discounts.Queries;
using CoreAlign.Domain.Entities.Pricing;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Pricing.Discounts.Handlers;

public class ListDiscountRulesHandler : IRequestHandler<ListDiscountRulesQuery, IReadOnlyList<DiscountRuleDto>>
{
    private readonly IPricingDiscountRuleRepository _repo;
    public ListDiscountRulesHandler(IPricingDiscountRuleRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<DiscountRuleDto>> Handle(ListDiscountRulesQuery q, CancellationToken ct)
        => (await _repo.ListAsync(q.IsActive, ct)).Select(PricingMappers.ToDto).ToList();
}

public class GetDiscountRuleByIdHandler : IRequestHandler<GetDiscountRuleByIdQuery, DiscountRuleDto?>
{
    private readonly IPricingDiscountRuleRepository _repo;
    public GetDiscountRuleByIdHandler(IPricingDiscountRuleRepository repo) => _repo = repo;

    public async Task<DiscountRuleDto?> Handle(GetDiscountRuleByIdQuery q, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(q.Id, ct);
        return entity is null ? null : PricingMappers.ToDto(entity);
    }
}

public class CreateDiscountRuleHandler : IRequestHandler<CreateDiscountRuleCommand, DiscountRuleDto>
{
    private readonly IPricingDiscountRuleRepository _repo;
    private readonly IUnitOfWork _uow;

    public CreateDiscountRuleHandler(IPricingDiscountRuleRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<DiscountRuleDto> Handle(CreateDiscountRuleCommand c, CancellationToken ct)
    {
        var existing = await _repo.GetByCodeAsync(c.Code, ct);
        if (existing is not null)
        {
            throw new DiscountRuleCodeConflictException(c.Code);
        }

        var rule = new DiscountRule(
            c.Code,
            c.Name,
            c.Scope,
            c.ValueType,
            c.Value,
            c.CustomerGroupId,
            c.ProductCategoryId,
            c.ProductId,
            c.ValidFromUtc,
            c.ValidUntilUtc,
            c.MinQuantity,
            c.Priority,
            c.Description);
        await _repo.AddAsync(rule, ct);
        await _uow.SaveChangesAsync(ct);
        return PricingMappers.ToDto(rule);
    }
}

public class UpdateDiscountRuleHandler : IRequestHandler<UpdateDiscountRuleCommand, DiscountRuleDto>
{
    private readonly IPricingDiscountRuleRepository _repo;
    private readonly IUnitOfWork _uow;

    public UpdateDiscountRuleHandler(IPricingDiscountRuleRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<DiscountRuleDto> Handle(UpdateDiscountRuleCommand c, CancellationToken ct)
    {
        var rule = await _repo.GetByIdAsync(c.Id, ct)
            ?? throw new DiscountRuleNotFoundException(c.Id);
        rule.Update(
            c.Name,
            c.Scope,
            c.ValueType,
            c.Value,
            c.CustomerGroupId,
            c.ProductCategoryId,
            c.ProductId,
            c.ValidFromUtc,
            c.ValidUntilUtc,
            c.MinQuantity,
            c.Priority,
            c.IsActive,
            c.Description);
        _repo.Update(rule);
        await _uow.SaveChangesAsync(ct);
        return PricingMappers.ToDto(rule);
    }
}

public class DeleteDiscountRuleHandler : IRequestHandler<DeleteDiscountRuleCommand, bool>
{
    private readonly IPricingDiscountRuleRepository _repo;
    private readonly IUnitOfWork _uow;

    public DeleteDiscountRuleHandler(IPricingDiscountRuleRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<bool> Handle(DeleteDiscountRuleCommand c, CancellationToken ct)
    {
        var rule = await _repo.GetByIdAsync(c.Id, ct);
        if (rule is null) return false;
        _repo.Remove(rule);
        await _uow.SaveChangesAsync(ct);
        return true;
    }
}
