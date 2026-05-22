using CoreAlign.Application.Accounting.Commands;
using CoreAlign.Application.Accounting.DTOs;
using CoreAlign.Application.Accounting.Mapping;
using CoreAlign.Application.Accounting.Queries;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Accounting.Handlers;

public class CreateGLAccountHandler : IRequestHandler<CreateGLAccountCommand, GLAccountDto>
{
    private readonly IGLAccountRepository _repo;
    private readonly IUnitOfWork _uow;
    public CreateGLAccountHandler(IGLAccountRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<GLAccountDto> Handle(CreateGLAccountCommand c, CancellationToken ct)
    {
        if (await _repo.CodeExistsAsync(c.Code, null, ct))
        {
            throw new GLAccountCodeConflictException(c.Code);
        }

        var type = ParseAccountType(c.Type);
        GLAccount? parent = null;
        var level = 1;
        if (c.ParentId.HasValue)
        {
            parent = await _repo.GetByIdAsync(c.ParentId.Value, ct)
                ?? throw new GLAccountNotFoundException(c.ParentId.Value);
            // A non-postable parent must stay non-postable once it has children;
            // posting only happens at the leaf.
            if (parent.IsPostable)
            {
                parent.ChangePostable(false);
                _repo.Update(parent);
            }
            level = parent.Level + 1;
        }

        var account = new GLAccount(c.Code, c.Name, type, c.IsPostable, parent?.Id, level, c.Currency, c.Description);
        await _repo.AddAsync(account, ct);
        await _uow.SaveChangesAsync(ct);

        return AccountingMapper.ToDto(account, parent?.Code);
    }

    private static AccountType ParseAccountType(string raw)
    {
        if (Enum.TryParse<AccountType>(raw, ignoreCase: true, out var t)) return t;
        throw new GLAccountInvalidTypeException(raw);
    }
}

public class UpdateGLAccountHandler : IRequestHandler<UpdateGLAccountCommand, GLAccountDto>
{
    private readonly IGLAccountRepository _repo;
    private readonly IUnitOfWork _uow;
    public UpdateGLAccountHandler(IGLAccountRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<GLAccountDto> Handle(UpdateGLAccountCommand c, CancellationToken ct)
    {
        var account = await _repo.GetByIdAsync(c.Id, ct)
            ?? throw new GLAccountNotFoundException(c.Id);

        account.Rename(c.Name, c.Description);
        account.ChangeCurrency(c.Currency);

        // Toggling posting on a parent with children is rejected here — the
        // domain method doesn't know about children, so the application layer
        // enforces the invariant.
        if (account.IsPostable != c.IsPostable && c.IsPostable && await _repo.HasChildrenAsync(account.Id, ct))
        {
            throw new GLAccountPostableInvariantException();
        }
        account.ChangePostable(c.IsPostable);

        _repo.Update(account);
        await _uow.SaveChangesAsync(ct);

        string? parentCode = null;
        if (account.ParentId.HasValue)
        {
            var parent = await _repo.GetByIdAsync(account.ParentId.Value, ct);
            parentCode = parent?.Code;
        }
        return AccountingMapper.ToDto(account, parentCode);
    }
}

public class SetGLAccountActiveHandler : IRequestHandler<SetGLAccountActiveCommand, GLAccountDto>
{
    private readonly IGLAccountRepository _repo;
    private readonly IUnitOfWork _uow;
    public SetGLAccountActiveHandler(IGLAccountRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<GLAccountDto> Handle(SetGLAccountActiveCommand c, CancellationToken ct)
    {
        var account = await _repo.GetByIdAsync(c.Id, ct)
            ?? throw new GLAccountNotFoundException(c.Id);
        if (c.IsActive) account.Activate(); else account.Deactivate();
        _repo.Update(account);
        await _uow.SaveChangesAsync(ct);
        return AccountingMapper.ToDto(account);
    }
}

public class DeleteGLAccountHandler : IRequestHandler<DeleteGLAccountCommand, bool>
{
    private readonly IGLAccountRepository _repo;
    private readonly IUnitOfWork _uow;
    public DeleteGLAccountHandler(IGLAccountRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<bool> Handle(DeleteGLAccountCommand c, CancellationToken ct)
    {
        var account = await _repo.GetByIdAsync(c.Id, ct)
            ?? throw new GLAccountNotFoundException(c.Id);

        if (await _repo.HasChildrenAsync(account.Id, ct))
        {
            throw new GLAccountHasChildrenException();
        }
        // Future hardening: once JournalLine exists, also reject delete if any
        // postings reference this account — for now there are no postings yet.

        _repo.Remove(account);
        await _uow.SaveChangesAsync(ct);
        return true;
    }
}

public class GetGLAccountByIdHandler : IRequestHandler<GetGLAccountByIdQuery, GLAccountDto?>
{
    private readonly IGLAccountRepository _repo;
    public GetGLAccountByIdHandler(IGLAccountRepository repo) => _repo = repo;

