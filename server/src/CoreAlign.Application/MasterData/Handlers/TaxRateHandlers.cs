using CoreAlign.Application.MasterData.Commands;
using CoreAlign.Application.MasterData.DTOs;
using CoreAlign.Application.MasterData.Mapping;
using CoreAlign.Application.MasterData.Queries;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.MasterData.Handlers;

public class ListTaxRatesHandler : IRequestHandler<ListTaxRatesQuery, IReadOnlyList<TaxRateDto>>
{
    private readonly ITaxRateRepository _repo;
    public ListTaxRatesHandler(ITaxRateRepository repo) => _repo = repo;
    public async Task<IReadOnlyList<TaxRateDto>> Handle(ListTaxRatesQuery q, CancellationToken ct)
        => (await _repo.ListAsync(q.IsActive, q.IsWithholding, ct)).Select(MasterDataMapper.ToDto).ToList();
}

public class GetTaxRateByIdHandler : IRequestHandler<GetTaxRateByIdQuery, TaxRateDto?>
{
    private readonly ITaxRateRepository _repo;
    public GetTaxRateByIdHandler(ITaxRateRepository repo) => _repo = repo;
    public async Task<TaxRateDto?> Handle(GetTaxRateByIdQuery q, CancellationToken ct)
    {
        var e = await _repo.GetByIdAsync(q.Id, ct);
        return e is null ? null : MasterDataMapper.ToDto(e);
    }
}

public class CreateTaxRateHandler : IRequestHandler<CreateTaxRateCommand, TaxRateDto>
{
    private readonly ITaxRateRepository _repo;
    private readonly IUnitOfWork _uow;
    public CreateTaxRateHandler(ITaxRateRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }
    public async Task<TaxRateDto> Handle(CreateTaxRateCommand c, CancellationToken ct)
    {
        var entity = new TaxRate(c.Code, c.Name, c.RatePercent, c.IsWithholding, c.CountryCode, c.Description);
        await _repo.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);
        return MasterDataMapper.ToDto(entity);
    }
}

public class UpdateTaxRateHandler : IRequestHandler<UpdateTaxRateCommand, TaxRateDto>
{
    private readonly ITaxRateRepository _repo;
    private readonly IUnitOfWork _uow;
    public UpdateTaxRateHandler(ITaxRateRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }
    public async Task<TaxRateDto> Handle(UpdateTaxRateCommand c, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(c.Id, ct) ?? throw new KeyNotFoundException("TaxRate not found");
        entity.Update(c.Code, c.Name, c.RatePercent, c.IsWithholding, c.CountryCode, c.Description, c.IsActive);
        _repo.Update(entity);
        await _uow.SaveChangesAsync(ct);
        return MasterDataMapper.ToDto(entity);
    }
}

public class DeleteTaxRateHandler : IRequestHandler<DeleteTaxRateCommand, bool>
{
    private readonly ITaxRateRepository _repo;
    private readonly IUnitOfWork _uow;
    public DeleteTaxRateHandler(ITaxRateRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }
    public async Task<bool> Handle(DeleteTaxRateCommand c, CancellationToken ct)
    {
        var e = await _repo.GetByIdAsync(c.Id, ct);
        if (e is null) return false;
        _repo.Remove(e);
        await _uow.SaveChangesAsync(ct);
        return true;
    }
}
