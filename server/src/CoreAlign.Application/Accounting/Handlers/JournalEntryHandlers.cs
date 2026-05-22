using CoreAlign.Application.Accounting.Commands;
using CoreAlign.Application.Accounting.DTOs;
using CoreAlign.Application.Accounting.Mapping;
using CoreAlign.Application.Accounting.Queries;
using CoreAlign.Application.Common;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Accounting.Handlers;

/// <summary>
/// Resolves and snapshots a GL account onto a journal line, enforcing the
/// postable + active invariants every time a line is appended.
/// </summary>
internal static class JournalLineAttacher
{
    public static void AppendLines(
        JournalEntry entry,
        IReadOnlyList<JournalLineInput> inputs,
        IReadOnlyDictionary<Guid, GLAccount> accountsById)
    {
        foreach (var line in inputs)
        {
            if (!accountsById.TryGetValue(line.AccountId, out var account))
            {
                throw new GLAccountNotFoundException(line.AccountId);
            }
            if (!account.IsPostable)
            {
                throw new JournalLineNotPostableException(account.Code);
            }
            if (!account.IsActive)
            {
                throw new JournalLineInactiveAccountException(account.Code);
            }
            entry.AddLine(
                accountId: account.Id,
                accountCode: account.Code,
                accountName: account.Name,
                debit: line.Debit,
                credit: line.Credit,
                currency: string.IsNullOrWhiteSpace(line.Currency) ? account.Currency : line.Currency,
                description: line.Description,
                costCenter: line.CostCenter,
                project: line.Project,
                foreignAmount: line.ForeignAmount,
                exchangeRate: line.ExchangeRate);
        }
    }
}

public class CreateJournalEntryHandler : IRequestHandler<CreateJournalEntryCommand, JournalEntryDto>
{
    private readonly IJournalEntryRepository _journals;
    private readonly IGLAccountRepository _accounts;
    private readonly IDocumentSequenceRepository _sequences;
    private readonly IAccountingPeriodRepository _periods;
    private readonly IUnitOfWork _uow;

    public CreateJournalEntryHandler(
        IJournalEntryRepository journals,
        IGLAccountRepository accounts,
        IDocumentSequenceRepository sequences,
        IAccountingPeriodRepository periods,
        IUnitOfWork uow)
    {
        _journals = journals;
        _accounts = accounts;
        _sequences = sequences;
        _periods = periods;
        _uow = uow;
    }

    public async Task<JournalEntryDto> Handle(CreateJournalEntryCommand c, CancellationToken ct)
    {
        if (c.Lines is null || c.Lines.Count < 2)
        {
            throw new JournalEntryEmptyException();
        }

        // Period must be open at posting date — protects historical periods.
        var period = await _periods.GetByDateAsync(c.PostingDate, ct);
        period?.EnsurePostingAllowed(c.PostingDate);

        var type = ParseType(c.Type);
        var number = await _sequences.ConsumeAsync(DocumentSequenceType.JournalNumber, c.EntryDate, ct);

        var entry = new JournalEntry(number, c.EntryDate, c.PostingDate, type, c.Description, c.Reference);

        // Batch-load every referenced account in one round-trip.
        var accountIds = c.Lines.Select(l => l.AccountId).Distinct().ToArray();
        var accounts = await _accounts.ListAsync(null, null, null, null, ct);
        var byId = accounts
            .Where(a => accountIds.Contains(a.Id))
            .ToDictionary(a => a.Id);

        JournalLineAttacher.AppendLines(entry, c.Lines, byId);

        if (c.PostImmediately)
        {
            entry.Post(Guid.Empty);
        }

        await _journals.AddAsync(entry, ct);
        await _uow.SaveChangesAsync(ct);
        return AccountingMapper.ToDto(entry);
    }

    private static JournalEntryType ParseType(string raw)
    {
        if (Enum.TryParse<JournalEntryType>(raw, ignoreCase: true, out var t)) return t;
        throw new JournalEntryInvalidTypeException(raw);
    }
}

public class UpdateJournalEntryHeaderHandler : IRequestHandler<UpdateJournalEntryHeaderCommand, JournalEntryDto>
{
    private readonly IJournalEntryRepository _journals;
    private readonly IUnitOfWork _uow;

    public UpdateJournalEntryHeaderHandler(IJournalEntryRepository journals, IUnitOfWork uow)
    {
        _journals = journals;
        _uow = uow;
    }

    public async Task<JournalEntryDto> Handle(UpdateJournalEntryHeaderCommand c, CancellationToken ct)
    {
        var entry = await _journals.GetWithLinesAsync(c.Id, ct)
            ?? throw new JournalEntryNotFoundException(c.Id);

        if (!Enum.TryParse<JournalEntryType>(c.Type, ignoreCase: true, out var type))
        {
            throw new JournalEntryInvalidTypeException(c.Type);
        }
        entry.UpdateHeader(c.EntryDate, c.PostingDate, type, c.Description, c.Reference);
        _journals.Update(entry);
        await _uow.SaveChangesAsync(ct);
        return AccountingMapper.ToDto(entry);
    }
}

