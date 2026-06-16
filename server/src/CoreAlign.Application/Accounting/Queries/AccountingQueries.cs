using CoreAlign.Application.Accounting.DTOs;
using CoreAlign.Domain.Enums;
using MediatR;

namespace CoreAlign.Application.Accounting.Queries;

public record GetAccountingPeriodByIdQuery(Guid Id) : IRequest<AccountingPeriodDto?>;

public record ListAccountingPeriodsQuery(int? Year = null) : IRequest<IReadOnlyList<AccountingPeriodDto>>;

public record GetCustomerProductPricesQuery(Guid? CustomerId = null, Guid? ProductId = null)
    : IRequest<IReadOnlyList<CustomerProductPriceDto>>;

public record ResolvePriceQuery(
    Guid ProductId,
    Guid CustomerId,
    decimal Quantity = 1m,
    string? Currency = null) : IRequest<ResolvedPriceDto>;

public record GetGLAccountByIdQuery(Guid Id) : IRequest<GLAccountDto?>;

public record ListGLAccountsQuery(
    AccountType? Type = null,
    bool? IsActive = null,
    bool? IsPostable = null,
    Guid? ParentId = null) : IRequest<IReadOnlyList<GLAccountDto>>;

/// <summary>Whole chart of the current tenant — used for tree rendering.</summary>
public record GetGLAccountTreeQuery() : IRequest<IReadOnlyList<GLAccountDto>>;

// ---------- Journal Entries ----------

public record GetJournalEntryByIdQuery(Guid Id) : IRequest<JournalEntryDto?>;

public record SearchJournalEntriesQuery(
    string? Search = null,
    JournalEntryType? Type = null,
    JournalEntryStatus? Status = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    int Page = 1,
    int PageSize = 25) : IRequest<Common.PagedResult<JournalEntrySummaryDto>>;

// ---------- Mizan / Trial Balance ----------

public record ListGLPostingMappingsQuery() : IRequest<IReadOnlyList<GLPostingMappingDto>>;

public record GetTrialBalanceQuery(
    DateTime? FromDate = null,
    DateTime? ToDate = null) : IRequest<TrialBalanceReportDto>;

// ---------- Financial Statements ----------

/// <summary>Server-authoritative balance sheet (Bilanço) at a cumulative as-of cutoff.</summary>
public record GetBalanceSheetQuery(DateTime AsOf) : IRequest<BalanceSheetReportDto>;

/// <summary>Income statement (Gelir Tablosu) over a posting-date range — movement only.</summary>
public record GetIncomeStatementQuery(DateTime FromDate, DateTime ToDate) : IRequest<IncomeStatementReportDto>;

/// <summary>Subledger-to-GL reconciliation (AR 120 / AP 320 / cash 100+102) at a cumulative as-of.</summary>
public record GetSubledgerReconciliationQuery(DateTime AsOf) : IRequest<ReconciliationReportDto>;
