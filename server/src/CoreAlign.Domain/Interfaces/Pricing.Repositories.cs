using CoreAlign.Domain.Entities.Pricing;

namespace CoreAlign.Domain.Interfaces;

public interface IPricingDiscountRuleRepository
{
    Task<DiscountRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DiscountRule?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DiscountRule>> ListAsync(bool? isActive = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DiscountRule>> ListActiveAtAsync(DateTime asOfUtc, CancellationToken cancellationToken = default);
    Task AddAsync(DiscountRule rule, CancellationToken cancellationToken = default);
    void Update(DiscountRule rule);
    void Remove(DiscountRule rule);
}

public interface ITaxRuleRepository
{
    Task<TaxRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TaxRule?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaxRule>> ListAsync(bool? isActive = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaxRule>> ListActiveAtAsync(DateTime asOfUtc, CancellationToken cancellationToken = default);
    Task AddAsync(TaxRule rule, CancellationToken cancellationToken = default);
    void Update(TaxRule rule);
    void Remove(TaxRule rule);
}

public record TaxResolutionContext(
    Guid ProductId,
    Guid? ProductCategoryId,
    string? ProductClass,
    Guid CustomerId,
    string? CustomerRegionCode,
    DateTime AsOfUtc);

public record TaxResolutionResult(
    decimal RatePercent,
    Guid? TaxRuleId,
    Guid? FallbackTaxRateId,
    string Source);

public record DiscountResolutionContext(
    Guid ProductId,
    Guid? ProductCategoryId,
    Guid CustomerId,
    Guid? CustomerGroupId,
    decimal Quantity,
    decimal LineSubtotal,
    DateTime AsOfUtc);

public record DiscountResolutionResult(
    decimal DiscountAmount,
    decimal DiscountPercent,
    Guid? AppliedDiscountRuleId,
    string? AppliedDiscountRuleCode);
