using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Accounting.DTOs;

public class AccountingPeriodDto
{
    public Guid Id { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public string Code { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public AccountingPeriodStatus Status { get; set; }
    public DateTime? ClosedAtUtc { get; set; }
    public Guid? ClosedByUserId { get; set; }
    public DateTime? ReopenedAtUtc { get; set; }
    public string? Notes { get; set; }
}

public class GLAccountDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public AccountType Type { get; set; }
    public NormalSide NormalSide { get; set; }
    public Guid? ParentId { get; set; }
    public string? ParentCode { get; set; }
    public int Level { get; set; }
    public bool IsPostable { get; set; }
    public bool IsActive { get; set; }
    public string Currency { get; set; } = "TRY";
}

public class CustomerProductPriceDto
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public Guid ProductId { get; set; }
    public string ProductSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Currency { get; set; } = "TRY";
    public decimal Price { get; set; }
    public decimal? DiscountPercent { get; set; }
    public decimal? MinQuantity { get; set; }
    public decimal? MaxQuantity { get; set; }
    public DateTime? ValidFromUtc { get; set; }
    public DateTime? ValidUntilUtc { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
}

public record GLPostingMappingDto(
    GLPostingKey Key,
    string KeyName,
    string EffectiveAccountCode,
    string? OverrideAccountCode,
    string? DefaultAccountCode,
    string? AccountName,
    bool IsPostable);

public class ResolvedPriceDto
{
    public decimal UnitPrice { get; set; }
    public string Currency { get; set; } = "TRY";
    public decimal DiscountPercent { get; set; }
    public PriceSource Source { get; set; }
    public string SourceLabel { get; set; } = string.Empty;
    public decimal? ReferenceListPrice { get; set; }
    public decimal TaxRatePercent { get; set; }
    public bool IsTaxInclusive { get; set; }
    public Guid? TaxRateId { get; set; }
    public Guid? AppliedRecordId { get; set; }
}

// ---------- Financial Statements (Bilanço / Gelir Tablosu) ----------

public record StatementLineDto(Guid AccountId, string AccountCode, string AccountName, decimal Amount);

public record StatementSectionDto(List<StatementLineDto> Lines, decimal Total);

public record BalanceSheetReportDto(
    DateTime AsOf,
    StatementSectionDto Assets,
    StatementSectionDto Liabilities,
    StatementSectionDto Equity,
    decimal CurrentYearEarnings,
    decimal RetainedPriorEarnings,
    decimal TotalLiabilitiesAndEquity,
    bool IsBalanced,
    decimal Variance);

public record IncomeStatementReportDto(
    DateTime FromDate,
    DateTime ToDate,
    StatementSectionDto Revenue,
    StatementSectionDto Cogs,
    StatementSectionDto Opex,
    decimal GrossProfit,
    decimal NetIncome);

// ---------- Year-End Close / Opening ----------

/// <summary>
/// Outcome of a year-end close / opening / close-reversal. <see cref="AlreadyExisted"/>
/// is true when the deterministic-id guard short-circuited (idempotent re-run) and the
/// returned <see cref="Entry"/> is the pre-existing one rather than a freshly posted fiş.
/// <see cref="NetResult"/> is the period net (590 credit if profit, −591 debit if loss)
/// for a close, or the rolled retained-earnings amount for an opening.
/// </summary>
public record YearEndEntryDto(
    int Year,
    JournalEntryDto Entry,
    decimal NetResult,
    bool AlreadyExisted);

// ---------- Subledger-to-GL Reconciliation ----------

public record ReconciliationLineDto(
    string ControlCode,
    string ControlName,
    string Subledger,
    decimal GlBalance,
    decimal SubledgerBalance,
    decimal Variance,
    bool IsReconciled);

public record ReconciliationReportDto(
    DateTime AsOf,
    List<ReconciliationLineDto> Lines,
    bool AllReconciled);
