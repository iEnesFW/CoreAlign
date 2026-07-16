using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities;

public class PriceList : TenantEntity, IHasConcurrencyToken
{
    public long ConcurrencyToken { get; private set; }
    void IHasConcurrencyToken.BumpConcurrencyToken() => ConcurrencyToken++;

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Currency { get; private set; } = "TRY";
    public bool IsTaxInclusive { get; private set; }
    public DateTime? ValidFromUtc { get; private set; }
    public DateTime? ValidUntilUtc { get; private set; }
    public bool IsDefault { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;

    public ICollection<PriceListItem> Items { get; private set; } = new List<PriceListItem>();

    protected PriceList() { }

    public PriceList(string code, string name, string currency, bool isTaxInclusive = false, DateTime? validFromUtc = null, DateTime? validUntilUtc = null, bool isDefault = false, string? description = null)
    {
        Code = code;
        Name = name;
        Currency = currency;
        IsTaxInclusive = isTaxInclusive;
        ValidFromUtc = validFromUtc;
        ValidUntilUtc = validUntilUtc;
        IsDefault = isDefault;
        Description = description;
    }

    public bool IsCurrentlyValid(DateTime nowUtc)
    {
        if (!IsActive) return false;
        if (ValidFromUtc is { } from && nowUtc < from) return false;
        if (ValidUntilUtc is { } until && nowUtc > until) return false;
        return true;
    }

    public void Update(
        string code,
        string name,
        string currency,
        bool isTaxInclusive,
        DateTime? validFromUtc,
        DateTime? validUntilUtc,
        bool isDefault,
        string? description,
        bool isActive)
    {
        Code = code;
        Name = name;
        Currency = currency;
        IsTaxInclusive = isTaxInclusive;
        ValidFromUtc = validFromUtc;
        ValidUntilUtc = validUntilUtc;
        IsDefault = isDefault;
        Description = description;
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
