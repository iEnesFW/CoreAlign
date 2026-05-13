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
