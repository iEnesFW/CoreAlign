using System.Text;

namespace CoreAlign.Domain.Entities;

/// <summary>
/// Tenant (firma) master. Wave 10 extended the entity with company-info fields
/// — these surface on the Settings panel and feed invoice print headers,
/// e-Fatura sender info, and outbound email "From" defaults.
/// </summary>
public class Tenant
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    // ---------- Company identity ----------
    public string? LegalName { get; set; }
    public string? TradeName { get; set; }
    public string? TaxNumber { get; set; }
    public string? TaxOffice { get; set; }
    public string? NationalId { get; set; }
    public string? MersisNumber { get; set; }
    public string? TradeRegistryNumber { get; set; }
    public string? Sector { get; set; }
    public DateTime? FoundedOn { get; set; }
    public string? LogoUrl { get; set; }

    // ---------- Address ----------
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? StateProvince { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }

    // ---------- Contact ----------
    public string? Phone { get; set; }
    public string? Fax { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }

    // ---------- Locale / Finance defaults ----------
    public string DefaultCurrency { get; set; } = "TRY";
    public string? ReportingCurrency { get; set; }
    public string LocaleCode { get; set; } = "tr-TR";
    public string TimeZoneId { get; set; } = "Europe/Istanbul";
    public int FiscalYearStartMonth { get; set; } = 1;

    // ---------- Branding ----------
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();

    protected Tenant() { }

    public Tenant(string name, string slug)
    {
        Id = Guid.NewGuid();
        Name = name;
        Slug = slug;
    }

    public void UpdateProfile(
        string name,
        string? legalName,
        string? tradeName,
        string? taxNumber,
        string? taxOffice,
        string? nationalId,
        string? mersisNumber,
        string? tradeRegistryNumber,
        string? sector,
        DateTime? foundedOn,
        string? logoUrl,
        string? addressLine1,
        string? addressLine2,
        string? city,
        string? stateProvince,
        string? postalCode,
        string? country,
        string? phone,
        string? fax,
        string? email,
        string? website,
        string defaultCurrency,
        string? reportingCurrency,
        string localeCode,
        string timeZoneId,
        int fiscalYearStartMonth,
        string? primaryColor,
        string? secondaryColor)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Tenant name is required.", nameof(name));
        if (fiscalYearStartMonth < 1 || fiscalYearStartMonth > 12)
            throw new ArgumentOutOfRangeException(nameof(fiscalYearStartMonth), "Fiscal year start month must be 1-12.");

        Name = name.Trim();
        LegalName = legalName?.Trim();
        TradeName = tradeName?.Trim();
        TaxNumber = taxNumber?.Trim();
        TaxOffice = taxOffice?.Trim();
        NationalId = nationalId?.Trim();
        MersisNumber = mersisNumber?.Trim();
        TradeRegistryNumber = tradeRegistryNumber?.Trim();
        Sector = sector?.Trim();
        FoundedOn = foundedOn;
        LogoUrl = logoUrl?.Trim();
        AddressLine1 = addressLine1?.Trim();
        AddressLine2 = addressLine2?.Trim();
        City = city?.Trim();
        StateProvince = stateProvince?.Trim();
        PostalCode = postalCode?.Trim();
        Country = country?.Trim();
        Phone = phone?.Trim();
        Fax = fax?.Trim();
        Email = email?.Trim();
        Website = website?.Trim();
        DefaultCurrency = defaultCurrency.Trim().ToUpperInvariant();
        ReportingCurrency = string.IsNullOrWhiteSpace(reportingCurrency) ? null : reportingCurrency.Trim().ToUpperInvariant();
        LocaleCode = string.IsNullOrWhiteSpace(localeCode) ? "tr-TR" : localeCode.Trim();
        TimeZoneId = string.IsNullOrWhiteSpace(timeZoneId) ? "Europe/Istanbul" : timeZoneId.Trim();
        FiscalYearStartMonth = fiscalYearStartMonth;
        PrimaryColor = primaryColor?.Trim();
        SecondaryColor = secondaryColor?.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public static string GenerateSlug(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return Guid.NewGuid().ToString("N")[..8];
        }

        var builder = new StringBuilder(input.Length);
        foreach (var c in input.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c))
            {
                builder.Append(c);
            }
            else if (c is ' ' or '-' or '_' or '.')
            {
                builder.Append('-');
            }
        }

        var slug = builder.ToString();
        while (slug.Contains("--"))
        {
            slug = slug.Replace("--", "-");
        }

        slug = slug.Trim('-');
        return string.IsNullOrEmpty(slug) ? Guid.NewGuid().ToString("N")[..8] : slug;
    }
}
