using CoreAlign.Application.Settings.DTOs;
using CoreAlign.Domain.Entities;

namespace CoreAlign.Application.Settings.Mapping;

public static class SettingsMapper
{
    public static CompanyProfileDto ToDto(Tenant t) => new()
    {
        Id = t.Id,
        Name = t.Name,
        Slug = t.Slug,
        LegalName = t.LegalName,
        TradeName = t.TradeName,
        TaxNumber = t.TaxNumber,
        TaxOffice = t.TaxOffice,
        NationalId = t.NationalId,
        MersisNumber = t.MersisNumber,
        TradeRegistryNumber = t.TradeRegistryNumber,
        Sector = t.Sector,
        FoundedOn = t.FoundedOn,
        LogoUrl = t.LogoUrl,
        AddressLine1 = t.AddressLine1,
        AddressLine2 = t.AddressLine2,
        City = t.City,
        StateProvince = t.StateProvince,
        PostalCode = t.PostalCode,
        Country = t.Country,
        Phone = t.Phone,
        Fax = t.Fax,
        Email = t.Email,
        Website = t.Website,
        DefaultCurrency = t.DefaultCurrency,
        ReportingCurrency = t.ReportingCurrency,
        LocaleCode = t.LocaleCode,
        TimeZoneId = t.TimeZoneId,
        FiscalYearStartMonth = t.FiscalYearStartMonth,
        PrimaryColor = t.PrimaryColor,
        SecondaryColor = t.SecondaryColor,
    };

    public static TenantSettingDto ToDto(TenantSetting s) => new()
    {
        Id = s.Id,
        Category = s.Category,
        Key = s.Key,
        // Mask sensitive values on the wire — the UI sees that something is set
        // (via a placeholder) but never the secret.
        Value = s.IsSensitive && !string.IsNullOrEmpty(s.Value) ? "********" : s.Value,
        DataType = s.DataType,
        Description = s.Description,
        IsSensitive = s.IsSensitive,
    };

    public static EmailTemplateDto ToDto(EmailTemplate t) => new()
    {
        Id = t.Id,
        Code = t.Code,
        Name = t.Name,
        Subject = t.Subject,
        Body = t.Body,
        Locale = t.Locale,
        IsActive = t.IsActive,
        Description = t.Description,
        AvailableVariables = t.AvailableVariables,
        UpdatedAtUtc = t.UpdatedAtUtc,
    };
}