public class ReplaceJournalEntryLinesHandler : IRequestHandler<ReplaceJournalEntryLinesCommand, JournalEntryDto>
{
    private readonly IJournalEntryRepository _journals;
    private readonly IGLAccountRepository _accounts;
    private readonly IUnitOfWork _uow;

    public ReplaceJournalEntryLinesHandler(
        IJournalEntryRepository journals,
        IGLAccountRepository accounts,
        IUnitOfWork uow)
    {
        _journals = journals;
        _accounts = accounts;
        _uow = uow;
    }

    public async Task<JournalEntryDto> Handle(ReplaceJournalEntryLinesCommand c, CancellationToken ct)
    {
        if (c.Lines is null || c.Lines.Count < 2)
        {
            throw new JournalEntryEmptyException();
        }

        var entry = await _journals.GetWithLinesAsync(c.Id, ct)
            ?? throw new JournalEntryNotFoundException(c.Id);

        var accountIds = c.Lines.Select(l => l.AccountId).Distinct().ToArray();
        var accounts = await _accounts.ListAsync(null, null, null, null, ct);
        var byId = accounts.Where(a => accountIds.Contains(a.Id)).ToDictionary(a => a.Id);

        // Clear and re-append through the aggregate so it recalculates totals
        // and re-validates each line via the entity constructor.
        entry.ReplaceLines(Array.Empty<JournalLine>());
        JournalLineAttacher.AppendLines(entry, c.Lines, byId);

        _journals.Update(entry);
        await _uow.SaveChangesAsync(ct);
        return AccountingMapper.ToDto(entry);
    }
}

public class PostJournalEntryHandler : IRequestHandler<PostJournalEntryCommand, JournalEntryDto>
{
    private readonly IJournalEntryRepository _journals;
    private readonly IAccountingPeriodRepository _periods;
    private readonly IUnitOfWork _uow;

    public PostJournalEntryHandler(
        IJournalEntryRepository journals,
        IAccountingPeriodRepository periods,
        IUnitOfWork uow)
    {
        _journals = journals;
        _periods = periods;
        _uow = uow;
    }

    public async Task<JournalEntryDto> Handle(PostJournalEntryCommand c, CancellationToken ct)
    {
        var entry = await _journals.GetWithLinesAsync(c.Id, ct)
            ?? throw new JournalEntryNotFoundException(c.Id);

        var period = await _periods.GetByDateAsync(entry.PostingDate, ct);
        period?.EnsurePostingAllowed(entry.PostingDate);

        entry.Post(c.PostedByUserId ?? Guid.Empty);
        _journals.Update(entry);
        await _uow.SaveChangesAsync(ct);
        return AccountingMapper.ToDto(entry);
    }
}

public class ReverseJournalEntryHandler : IRequestHandler<ReverseJournalEntryCommand, JournalEntryDto>
{
    private readonly IJournalEntryRepository _journals;
    private readonly IDocumentSequenceRepository _sequences;
    private readonly IAccountingPeriodRepository _periods;
    private readonly IUnitOfWork _uow;

    public ReverseJournalEntryHandler(
        IJournalEntryRepository journals,
        IDocumentSequenceRepository sequences,
        IAccountingPeriodRepository periods,
        IUnitOfWork uow)
    {
        _journals = journals;
        _sequences = sequences;
        _periods = periods;
        _uow = uow;
    }

    public async Task<JournalEntryDto> Handle(ReverseJournalEntryCommand c, CancellationToken ct)
    {
        var original = await _journals.GetWithLinesAsync(c.Id, ct)
            ?? throw new JournalEntryNotFoundException(c.Id);

        if (original.Status != JournalEntryStatus.Posted)
        {
            throw new JournalEntryStatusTransitionException(original.Status.ToString(), "reverse");
        }

        var postingDate = c.ReversalPostingDate ?? DateTime.UtcNow;
        var period = await _periods.GetByDateAsync(postingDate, ct);
        period?.EnsurePostingAllowed(postingDate);

        var revNumber = await _sequences.ConsumeAsync(DocumentSequenceType.JournalNumber, postingDate, ct);
        var reversal = new JournalEntry(
            revNumber,
            postingDate,
            postingDate,
            original.Type,
            $"Reversal of {original.Number}",
            original.Reference);
        reversal.MarkAsReversalOf(original.Id);

        // Swap debit/credit for each line — the reversal nets the original out
        // exactly while keeping audit history intact (both entries remain
        // visible in the journal).
        foreach (var l in original.Lines.OrderBy(l => l.LineNumber))
        {
            reversal.AddLine(
                accountId: l.AccountId,
                accountCode: l.AccountCode,
                accountName: l.AccountName,
                debit: l.Credit,
                credit: l.Debit,
                currency: l.Currency,
                description: l.Description,
                costCenter: l.CostCenter,
                project: l.Project,
                foreignAmount: l.ForeignAmount,
                exchangeRate: l.ExchangeRate);
        }
        reversal.Post(c.ReversedByUserId ?? Guid.Empty);

        original.MarkReversed(reversal.Id, c.ReversedByUserId ?? Guid.Empty);

        await _journals.AddAsync(reversal, ct);
        _journals.Update(original);
        await _uow.SaveChangesAsync(ct);
        return AccountingMapper.ToDto(reversal);
    }
}

