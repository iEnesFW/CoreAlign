using CoreAlign.Domain.Entities;

namespace CoreAlign.Domain.Interfaces;

public enum PriceSource
{
    ProductListPrice = 0,
    PriceList = 1,
    CustomerProductPrice = 2,
    Promotion = 3,
    ManualOverride = 4,
}

public record PriceResolutionRequest(
    Guid ProductId,
    Guid CustomerId,
    decimal Quantity,
    DateTime AsOfUtc,
    string? RequestedCurrency = null);

public record PriceResolutionResult(
    decimal UnitPrice,
    string Currency,
    decimal DiscountPercent,
    PriceSource Source,
    string SourceLabel,
    decimal? ReferenceListPrice,
    decimal TaxRatePercent,
    bool IsTaxInclusive,
    Guid? TaxRateId,
    Guid? AppliedRecordId);

public interface IPricingService
{
    Task<PriceResolutionResult> ResolveAsync(PriceResolutionRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PriceResolutionResult>> ResolveBatchAsync(IEnumerable<PriceResolutionRequest> requests, CancellationToken cancellationToken = default);
    Task<decimal?> ResolveMinQuantityAsync(Guid productId, Guid customerId, CancellationToken cancellationToken = default);
    Task<TaxResolutionResult> ResolveTaxAsync(TaxResolutionContext context, CancellationToken cancellationToken = default);
    Task<DiscountResolutionResult> ResolveDiscountAsync(DiscountResolutionContext context, CancellationToken cancellationToken = default);
}

public interface IAccountingPeriodRepository
{
    Task<AccountingPeriod?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AccountingPeriod?> GetByMonthAsync(int year, int month, CancellationToken cancellationToken = default);
    Task<AccountingPeriod?> GetByDateAsync(DateTime postingDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AccountingPeriod>> ListAsync(int? year = null, CancellationToken cancellationToken = default);
    Task<AccountingPeriod> GetOrCreateForDateAsync(DateTime postingDate, CancellationToken cancellationToken = default);
    Task AddAsync(AccountingPeriod period, CancellationToken cancellationToken = default);
    void Update(AccountingPeriod period);
}

public interface IGLAccountRepository
{
    Task<GLAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<GLAccount?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<bool> CodeExistsAsync(string code, Guid? excludeId, CancellationToken cancellationToken = default);
    Task<bool> HasChildrenAsync(Guid parentId, CancellationToken cancellationToken = default);
    /// <summary>Flat list filtered by optional type / active / postable / parent.</summary>
    Task<IReadOnlyList<GLAccount>> ListAsync(
        Domain.Enums.AccountType? type,
        bool? isActive,
        bool? isPostable,
        Guid? parentId,
        CancellationToken cancellationToken = default);
    /// <summary>Full tenant chart for tree rendering — bounded by tenant filter; no paging.</summary>
    Task<IReadOnlyList<GLAccount>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(GLAccount account, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<GLAccount> accounts, CancellationToken cancellationToken = default);
    void Update(GLAccount account);
    void Remove(GLAccount account);
}

public interface IJournalEntryRepository
{
    Task<JournalEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<JournalEntry?> GetWithLinesAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> NumberExistsAsync(string number, Guid? excludeId, CancellationToken cancellationToken = default);
    Task<bool> ExistsForSourceAsync(Domain.Enums.JournalSourceType sourceType, Guid sourceDocumentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The non-reversed entry (with lines) for a source key, or null when none
    /// exists or the only match has been reversed. Lets the year-end close treat a
    /// reversed Kapanis as "absent" so a corrected close can re-post under the same
    /// deterministic id with a fresh number.
    /// </summary>
    Task<JournalEntry?> GetActiveBySourceAsync(Domain.Enums.JournalSourceType sourceType, Guid sourceDocumentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Most-recent posted entry of a given source type with a posting date strictly
    /// before <paramref name="beforePostingDate"/>, including its lines. Used by the
    /// FX revaluation job to reverse the prior mark so consecutive runs net to the
    /// current position instead of accumulating.
    /// </summary>
    // Cross-tenant on purpose: the FX revaluation job runs without an ambient tenant and has to
    // find tenants whose prior mark still needs reversing even when they no longer have any open
    // foreign balance to revalue.
    Task<IReadOnlyList<Guid>> GetTenantIdsWithPostedSourceTypeBeforeAsync(
        Domain.Enums.JournalSourceType sourceType,
        DateTime beforePostingDate,
        CancellationToken cancellationToken = default);
    Task<JournalEntry?> GetMostRecentBySourceTypeBeforeAsync(
        Domain.Enums.JournalSourceType sourceType,
        DateTime beforePostingDate,
        CancellationToken cancellationToken = default);

    /// <summary>Search with paging — list view skips the lines collection.</summary>
    Task<(IReadOnlyList<JournalEntrySearchRow> Items, int Total)> SearchAsync(
        string? search,
        Domain.Enums.JournalEntryType? type,
        Domain.Enums.JournalEntryStatus? status,
        DateTime? fromDate,
        DateTime? toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>Aggregate posted lines for trial balance / mizan reporting.</summary>
    Task<IReadOnlyList<AccountBalanceRow>> GetAccountBalancesAsync(
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cumulative as-of variant: sums ALL posted history up to and including
    /// <paramref name="asOf"/> (no lower bound). Full history is the carry-forward,
    /// so this returns true account positions — the backbone of the balance sheet
    /// and the subledger-to-GL reconciliation control figures.
    /// </summary>
    Task<IReadOnlyList<AccountBalanceRow>> GetAccountBalancesAsOfAsync(
        DateTime asOf,
        CancellationToken ct = default);

    Task AddAsync(JournalEntry entry, CancellationToken cancellationToken = default);
    void Update(JournalEntry entry);
    void Remove(JournalEntry entry);
}

public record JournalEntrySearchRow(
    Guid Id,
    string Number,
    DateTime EntryDate,
    DateTime PostingDate,
    Domain.Enums.JournalEntryType Type,
    Domain.Enums.JournalEntryStatus Status,
    string? Description,
    string? Reference,
    decimal TotalDebit,
    decimal TotalCredit,
    int LineCount);

public record AccountBalanceRow(
    Guid AccountId,
    string AccountCode,
    string AccountName,
    decimal Debit,
    decimal Credit);

public interface IVendorRepository
{
    Task<Vendor?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Vendor?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<Vendor?> GetByTaxNumberAsync(string taxNumber, CancellationToken cancellationToken = default);
    Task<bool> CodeExistsAsync(string code, Guid? excludeId, CancellationToken cancellationToken = default);
    Task<bool> TaxNumberExistsAsync(string taxNumber, Guid? excludeId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<VendorSearchRow> Items, int Total)> SearchAsync(
        string? search,
        Domain.Enums.VendorStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task AddAsync(Vendor vendor, CancellationToken cancellationToken = default);
    void Update(Vendor vendor);
    void Remove(Vendor vendor);
    Task<IReadOnlyList<DuplicateGroupRow>> FindDuplicatesAsync(DuplicateKeyKind key, CancellationToken cancellationToken = default);
}

public record VendorSearchRow(
    Guid Id,
    string? Code,
    string Name,
    string? LegalName,
    string? TaxNumber,
    string? Email,
    string? Phone,
    Domain.Enums.VendorType Type,
    Domain.Enums.VendorStatus Status,
    string DefaultCurrency,
    decimal CurrentBalance,
    decimal OverdueAmount);

public interface IVendorAddressRepository
{
    Task<VendorAddress?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VendorAddress>> GetByVendorAsync(Guid vendorId, CancellationToken cancellationToken = default);
    Task AddAsync(VendorAddress address, CancellationToken cancellationToken = default);
    void Update(VendorAddress address);
    void Remove(VendorAddress address);
}

public interface IVendorContactRepository
{
    Task<VendorContact?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VendorContact>> GetByVendorAsync(Guid vendorId, CancellationToken cancellationToken = default);
    Task AddAsync(VendorContact contact, CancellationToken cancellationToken = default);
    void Update(VendorContact contact);
    void Remove(VendorContact contact);
}

public interface IVendorBankAccountRepository
{
    Task<VendorBankAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VendorBankAccount>> GetByVendorAsync(Guid vendorId, CancellationToken cancellationToken = default);
    Task AddAsync(VendorBankAccount account, CancellationToken cancellationToken = default);
    void Update(VendorBankAccount account);
    void Remove(VendorBankAccount account);
}

public interface IVendorLedgerRepository
{
    Task AddAsync(VendorLedgerEntry entry, CancellationToken cancellationToken = default);
    Task AcquireAppendLockAsync(Guid vendorId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<VendorLedgerEntry> Items, int Total)> SearchByVendorAsync(
        Guid vendorId,
        DateTime? fromUtc,
        DateTime? toUtc,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<decimal> GetCurrentBalanceAsync(Guid vendorId, CancellationToken cancellationToken = default);
    Task<decimal> GetLastRunningBalanceAsync(Guid vendorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Aggregate as-of balance across ALL vendors (Σ credit − Σ debit, the
    /// "we owe" convention) where PostingDate &lt;= asOf. Used by the
    /// subledger-to-GL reconciliation to compare against control account 320.
    /// </summary>
    Task<decimal> GetTotalBalanceAsOfAsync(DateTime asOf, CancellationToken cancellationToken = default);
}

public interface ICustomerProductPriceRepository
{
    Task<CustomerProductPrice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerProductPrice>> GetForCustomerAndProductAsync(Guid customerId, Guid productId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerProductPrice>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerProductPrice>> GetByProductAsync(Guid productId, CancellationToken cancellationToken = default);
    Task AddAsync(CustomerProductPrice price, CancellationToken cancellationToken = default);
    void Update(CustomerProductPrice price);
    void Remove(CustomerProductPrice price);
}
