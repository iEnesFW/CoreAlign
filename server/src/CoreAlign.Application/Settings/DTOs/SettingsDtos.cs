namespace CoreAlign.Application.Settings.DTOs;

public class CompanyProfileDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
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

    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? StateProvince { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }

    public string? Phone { get; set; }
    public string? Fax { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }

    public string DefaultCurrency { get; set; } = "TRY";
    public string? ReportingCurrency { get; set; }
    public string LocaleCode { get; set; } = "tr-TR";
    public string TimeZoneId { get; set; } = "Europe/Istanbul";
    public int FiscalYearStartMonth { get; set; } = 1;

    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }
}

public class TenantSettingDto
{
    public Guid Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string DataType { get; set; } = "string";
    public string? Description { get; set; }
    public bool IsSensitive { get; set; }
}

public class EmailTemplateDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Locale { get; set; } = "tr-TR";
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
    public string? AvailableVariables { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
