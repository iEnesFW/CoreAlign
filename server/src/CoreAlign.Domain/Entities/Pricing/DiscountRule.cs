using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities.Pricing;

public enum DiscountRuleScope
{
    Global = 0,
    CustomerGroup = 1,
    ProductCategory = 2,
    Product = 3,
}

public enum DiscountValueType
{
    Percent = 0,
    FixedAmount = 1,
}

public class DiscountRule : TenantEntity, IHasConcurrencyToken
{
    public long ConcurrencyToken { get; private set; }
    void IHasConcurrencyToken.BumpConcurrencyToken() => ConcurrencyToken++;

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public DiscountRuleScope Scope { get; private set; }
    public Guid? CustomerGroupId { get; private set; }
    public Guid? ProductCategoryId { get; private set; }
    public Guid? ProductId { get; private set; }
    public DateTime? ValidFromUtc { get; private set; }
    public DateTime? ValidUntilUtc { get; private set; }
    public decimal? MinQuantity { get; private set; }
    public DiscountValueType ValueType { get; private set; }
    public decimal Value { get; private set; }
    public int Priority { get; private set; }
    public bool IsActive { get; private set; } = true;
    public string? Description { get; private set; }

    protected DiscountRule() { }

    public DiscountRule(
        string code,
        string name,
        DiscountRuleScope scope,
        DiscountValueType valueType,
        decimal value,
        Guid? customerGroupId = null,
        Guid? productCategoryId = null,
        Guid? productId = null,
        DateTime? validFromUtc = null,
        DateTime? validUntilUtc = null,
        decimal? minQuantity = null,
        int priority = 0,
        string? description = null)
    {
        EnsureValueRange(valueType, value);
        EnsureScopeConsistency(scope, customerGroupId, productCategoryId, productId);
        EnsureWindow(validFromUtc, validUntilUtc);
        Code = code;
        Name = name;
        Scope = scope;
        ValueType = valueType;
        Value = value;
        CustomerGroupId = customerGroupId;
        ProductCategoryId = productCategoryId;
        ProductId = productId;
        ValidFromUtc = validFromUtc;
        ValidUntilUtc = validUntilUtc;
        MinQuantity = minQuantity;
        Priority = priority;
        Description = description;
    }

    public void Update(
        string name,
        DiscountRuleScope scope,
        DiscountValueType valueType,
        decimal value,
        Guid? customerGroupId,
        Guid? productCategoryId,
        Guid? productId,
        DateTime? validFromUtc,
        DateTime? validUntilUtc,
        decimal? minQuantity,
        int priority,
        bool isActive,
        string? description)
    {
        EnsureValueRange(valueType, value);
        EnsureScopeConsistency(scope, customerGroupId, productCategoryId, productId);
        EnsureWindow(validFromUtc, validUntilUtc);
        Name = name;
        Scope = scope;
        ValueType = valueType;
        Value = value;
        CustomerGroupId = customerGroupId;
        ProductCategoryId = productCategoryId;
        ProductId = productId;
        ValidFromUtc = validFromUtc;
        ValidUntilUtc = validUntilUtc;
        MinQuantity = minQuantity;
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
        Guid? customerGroupId,
        Guid? productCategoryId,
        Guid productId,
        decimal quantity,
        DateTime nowUtc)
    {
        if (!IsCurrentlyValid(nowUtc)) return false;
        if (MinQuantity is { } min && quantity < min) return false;

        return Scope switch
        {
            DiscountRuleScope.Global => true,
            DiscountRuleScope.CustomerGroup => customerGroupId.HasValue && CustomerGroupId == customerGroupId,
            DiscountRuleScope.ProductCategory => productCategoryId.HasValue && ProductCategoryId == productCategoryId,
            DiscountRuleScope.Product => ProductId == productId,
            _ => false,
        };
    }

    public decimal ApplyTo(decimal lineSubtotal)
    {
        if (lineSubtotal <= 0m) return 0m;
        return ValueType switch
        {
            DiscountValueType.Percent => Math.Min(lineSubtotal, lineSubtotal * Value / 100m),
            DiscountValueType.FixedAmount => Math.Min(lineSubtotal, Value),
            _ => 0m,
        };
    }

    private static void EnsureValueRange(DiscountValueType type, decimal value)
    {
        if (value < 0m)
        {
            throw new ArgumentException("Discount value must be non-negative.", nameof(value));
        }
        if (type == DiscountValueType.Percent && value > 100m)
        {
            throw new ArgumentException("Percent discount cannot exceed 100.", nameof(value));
        }
    }

    private static void EnsureScopeConsistency(
        DiscountRuleScope scope,
        Guid? customerGroupId,
        Guid? productCategoryId,
        Guid? productId)
    {
        switch (scope)
        {
            case DiscountRuleScope.CustomerGroup when !customerGroupId.HasValue:
                throw new ArgumentException("CustomerGroup scope requires CustomerGroupId.", nameof(customerGroupId));
            case DiscountRuleScope.ProductCategory when !productCategoryId.HasValue:
                throw new ArgumentException("ProductCategory scope requires ProductCategoryId.", nameof(productCategoryId));
            case DiscountRuleScope.Product when !productId.HasValue:
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
