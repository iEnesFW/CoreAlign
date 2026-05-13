using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities;

public class TaxRate : TenantEntity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public decimal RatePercent { get; private set; }
    public bool IsWithholding { get; private set; }
    public string? CountryCode { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;

    protected TaxRate() { }

    public TaxRate(string code, string name, decimal ratePercent, bool isWithholding = false, string? countryCode = null, string? description = null)
    {
        if (ratePercent < 0m || ratePercent > 100m)
        {
            throw new ArgumentException("Tax rate must be between 0 and 100.", nameof(ratePercent));
        }
        Code = code;
        Name = name;
        RatePercent = ratePercent;
        IsWithholding = isWithholding;
        CountryCode = countryCode;
        Description = description;
    }

    public void Update(string code, string name, decimal ratePercent, bool isWithholding, string? countryCode, string? description, bool isActive)
    {
        if (ratePercent < 0m || ratePercent > 100m)
        {
            throw new ArgumentException("Tax rate must be between 0 and 100.", nameof(ratePercent));
        }
        Code = code;
        Name = name;
        RatePercent = ratePercent;
        IsWithholding = isWithholding;
        CountryCode = countryCode;
        Description = description;
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
