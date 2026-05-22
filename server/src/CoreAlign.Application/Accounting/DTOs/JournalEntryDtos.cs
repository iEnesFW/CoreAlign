using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Accounting.DTOs;

public class JournalEntryDto
{
    public Guid Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public DateTime EntryDate { get; set; }
    public DateTime PostingDate { get; set; }
    public JournalEntryType Type { get; set; }
    public JournalEntryStatus Status { get; set; }
    public string? Description { get; set; }
    public string? Reference { get; set; }
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public DateTime? PostedAtUtc { get; set; }
    public DateTime? ReversedAtUtc { get; set; }
    public Guid? ReversalOfId { get; set; }
    public Guid? ReversedById { get; set; }
    public List<JournalLineDto> Lines { get; set; } = new();
}

public class JournalLineDto
{
    public Guid Id { get; set; }
    public int LineNumber { get; set; }
    public Guid AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string Currency { get; set; } = "TRY";
    public string? Description { get; set; }
    public string? CostCenter { get; set; }
    public string? Project { get; set; }
    public decimal? ForeignAmount { get; set; }
    public decimal? ExchangeRate { get; set; }
}

public class JournalEntrySummaryDto
{
    public Guid Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public DateTime EntryDate { get; set; }
    public DateTime PostingDate { get; set; }
    public JournalEntryType Type { get; set; }
    public JournalEntryStatus Status { get; set; }
    public string? Description { get; set; }
    public string? Reference { get; set; }
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public int LineCount { get; set; }
}

public class TrialBalanceRowDto
{
    public Guid AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public AccountType Type { get; set; }
    public NormalSide NormalSide { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal Balance { get; set; }
}

public class TrialBalanceReportDto
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public List<TrialBalanceRowDto> Rows { get; set; } = new();
}
