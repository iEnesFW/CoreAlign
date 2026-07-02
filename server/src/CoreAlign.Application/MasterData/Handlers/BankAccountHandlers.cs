using CoreAlign.Application.MasterData.Commands;
using CoreAlign.Application.MasterData.DTOs;
using CoreAlign.Application.MasterData.Mapping;
using CoreAlign.Application.MasterData.Queries;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.MasterData.Handlers;

public class ListBankAccountsHandler : IRequestHandler<ListBankAccountsQuery, IReadOnlyList<BankAccountDto>>
{
    private readonly IBankAccountRepository _repo;
    public ListBankAccountsHandler(IBankAccountRepository repo) => _repo = repo;
    public async Task<IReadOnlyList<BankAccountDto>> Handle(ListBankAccountsQuery q, CancellationToken ct)
        => (await _repo.ListAsync(q.IsActive, ct)).Select(MasterDataMapper.ToDto).ToList();
}

public class GetBankAccountByIdHandler : IRequestHandler<GetBankAccountByIdQuery, BankAccountDto>
{
    private readonly IBankAccountRepository _repo;
    public GetBankAccountByIdHandler(IBankAccountRepository repo) => _repo = repo;
    public async Task<BankAccountDto> Handle(GetBankAccountByIdQuery q, CancellationToken ct)
    {
        var e = await _repo.GetByIdAsync(q.Id, ct) ?? throw new BankAccountNotFoundException();
        return MasterDataMapper.ToDto(e);
    }
}

public class CreateBankAccountHandler : IRequestHandler<CreateBankAccountCommand, BankAccountDto>
{
    private readonly IBankAccountRepository _repo;
    private readonly IUnitOfWork _uow;
    public CreateBankAccountHandler(IBankAccountRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }
    public async Task<BankAccountDto> Handle(CreateBankAccountCommand c, CancellationToken ct)
    {
        if (c.IsPrimary) await _repo.ClearPrimaryFlagAsync(null, ct);
        var entity = new BankAccount(c.AccountName, c.BankName, c.Iban, c.Currency, c.OpeningBalance, c.BranchName, c.Swift, c.IsPrimary, c.Notes);
        await _repo.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);
        return MasterDataMapper.ToDto(entity);
    }
}

public class UpdateBankAccountHandler : IRequestHandler<UpdateBankAccountCommand, BankAccountDto>
{
    private readonly IBankAccountRepository _repo;
    private readonly IUnitOfWork _uow;
    public UpdateBankAccountHandler(IBankAccountRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }
    public async Task<BankAccountDto> Handle(UpdateBankAccountCommand c, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(c.Id, ct) ?? throw new BankAccountNotFoundException();
        if (c.IsPrimary) await _repo.ClearPrimaryFlagAsync(c.Id, ct);
        entity.Update(c.AccountName, c.BankName, c.Iban, c.Currency, c.OpeningBalance, c.BranchName, c.Swift, c.IsPrimary, c.IsActive, c.Notes);
        _repo.Update(entity);
        await _uow.SaveChangesAsync(ct);
        return MasterDataMapper.ToDto(entity);
    }
}

public class DeleteBankAccountHandler : IRequestHandler<DeleteBankAccountCommand, bool>
{
    private readonly IBankAccountRepository _repo;
    private readonly IUnitOfWork _uow;
    public DeleteBankAccountHandler(IBankAccountRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }
    public async Task<bool> Handle(DeleteBankAccountCommand c, CancellationToken ct)
    {
        var e = await _repo.GetByIdAsync(c.Id, ct) ?? throw new BankAccountNotFoundException();
        _repo.Remove(e);
        await _uow.SaveChangesAsync(ct);
        return true;
    }
}