public class DeleteJournalEntryHandler : IRequestHandler<DeleteJournalEntryCommand, bool>
{
    private readonly IJournalEntryRepository _journals;
    private readonly IUnitOfWork _uow;

    public DeleteJournalEntryHandler(IJournalEntryRepository journals, IUnitOfWork uow)
    {
        _journals = journals;
        _uow = uow;
    }

    public async Task<bool> Handle(DeleteJournalEntryCommand c, CancellationToken ct)
    {
        var entry = await _journals.GetByIdAsync(c.Id, ct)
            ?? throw new JournalEntryNotFoundException(c.Id);
        if (entry.Status != JournalEntryStatus.Draft)
        {
            throw new JournalEntryStatusTransitionException(entry.Status.ToString(), "delete");
        }
        _journals.Remove(entry);
        await _uow.SaveChangesAsync(ct);
        return true;
    }
}

public class GetJournalEntryByIdHandler : IRequestHandler<GetJournalEntryByIdQuery, JournalEntryDto?>
{
    private readonly IJournalEntryRepository _journals;
    public GetJournalEntryByIdHandler(IJournalEntryRepository journals) => _journals = journals;

    public async Task<JournalEntryDto?> Handle(GetJournalEntryByIdQuery q, CancellationToken ct)
    {
        var entry = await _journals.GetWithLinesAsync(q.Id, ct);
        return entry is null ? null : AccountingMapper.ToDto(entry);
    }
}

public class SearchJournalEntriesHandler : IRequestHandler<SearchJournalEntriesQuery, PagedResult<JournalEntrySummaryDto>>
{
    private readonly IJournalEntryRepository _journals;
    public SearchJournalEntriesHandler(IJournalEntryRepository journals) => _journals = journals;

    public async Task<PagedResult<JournalEntrySummaryDto>> Handle(SearchJournalEntriesQuery q, CancellationToken ct)
    {
        var page = Math.Max(1, q.Page);
        var pageSize = Math.Clamp(q.PageSize, 1, 100);
        var (items, total) = await _journals.SearchAsync(
            q.Search, q.Type, q.Status, q.FromDate, q.ToDate, page, pageSize, ct);
        return new PagedResult<JournalEntrySummaryDto>
        {
            Items = items.Select(AccountingMapper.ToDto).ToList(),
            Total = total,
            Page = page,
            PageSize = pageSize,
        };
    }
}

public class GetTrialBalanceHandler : IRequestHandler<GetTrialBalanceQuery, TrialBalanceReportDto>
{
    private readonly IJournalEntryRepository _journals;
    private readonly IGLAccountRepository _accounts;

    public GetTrialBalanceHandler(IJournalEntryRepository journals, IGLAccountRepository accounts)
    {
        _journals = journals;
        _accounts = accounts;
    }

    public async Task<TrialBalanceReportDto> Handle(GetTrialBalanceQuery q, CancellationToken ct)
    {
        var aggregates = await _journals.GetAccountBalancesAsync(q.FromDate, q.ToDate, ct);
        var accounts = await _accounts.GetAllAsync(ct);
        var byId = accounts.ToDictionary(a => a.Id);

        var rows = aggregates.Select(r =>
        {
            byId.TryGetValue(r.AccountId, out var account);
            var debit = Math.Round(r.Debit, 4, MidpointRounding.ToEven);
            var credit = Math.Round(r.Credit, 4, MidpointRounding.ToEven);
            // Balance is presented as a single signed number from the account's
            // own perspective — positive when on its normal side.
            var balance = account?.NormalSide == NormalSide.Debit
                ? debit - credit
                : credit - debit;
            return new TrialBalanceRowDto
            {
                AccountId = r.AccountId,
                AccountCode = r.AccountCode,
                AccountName = r.AccountName,
                Type = account?.Type ?? AccountType.Asset,
                NormalSide = account?.NormalSide ?? NormalSide.Debit,
                Debit = debit,
                Credit = credit,
                Balance = balance,
            };
        }).ToList();

        return new TrialBalanceReportDto
        {
            FromDate = q.FromDate,
            ToDate = q.ToDate,
            TotalDebit = rows.Sum(r => r.Debit),
            TotalCredit = rows.Sum(r => r.Credit),
            Rows = rows,
        };
    }
}