    public async Task<GLAccountDto?> Handle(GetGLAccountByIdQuery q, CancellationToken ct)
    {
        var account = await _repo.GetByIdAsync(q.Id, ct);
        if (account is null) return null;
        string? parentCode = null;
        if (account.ParentId.HasValue)
        {
            var parent = await _repo.GetByIdAsync(account.ParentId.Value, ct);
            parentCode = parent?.Code;
        }
        return AccountingMapper.ToDto(account, parentCode);
    }
}

public class ListGLAccountsHandler : IRequestHandler<ListGLAccountsQuery, IReadOnlyList<GLAccountDto>>
{
    private readonly IGLAccountRepository _repo;
    public ListGLAccountsHandler(IGLAccountRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<GLAccountDto>> Handle(ListGLAccountsQuery q, CancellationToken ct)
    {
        var accounts = await _repo.ListAsync(q.Type, q.IsActive, q.IsPostable, q.ParentId, ct);
        // Parent code lookup is O(N) over the same set — build once.
        var byId = accounts.ToDictionary(a => a.Id, a => a.Code);
        return accounts
            .Select(a => AccountingMapper.ToDto(a, a.ParentId.HasValue && byId.TryGetValue(a.ParentId.Value, out var pc) ? pc : null))
            .ToList();
    }
}

public class GetGLAccountTreeHandler : IRequestHandler<GetGLAccountTreeQuery, IReadOnlyList<GLAccountDto>>
{
    private readonly IGLAccountRepository _repo;
    public GetGLAccountTreeHandler(IGLAccountRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<GLAccountDto>> Handle(GetGLAccountTreeQuery q, CancellationToken ct)
    {
        var accounts = await _repo.GetAllAsync(ct);
        var byId = accounts.ToDictionary(a => a.Id, a => a.Code);
        return accounts
            .Select(a => AccountingMapper.ToDto(a, a.ParentId.HasValue && byId.TryGetValue(a.ParentId.Value, out var pc) ? pc : null))
            .ToList();
    }
}

public class SeedTurkishChartOfAccountsHandler : IRequestHandler<SeedTurkishChartOfAccountsCommand, int>
{
    private readonly IGLAccountRepository _repo;
    private readonly IUnitOfWork _uow;
    public SeedTurkishChartOfAccountsHandler(IGLAccountRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<int> Handle(SeedTurkishChartOfAccountsCommand c, CancellationToken ct)
    {
        var existing = await _repo.GetAllAsync(ct);
        var existingCodes = new HashSet<string>(existing.Select(a => a.Code), StringComparer.Ordinal);

        var codeToAccount = new Dictionary<string, GLAccount>(StringComparer.Ordinal);
        var toAdd = new List<GLAccount>();

        // First pass: create accounts without parent IDs (we'll wire parents in
        // a second pass once we know which fresh accounts got which IDs).
        foreach (var seed in TurkishChartOfAccountsSeed.Entries)
        {
            if (existingCodes.Contains(seed.Code))
            {
                continue;
            }
            var account = new GLAccount(
                seed.Code,
                seed.Name,
                seed.Type,
                seed.IsPostable,
                parentId: null,
                level: seed.Level);
            codeToAccount[seed.Code] = account;
            toAdd.Add(account);
        }

        // Second pass: link parents by prefix. The seed list is ordered shortest
        // code first, so by the time we see "100.01" the "100" account is
        // already in the lookup (either freshly created or pre-existing).
        var existingByCode = existing.ToDictionary(a => a.Code, StringComparer.Ordinal);
        foreach (var (code, account) in codeToAccount)
        {
            var parentCode = TurkishChartOfAccountsSeed.ParentCodeOf(code);
            if (parentCode is null) continue;
            if (codeToAccount.TryGetValue(parentCode, out var parentNew))
            {
                SetParent(account, parentNew.Id);
            }
            else if (existingByCode.TryGetValue(parentCode, out var parentOld))
            {
                SetParent(account, parentOld.Id);
            }
        }

        if (toAdd.Count > 0)
        {
            await _repo.AddRangeAsync(toAdd, ct);
            await _uow.SaveChangesAsync(ct);
        }
        return toAdd.Count;
    }

    // GLAccount.ParentId has a private setter — we can't use the public ctor
    // because it requires the parent to exist before the child is constructed,
    // and during seed we mint children in one pass. Reflection-free helper:
    // we just call the public ctor with parentId set when we know the parent.
    private static void SetParent(GLAccount account, Guid parentId)
    {
        // Re-emit through the public surface by recreating? No — we just use
        // the field hack via a tiny internal setter on the entity.
        account.AssignParent(parentId, account.Level);
    }
}
