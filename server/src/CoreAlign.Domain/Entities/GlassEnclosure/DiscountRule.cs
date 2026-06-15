using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Entities.GlassEnclosure;

public class DiscountRule : TenantEntity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public DiscountScope Scope { get; private set; } = DiscountScope.Manual;
    public Guid? CustomerGroupId { get; private set; }
    public string? CouponCode { get; private set; }
    public decimal? MinAreaM2 { get; private set; }
    public DateTime? ValidFromUtc { get; private set; }
    public DateTime? ValidUntilUtc { get; private set; }
    public DiscountKind DiscountKind { get; private set; } = DiscountKind.Percent;
    public decimal DiscountValue { get; private set; }
    public bool Stackable { get; private set; }
    public int Priority { get; private set; }
    public bool IsActive { get; private set; } = true;

    protected DiscountRule() { }

    public DiscountRule(
        string code,
        string name,
        DiscountScope scope,
        DiscountKind discountKind,
        decimal discountValue,
        Guid? customerGroupId = null,
        string? couponCode = null,
        decimal? minAreaM2 = null,
        DateTime? validFromUtc = null,
        DateTime? validUntilUtc = null,
        bool stackable = false,
        int priority = 0)
    {
        Code = code;
        Name = name;
        Scope = scope;
        DiscountKind = discountKind;
        DiscountValue = discountValue;
        CustomerGroupId = customerGroupId;
        CouponCode = couponCode;
        MinAreaM2 = minAreaM2;
        ValidFromUtc = validFromUtc;
        ValidUntilUtc = validUntilUtc;
        Stackable = stackable;
        Priority = priority;
    }

    public void Update(
        string name,
        DiscountScope scope,
        DiscountKind discountKind,
        decimal discountValue,
        Guid? customerGroupId,
        string? couponCode,
        decimal? minAreaM2,
        DateTime? validFromUtc,
        DateTime? validUntilUtc,
        bool stackable,
        int priority,
        bool isActive)
    {
        Name = name;
        Scope = scope;
        DiscountKind = discountKind;
        DiscountValue = discountValue;
        CustomerGroupId = customerGroupId;
        CouponCode = couponCode;
        MinAreaM2 = minAreaM2;
        ValidFromUtc = validFromUtc;
        ValidUntilUtc = validUntilUtc;
        Stackable = stackable;
        Priority = priority;
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
