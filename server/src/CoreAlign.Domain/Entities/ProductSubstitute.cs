using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities;

public class ProductSubstitute : TenantEntity, IHasConcurrencyToken
{
    public Guid ProductId { get; private set; }
    public Guid SubstituteProductId { get; private set; }
    public decimal ConversionRate { get; private set; } = 1m;
    public bool IsBidirectional { get; private set; }
    public int Priority { get; private set; }
    public string? Notes { get; private set; }
    public long ConcurrencyToken { get; private set; }

    void IHasConcurrencyToken.BumpConcurrencyToken() => ConcurrencyToken++;

    protected ProductSubstitute() { }

    public ProductSubstitute(
        Guid productId,
        Guid substituteProductId,
        decimal conversionRate = 1m,
        bool isBidirectional = false,
        int priority = 0,
        string? notes = null)
    {
        if (productId == substituteProductId)
            throw new ArgumentException("Product cannot substitute for itself");
        ProductId = productId;
        SubstituteProductId = substituteProductId;
        ConversionRate = conversionRate > 0 ? conversionRate : 1m;
        IsBidirectional = isBidirectional;
        Priority = priority;
        Notes = notes;
    }

    public void Update(decimal conversionRate, bool isBidirectional, int priority, string? notes)
    {
        ConversionRate = conversionRate > 0 ? conversionRate : 1m;
        IsBidirectional = isBidirectional;
        Priority = priority;
        Notes = notes;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
