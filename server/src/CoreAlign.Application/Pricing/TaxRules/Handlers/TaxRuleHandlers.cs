using CoreAlign.Application.Pricing.Common;
using CoreAlign.Application.Pricing.TaxRules.Commands;
using CoreAlign.Application.Pricing.TaxRules.Queries;
using CoreAlign.Domain.Entities.Pricing;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Pricing.TaxRules.Handlers;

public class ListTaxRulesHandler : IRequestHandler<ListTaxRulesQuery, IReadOnlyList<TaxRuleDto>>
{
    private readonly ITaxRuleRepository _repo;
    public ListTaxRulesHandler(ITaxRuleRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<TaxRuleDto>> Handle(ListTaxRulesQuery q, CancellationToken ct)
        => (await _repo.ListAsync(q.IsActive, ct)).Select(PricingMappers.ToDto).ToList();
}

public class GetTaxRuleByIdHandler : IRequestHandler<GetTaxRuleByIdQuery, TaxRuleDto?>
{
    private readonly ITaxRuleRepository _repo;
    public GetTaxRuleByIdHandler(ITaxRuleRepository repo) => _repo = repo;

    public async Task<TaxRuleDto?> Handle(GetTaxRuleByIdQuery q, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(q.Id, ct);
        return entity is null ? null : PricingMappers.ToDto(entity);
    }
}

public class CreateTaxRuleHandler : IRequestHandler<CreateTaxRuleCommand, TaxRuleDto>
{
    private readonly ITaxRuleRepository _repo;
    private readonly IUnitOfWork _uow;

    public CreateTaxRuleHandler(ITaxRuleRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<TaxRuleDto> Handle(CreateTaxRuleCommand c, CancellationToken ct)
    {
        var existing = await _repo.GetByCodeAsync(c.Code, ct);
        if (existing is not null)
        {
            throw new TaxRuleCodeConflictException(c.Code);
        }

        var rule = new TaxRule(
            c.Code,
            c.Name,
            c.Scope,
            c.RatePercent,
            c.RegionCode,
            c.ProductClass,
            c.ProductCategoryId,
            c.ProductId,
            c.FallbackTaxRateId,
            c.ValidFromUtc,
            c.ValidUntilUtc,
            c.Priority,
            c.Description);
        await _repo.AddAsync(rule, ct);
        await _uow.SaveChangesAsync(ct);
        return PricingMappers.ToDto(rule);
    }
}

public class UpdateTaxRuleHandler : IRequestHandler<UpdateTaxRuleCommand, TaxRuleDto>
{
    private readonly ITaxRuleRepository _repo;
    private readonly IUnitOfWork _uow;

    public UpdateTaxRuleHandler(ITaxRuleRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<TaxRuleDto> Handle(UpdateTaxRuleCommand c, CancellationToken ct)
    {
        var rule = await _repo.GetByIdAsync(c.Id, ct)
            ?? throw new TaxRuleNotFoundException(c.Id);
        rule.Update(
            c.Name,
            c.Scope,
            c.RatePercent,
            c.RegionCode,
            c.ProductClass,
            c.ProductCategoryId,
            c.ProductId,
            c.FallbackTaxRateId,
            c.ValidFromUtc,
            c.ValidUntilUtc,
            c.Priority,
            c.IsActive,
            c.Description);
        _repo.Update(rule);
        await _uow.SaveChangesAsync(ct);
        return PricingMappers.ToDto(rule);
    }
}

public class DeleteTaxRuleHandler : IRequestHandler<DeleteTaxRuleCommand, bool>
{
    private readonly ITaxRuleRepository _repo;
    private readonly IUnitOfWork _uow;

    public DeleteTaxRuleHandler(ITaxRuleRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<bool> Handle(DeleteTaxRuleCommand c, CancellationToken ct)
    {
        var rule = await _repo.GetByIdAsync(c.Id, ct);
        if (rule is null) return false;
        _repo.Remove(rule);
        await _uow.SaveChangesAsync(ct);
        return true;
    }
}
