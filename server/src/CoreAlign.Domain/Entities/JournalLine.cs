using CoreAlign.Domain.Common;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Domain.Entities;

/// <summary>
/// A single posting line within a <see cref="JournalEntry"/>. Either Debit or
/// Credit is positive, never both; the invariant is enforced at construction.
/// Account is referenced by FK (<see cref="AccountId"/>) and snapshot
/// (<see cref="AccountCode"/>/<see cref="AccountName"/>) so reports keep
/// readable history even when the chart of accounts is reorganized later.
/// </summary>
public class JournalLine : TenantEntity
{
    public Guid JournalEntryId { get; private set; }
    public int LineNumber { get; private set; }
    public Guid AccountId { get; private set; }
    public string AccountCode { get; private set; } = string.Empty;
    public string AccountName { get; private set; } = string.Empty;
    public decimal Debit { get; private set; }
    public decimal Credit { get; private set; }
    public string? Description { get; private set; }
    public string? CostCenter { get; private set; }
    public string? Project { get; private set; }
    public string Currency { get; private set; } = "TRY";
    public decimal? ForeignAmount { get; private set; }
    public decimal? ExchangeRate { get; private set; }

    protected JournalLine() { }

    public JournalLine(
        int lineNumber,
        Guid accountId,
        string accountCode,
        string accountName,
        decimal debit,
        decimal credit,
        string currency = "TRY",
        string? description = null,
        string? costCenter = null,
        string? project = null,
        decimal? foreignAmount = null,
        decimal? exchangeRate = null)
    {
        if (lineNumber < 1) throw new ArgumentOutOfRangeException(nameof(lineNumber));
        if (debit < 0 || credit < 0) throw new ArgumentOutOfRangeException("Debit/Credit must be ≥ 0.");
        if (debit > 0 && credit > 0) throw new JournalLineSidesException();
        if (debit == 0 && credit == 0) throw new JournalLineSidesException();
        if (string.IsNullOrWhiteSpace(accountCode)) throw new ArgumentException("Account code is required.");

        LineNumber = lineNumber;
        AccountId = accountId;
        AccountCode = accountCode.Trim();
        AccountName = accountName.Trim();
        Debit = debit;
        Credit = credit;
        Currency = currency.Trim().ToUpperInvariant();
        Description = description?.Trim();
        CostCenter = costCenter?.Trim();
        Project = project?.Trim();
        ForeignAmount = foreignAmount;
        ExchangeRate = exchangeRate;
    }

    internal void Renumber(int lineNumber)
    {
        if (lineNumber < 1) throw new ArgumentOutOfRangeException(nameof(lineNumber));
        LineNumber = lineNumber;
    }

    internal void AttachToEntry(Guid journalEntryId)
    {
        JournalEntryId = journalEntryId;
    }
}
