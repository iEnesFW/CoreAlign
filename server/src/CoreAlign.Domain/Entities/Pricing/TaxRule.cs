using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities.Pricing;

public enum TaxRuleScope
{
    Global = 0,
    Region = 1,
    ProductClass = 2,
    RegionAndProductClass = 3,
    Product = 4,
}

public class TaxRule : TenantEntity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public TaxRuleScope Scope { get; private set; }
    public string? RegionCode { get; private set; }
    public string? ProductClass { get; private set; }
    public Guid? ProductCategoryId { get; private set; }
    public Guid? ProductId { get; private set; }
    public decimal RatePercent { get; private set; }
    public Guid? FallbackTaxRateId { get; private set; }
    public DateTime? ValidFromUtc { get; private set; }
    public DateTime? ValidUntilUtc { get; private set; }
    public int Priority { get; private set; }
    public bool IsActive { get; private set; } = true;
    public string? Description { get; private set; }

    protected TaxRule() { }

    public TaxRule(
        string code,
        string name,
        TaxRuleScope scope,
        decimal ratePercent,
        string? regionCode = null,
        string? productClass = null,
        Guid? productCategoryId = null,
        Guid? productId = null,
        Guid? fallbackTaxRateId = null,
        DateTime? validFromUtc = null,
        DateTime? validUntilUtc = null,
        int priority = 0,
        string? description = null)
    {
        EnsureRate(ratePercent);
        EnsureScopeConsistency(scope, regionCode, productClass, productCategoryId, productId);
        EnsureWindow(validFromUtc, validUntilUtc);
        Code = code;
        Name = name;
        Scope = scope;
        RatePercent = ratePercent;
        RegionCode = regionCode;
        ProductClass = productClass;
        ProductCategoryId = productCategoryId;
        ProductId = productId;
        FallbackTaxRateId = fallbackTaxRateId;
        ValidFromUtc = validFromUtc;
        ValidUntilUtc = validUntilUtc;
        Priority = priority;
        Description = description;
    }

    public void Update(
        string name,
        TaxRuleScope scope,
        decimal ratePercent,
        string? regionCode,
        string? productClass,
        Guid? productCategoryId,
        Guid? productId,
        Guid? fallbackTaxRateId,
        DateTime? validFromUtc,
        DateTime? validUntilUtc,
        int priority,
        bool isActive,
        string? description)
    {
        EnsureRate(ratePercent);
        EnsureScopeConsistency(scope, regionCode, productClass, productCategoryId, productId);
        EnsureWindow(validFromUtc, validUntilUtc);
        Name = name;
        Scope = scope;
        RatePercent = ratePercent;
        RegionCode = regionCode;
        ProductClass = productClass;
        ProductCategoryId = productCategoryId;
        ProductId = productId;
        FallbackTaxRateId = fallbackTaxRateId;
        ValidFromUtc = validFromUtc;
        ValidUntilUtc = validUntilUtc;
        Priority = priority;
        IsActive = isActive;
        Description = description;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public bool IsCurrentlyValid(DateTime nowUtc)
    {
        if (!IsActive) return false;
        if (ValidFromUtc is { } from && nowUtc < from) return false;
        if (ValidUntilUtc is { } until && nowUtc > until) return false;
        return true;
    }

    public bool MatchesContext(
        string? regionCode,
        string? productClass,
        Guid? productCategoryId,
        Guid productId,
        DateTime nowUtc)
    {
        if (!IsCurrentlyValid(nowUtc)) return false;

        return Scope switch
        {
            TaxRuleScope.Global => true,
            TaxRuleScope.Region => RegionMatches(regionCode),
            TaxRuleScope.ProductClass => ClassOrCategoryMatches(productClass, productCategoryId),
            TaxRuleScope.RegionAndProductClass => RegionMatches(regionCode)
                && ClassOrCategoryMatches(productClass, productCategoryId),
            TaxRuleScope.Product => ProductId == productId,
            _ => false,
        };
    }

    private bool RegionMatches(string? regionCode) =>
        !string.IsNullOrWhiteSpace(RegionCode)
        && !string.IsNullOrWhiteSpace(regionCode)
        && string.Equals(RegionCode, regionCode, StringComparison.OrdinalIgnoreCase);

    private bool ClassOrCategoryMatches(string? productClass, Guid? productCategoryId)
    {
        if (!string.IsNullOrWhiteSpace(ProductClass))
        {
            return !string.IsNullOrWhiteSpace(productClass)
                && string.Equals(ProductClass, productClass, StringComparison.OrdinalIgnoreCase);
        }
        return ProductCategoryId.HasValue && ProductCategoryId == productCategoryId;
    }

    private static void EnsureRate(decimal ratePercent)
    {
        if (ratePercent < 0m || ratePercent > 100m)
        {
            throw new ArgumentException("Tax rate must be between 0 and 100.", nameof(ratePercent));
        }
    }

    private static void EnsureScopeConsistency(
        TaxRuleScope scope,
        string? regionCode,
        string? productClass,
        Guid? productCategoryId,
        Guid? productId)
    {
        switch (scope)
        {
            case TaxRuleScope.Region when string.IsNullOrWhiteSpace(regionCode):
                throw new ArgumentException("Region scope requires RegionCode.", nameof(regionCode));
            case TaxRuleScope.ProductClass when string.IsNullOrWhiteSpace(productClass) && !productCategoryId.HasValue:
                throw new ArgumentException("ProductClass scope requires ProductClass or ProductCategoryId.", nameof(productClass));
            case TaxRuleScope.RegionAndProductClass when string.IsNullOrWhiteSpace(regionCode)
                || (string.IsNullOrWhiteSpace(productClass) && !productCategoryId.HasValue):
                throw new ArgumentException("Region+Class scope requires both RegionCode and ProductClass/ProductCategoryId.");
            case TaxRuleScope.Product when !productId.HasValue:
                throw new ArgumentException("Product scope requires ProductId.", nameof(productId));
        }
    }

    private static void EnsureWindow(DateTime? fromUtc, DateTime? untilUtc)
    {
        if (fromUtc is { } from && untilUtc is { } until && until < from)
        {
            throw new ArgumentException("ValidUntilUtc cannot precede ValidFromUtc.", nameof(untilUtc));
        }
    }
}
