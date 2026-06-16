using CoreAlign.Application.Accounting.DTOs;
using CoreAlign.Application.Common;
using CoreAlign.Domain.Enums;
using MediatR;

namespace CoreAlign.Application.Accounting.Commands;

public record ConfigureGLPostingMappingCommand(GLPostingKey Key, string AccountCode)
    : IRequest<GLPostingMappingDto>, ITransactionalRequest;

public record CreateAccountingPeriodCommand(int Year, int Month)
    : IRequest<AccountingPeriodDto>, ITransactionalRequest;

public record ClosePeriodCommand(Guid Id, Guid? ClosedByUserId = null, string? Notes = null)
    : IRequest<AccountingPeriodDto>, ITransactionalRequest;

public record ReopenPeriodCommand(Guid Id, Guid? ReopenedByUserId = null)
    : IRequest<AccountingPeriodDto>, ITransactionalRequest;

public record LockPeriodCommand(Guid Id, Guid? LockedByUserId = null)
    : IRequest<AccountingPeriodDto>, ITransactionalRequest;

public record CreateCustomerProductPriceCommand(
    Guid CustomerId,
    Guid ProductId,
    decimal Price,
    string Currency = "TRY",
    decimal? DiscountPercent = null,
    decimal? MinQuantity = null,
    decimal? MaxQuantity = null,
    DateTime? ValidFromUtc = null,
    DateTime? ValidUntilUtc = null,
    string? Notes = null) : IRequest<CustomerProductPriceDto>, ITransactionalRequest;

public record UpdateCustomerProductPriceCommand(
    Guid Id,
    decimal Price,
    string Currency,
    decimal? DiscountPercent,
    decimal? MinQuantity,
    decimal? MaxQuantity,
    DateTime? ValidFromUtc,
    DateTime? ValidUntilUtc,
    string? Notes,
    bool IsActive) : IRequest<CustomerProductPriceDto>, ITransactionalRequest;

public record DeleteCustomerProductPriceCommand(Guid Id) : IRequest<bool>, ITransactionalRequest;

public record CreateGLAccountCommand(
    string Code,
    string Name,
    string Type,
    bool IsPostable,
    Guid? ParentId,
    string Currency = "TRY",
    string? Description = null) : IRequest<DTOs.GLAccountDto>, ITransactionalRequest;

public record UpdateGLAccountCommand(
    Guid Id,
    string Name,
    string? Description,
    bool IsPostable,
    string Currency) : IRequest<DTOs.GLAccountDto>, ITransactionalRequest;

public record SetGLAccountActiveCommand(Guid Id, bool IsActive) : IRequest<DTOs.GLAccountDto>, ITransactionalRequest;

public record DeleteGLAccountCommand(Guid Id) : IRequest<bool>, ITransactionalRequest;

/// <summary>
/// Seeds the standard Turkish Tek Düzen Hesap Planı (TDHP) for the current
/// tenant. Idempotent — accounts that already exist (by code) are skipped.
/// </summary>
public record SeedTurkishChartOfAccountsCommand() : IRequest<int>, ITransactionalRequest;

// ---------- Journal Entries (Yevmiye Fişleri) ----------

public record JournalLineInput(
    Guid AccountId,
    decimal Debit,
    decimal Credit,
    string Currency = "TRY",
    string? Description = null,
    string? CostCenter = null,
    string? Project = null,
    decimal? ForeignAmount = null,
    decimal? ExchangeRate = null);

public record CreateJournalEntryCommand(
    DateTime EntryDate,
    DateTime PostingDate,
    string Type,
    string? Description,
    string? Reference,
    IReadOnlyList<JournalLineInput> Lines,
    bool PostImmediately = false) : IRequest<DTOs.JournalEntryDto>, ITransactionalRequest;

public record UpdateJournalEntryHeaderCommand(
    Guid Id,
    DateTime EntryDate,
    DateTime PostingDate,
    string Type,
    string? Description,
    string? Reference) : IRequest<DTOs.JournalEntryDto>, ITransactionalRequest;

public record ReplaceJournalEntryLinesCommand(
    Guid Id,
    IReadOnlyList<JournalLineInput> Lines) : IRequest<DTOs.JournalEntryDto>, ITransactionalRequest;

public record PostJournalEntryCommand(Guid Id, Guid? PostedByUserId = null)
    : IRequest<DTOs.JournalEntryDto>, ITransactionalRequest;

public record ReverseJournalEntryCommand(Guid Id, DateTime? ReversalPostingDate, Guid? ReversedByUserId = null)
    : IRequest<DTOs.JournalEntryDto>, ITransactionalRequest;

public record DeleteJournalEntryCommand(Guid Id) : IRequest<bool>, ITransactionalRequest;

// ---------- Year-End Close / Opening (Kapanış / Açılış) ----------

/// <summary>Posts the statutory year-end closing fiş (Kapanış) for <paramref name="Year"/>.</summary>
public record CloseFiscalYearCommand(int Year, Guid? PostedByUserId = null)
    : IRequest<DTOs.YearEndEntryDto>, ITransactionalRequest;

/// <summary>
/// Posts the opening fiş (Açılış) that re-opens the books on Jan 1 of
/// <paramref name="Year"/>+1. <paramref name="Year"/> is the year being CLOSED.
/// </summary>
public record OpenFiscalYearCommand(int Year, Guid? PostedByUserId = null)
    : IRequest<DTOs.YearEndEntryDto>, ITransactionalRequest;

/// <summary>Contra-reverses the year-end close of <paramref name="Year"/> while it is still un-consumed.</summary>
public record ReverseFiscalYearCloseCommand(int Year, Guid? ReversedByUserId = null)
    : IRequest<DTOs.YearEndEntryDto>, ITransactionalRequest;
