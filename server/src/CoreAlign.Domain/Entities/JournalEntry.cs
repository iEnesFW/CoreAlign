using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Domain.Entities;

/// <summary>
/// Yevmiye fişi — the core posting document in Turkish accounting. Lines are
/// owned by the aggregate; balance (Σdebit = Σcredit) is enforced on Post.
/// </summary>
public class JournalEntry : TenantEntity
{
    public string Number { get; private set; } = string.Empty;
    public DateTime EntryDate { get; private set; }
    public DateTime PostingDate { get; private set; }
    public JournalEntryType Type { get; private set; }
    public JournalEntryStatus Status { get; private set; } = JournalEntryStatus.Draft;
    public string? Description { get; private set; }
    public string? Reference { get; private set; }
    public decimal TotalDebit { get; private set; }
    public decimal TotalCredit { get; private set; }

    public DateTime? PostedAtUtc { get; private set; }
    public Guid? PostedByUserId { get; private set; }
    public DateTime? ReversedAtUtc { get; private set; }
    public Guid? ReversedByUserId { get; private set; }
    /// <summary>For a reversal entry: the original entry being reversed.</summary>
    public Guid? ReversalOfId { get; private set; }
    /// <summary>For an original entry that has been reversed: the reversal entry.</summary>
    public Guid? ReversedById { get; private set; }

    public JournalSourceType? SourceType { get; private set; }
    public Guid? SourceDocumentId { get; private set; }
    public string? SourceDocumentNumber { get; private set; }

    private readonly List<JournalLine> _lines = new();
    public IReadOnlyCollection<JournalLine> Lines => _lines;

    protected JournalEntry() { }

    public JournalEntry(
        string number,
        DateTime entryDate,
        DateTime postingDate,
        JournalEntryType type,
        string? description = null,
        string? reference = null)
    {
        if (string.IsNullOrWhiteSpace(number)) throw new ArgumentException("Number is required.", nameof(number));
        Number = number.Trim();
        EntryDate = DateTime.SpecifyKind(entryDate, DateTimeKind.Utc);
        PostingDate = DateTime.SpecifyKind(postingDate, DateTimeKind.Utc);
        Type = type;
        Description = description?.Trim();
        Reference = reference?.Trim();
    }

    public void UpdateHeader(DateTime entryDate, DateTime postingDate, JournalEntryType type, string? description, string? reference)
    {
        EnsureDraft();
        EntryDate = DateTime.SpecifyKind(entryDate, DateTimeKind.Utc);
        PostingDate = DateTime.SpecifyKind(postingDate, DateTimeKind.Utc);
        Type = type;
        Description = description?.Trim();
        Reference = reference?.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public JournalLine AddLine(
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
        EnsureDraft();
        var lineNumber = _lines.Count + 1;
        var line = new JournalLine(
            lineNumber,
            accountId,
            accountCode,
            accountName,
            debit,
            credit,
            currency,
            description,
            costCenter,
            project,
            foreignAmount,
            exchangeRate)
        {
            TenantId = TenantId,
        };
        line.AttachToEntry(Id);
        _lines.Add(line);
        RecalculateTotals();
        return line;
    }

    public void RemoveLine(Guid lineId)
    {
        EnsureDraft();
        var line = _lines.FirstOrDefault(l => l.Id == lineId)
            ?? throw new KeyNotFoundException($"Journal line {lineId} not found on this entry.");
        _lines.Remove(line);
        var i = 1;
        foreach (var l in _lines) l.Renumber(i++);
        RecalculateTotals();
    }

    public void ReplaceLines(IEnumerable<JournalLine> lines)
    {
        EnsureDraft();
        _lines.Clear();
        var i = 1;
        foreach (var l in lines)
        {
            l.Renumber(i++);
            _lines.Add(l);
        }
        RecalculateTotals();
    }

    public void Post(Guid postedByUserId)
    {
        if (Status != JournalEntryStatus.Draft)
        {
            throw new JournalEntryStatusTransitionException(Status.ToString(), "post");
        }
        if (_lines.Count < 2)
        {
            throw new JournalEntryEmptyException();
        }
        RecalculateTotals();
        // Banker's rounding to 4 decimal places to avoid spurious imbalance from
        // floating-point inputs while still catching real-world drift.
        var debit = Math.Round(TotalDebit, 4, MidpointRounding.ToEven);
        var credit = Math.Round(TotalCredit, 4, MidpointRounding.ToEven);
        if (debit != credit)
        {
            throw new JournalEntryNotBalancedException(debit, credit);
        }
        Status = JournalEntryStatus.Posted;
        PostedAtUtc = DateTime.UtcNow;
        PostedByUserId = postedByUserId;
        UpdatedAtUtc = PostedAtUtc.Value;
    }

    public void MarkReversed(Guid reversalEntryId, Guid reversedByUserId)
    {
        if (Status != JournalEntryStatus.Posted)
        {
            throw new JournalEntryStatusTransitionException(Status.ToString(), "reverse");
        }
        Status = JournalEntryStatus.Reversed;
        ReversedAtUtc = DateTime.UtcNow;
        ReversedByUserId = reversedByUserId;
        ReversedById = reversalEntryId;
        UpdatedAtUtc = ReversedAtUtc.Value;
    }

    public void MarkAsReversalOf(Guid originalEntryId)
    {
        // Reversal entries are themselves Draft initially; the application layer
        // posts them right after creation.
        ReversalOfId = originalEntryId;
    }

    public void AssignSource(JournalSourceType sourceType, Guid sourceDocumentId, string? sourceDocumentNumber)
    {
        SourceType = sourceType;
        SourceDocumentId = sourceDocumentId == Guid.Empty ? null : sourceDocumentId;
        SourceDocumentNumber = string.IsNullOrWhiteSpace(sourceDocumentNumber) ? null : sourceDocumentNumber.Trim();
    }

    private void RecalculateTotals()
    {
        TotalDebit = _lines.Sum(l => l.Debit);
        TotalCredit = _lines.Sum(l => l.Credit);
    }

    private void EnsureDraft()
    {
        if (Status != JournalEntryStatus.Draft)
        {
            throw new JournalEntryStatusTransitionException(Status.ToString(), "edit");
        }
    }
}
