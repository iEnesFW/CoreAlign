using CoreAlign.Application.Common;
using CoreAlign.Application.Settings.DTOs;
using MediatR;

namespace CoreAlign.Application.Settings.Commands;

public record UpdateCompanyProfileCommand(
    string Name,
    string? LegalName,
    string? TradeName,
    string? TaxNumber,
    string? TaxOffice,
    string? NationalId,
    string? MersisNumber,
    string? TradeRegistryNumber,
    string? Sector,
    DateTime? FoundedOn,
    string? LogoUrl,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? StateProvince,
    string? PostalCode,
    string? Country,
    string? Phone,
    string? Fax,
    string? Email,
    string? Website,
    string DefaultCurrency,
    string? ReportingCurrency,
    string LocaleCode,
    string TimeZoneId,
    int FiscalYearStartMonth,
    string? PrimaryColor,
    string? SecondaryColor) : IRequest<CompanyProfileDto>, ITransactionalRequest;

public record SettingUpsertItem(
    string Category,
    string Key,
    string? Value,
    string DataType = "string",
    string? Description = null,
    bool IsSensitive = false);

public record UpsertTenantSettingsCommand(IReadOnlyList<SettingUpsertItem> Items)
    : IRequest<IReadOnlyList<TenantSettingDto>>, ITransactionalRequest;

public record DeleteTenantSettingCommand(string Category, string Key)
    : IRequest<bool>, ITransactionalRequest;

public record CreateEmailTemplateCommand(
    string Code,
    string Name,
    string Subject,
    string Body,
    string Locale = "tr-TR",
    string? Description = null,
    string? AvailableVariables = null) : IRequest<EmailTemplateDto>, ITransactionalRequest;

public record UpdateEmailTemplateCommand(
    Guid Id,
    string Name,
    string Subject,
    string Body,
    string Locale,
    string? Description,
    string? AvailableVariables,
    bool IsActive) : IRequest<EmailTemplateDto>, ITransactionalRequest;

public record DeleteEmailTemplateCommand(Guid Id) : IRequest<bool>, ITransactionalRequest;
