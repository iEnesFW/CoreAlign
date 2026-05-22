using CoreAlign.Application.Settings.Commands;
using CoreAlign.Application.Settings.DTOs;
using CoreAlign.Application.Settings.Mapping;
using CoreAlign.Application.Settings.Queries;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Settings.Handlers;

public class GetCompanyProfileHandler : IRequestHandler<GetCompanyProfileQuery, CompanyProfileDto?>
{
    private readonly ITenantRepository _tenants;
    private readonly ITenantContext _tenantContext;
    public GetCompanyProfileHandler(ITenantRepository tenants, ITenantContext tenantContext)
    {
        _tenants = tenants;
        _tenantContext = tenantContext;
    }

    public async Task<CompanyProfileDto?> Handle(GetCompanyProfileQuery q, CancellationToken ct)
    {
        var tenantId = _tenantContext.RequireTenantId();
        var tenant = await _tenants.GetByIdAsync(tenantId, ct);
        return tenant is null ? null : SettingsMapper.ToDto(tenant);
    }
}

public class UpdateCompanyProfileHandler : IRequestHandler<UpdateCompanyProfileCommand, CompanyProfileDto>
{
    private readonly ITenantRepository _tenants;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _uow;

    public UpdateCompanyProfileHandler(ITenantRepository tenants, ITenantContext tenantContext, IUnitOfWork uow)
    {
        _tenants = tenants;
        _tenantContext = tenantContext;
        _uow = uow;
    }

    public async Task<CompanyProfileDto> Handle(UpdateCompanyProfileCommand c, CancellationToken ct)
    {
        var tenantId = _tenantContext.RequireTenantId();
        var tenant = await _tenants.GetByIdAsync(tenantId, ct)
            ?? throw new TenantNotFoundException();

        tenant.UpdateProfile(
            c.Name,
            c.LegalName,
            c.TradeName,
            c.TaxNumber,
            c.TaxOffice,
            c.NationalId,
            c.MersisNumber,
            c.TradeRegistryNumber,
            c.Sector,
            c.FoundedOn,
            c.LogoUrl,
            c.AddressLine1,
            c.AddressLine2,
            c.City,
            c.StateProvince,
            c.PostalCode,
            c.Country,
            c.Phone,
            c.Fax,
            c.Email,
            c.Website,
            c.DefaultCurrency,
            c.ReportingCurrency,
            c.LocaleCode,
            c.TimeZoneId,
            c.FiscalYearStartMonth,
            c.PrimaryColor,
            c.SecondaryColor);

        _tenants.Update(tenant);
        await _uow.SaveChangesAsync(ct);
        return SettingsMapper.ToDto(tenant);
    }
}

public class GetTenantSettingsHandler : IRequestHandler<GetTenantSettingsQuery, IReadOnlyList<TenantSettingDto>>
{
    private readonly ITenantSettingRepository _repo;
    public GetTenantSettingsHandler(ITenantSettingRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<TenantSettingDto>> Handle(GetTenantSettingsQuery q, CancellationToken ct) =>
        (await _repo.ListAsync(q.Category, ct)).Select(SettingsMapper.ToDto).ToList();
}

public class UpsertTenantSettingsHandler : IRequestHandler<UpsertTenantSettingsCommand, IReadOnlyList<TenantSettingDto>>
{
    private readonly ITenantSettingRepository _repo;
    private readonly IUnitOfWork _uow;

    public UpsertTenantSettingsHandler(ITenantSettingRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<IReadOnlyList<TenantSettingDto>> Handle(UpsertTenantSettingsCommand c, CancellationToken ct)
    {
        // A "********" placeholder from the UI means "leave the existing
        // sensitive value alone" — we only overwrite when the caller submitted
        // a real new value.
        foreach (var item in c.Items)
        {
            if (item.IsSensitive && item.Value == "********")
            {
                continue;
            }
            await _repo.UpsertAsync(
                item.Category,
                item.Key,
                item.Value,
                item.DataType,
                item.Description,
                item.IsSensitive,
                ct);
        }
        await _uow.SaveChangesAsync(ct);
        var categories = c.Items.Select(i => i.Category).Distinct().ToArray();
        var refreshed = new List<TenantSetting>();
        foreach (var cat in categories)
        {
            refreshed.AddRange(await _repo.ListAsync(cat, ct));
        }
        return refreshed.Select(SettingsMapper.ToDto).ToList();
    }
}

public class DeleteTenantSettingHandler : IRequestHandler<DeleteTenantSettingCommand, bool>
{
    private readonly ITenantSettingRepository _repo;
    private readonly IUnitOfWork _uow;
    public DeleteTenantSettingHandler(ITenantSettingRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<bool> Handle(DeleteTenantSettingCommand c, CancellationToken ct)
    {
        await _repo.DeleteAsync(c.Category, c.Key, ct);
        await _uow.SaveChangesAsync(ct);
        return true;
    }
}

public class GetEmailTemplatesHandler : IRequestHandler<GetEmailTemplatesQuery, IReadOnlyList<EmailTemplateDto>>
{
    private readonly IEmailTemplateRepository _repo;
    public GetEmailTemplatesHandler(IEmailTemplateRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<EmailTemplateDto>> Handle(GetEmailTemplatesQuery q, CancellationToken ct) =>
        (await _repo.ListAsync(ct)).Select(SettingsMapper.ToDto).ToList();
}

public class GetEmailTemplateByIdHandler : IRequestHandler<GetEmailTemplateByIdQuery, EmailTemplateDto?>
{
    private readonly IEmailTemplateRepository _repo;
    public GetEmailTemplateByIdHandler(IEmailTemplateRepository repo) => _repo = repo;

    public async Task<EmailTemplateDto?> Handle(GetEmailTemplateByIdQuery q, CancellationToken ct)
    {
        var template = await _repo.GetByIdAsync(q.Id, ct);
        return template is null ? null : SettingsMapper.ToDto(template);
    }
}

public class CreateEmailTemplateHandler : IRequestHandler<CreateEmailTemplateCommand, EmailTemplateDto>
{
    private readonly IEmailTemplateRepository _repo;
    private readonly IUnitOfWork _uow;

    public CreateEmailTemplateHandler(IEmailTemplateRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<EmailTemplateDto> Handle(CreateEmailTemplateCommand c, CancellationToken ct)
    {
        var existing = await _repo.GetByCodeAsync(c.Code, c.Locale, ct);
        if (existing is not null)
        {
            throw new EmailTemplateConflictException(c.Code, c.Locale);
        }
        var template = new EmailTemplate(c.Code, c.Name, c.Subject, c.Body, c.Locale);
        template.Update(c.Name, c.Subject, c.Body, c.Locale, c.Description, c.AvailableVariables, true);
        await _repo.AddAsync(template, ct);
        await _uow.SaveChangesAsync(ct);
        return SettingsMapper.ToDto(template);
    }
}

public class UpdateEmailTemplateHandler : IRequestHandler<UpdateEmailTemplateCommand, EmailTemplateDto>
{
    private readonly IEmailTemplateRepository _repo;
    private readonly IUnitOfWork _uow;

    public UpdateEmailTemplateHandler(IEmailTemplateRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<EmailTemplateDto> Handle(UpdateEmailTemplateCommand c, CancellationToken ct)
    {
        var template = await _repo.GetByIdAsync(c.Id, ct)
            ?? throw new EmailTemplateNotFoundException(c.Id);
        template.Update(c.Name, c.Subject, c.Body, c.Locale, c.Description, c.AvailableVariables, c.IsActive);
        _repo.Update(template);
        await _uow.SaveChangesAsync(ct);
        return SettingsMapper.ToDto(template);
    }
}

public class DeleteEmailTemplateHandler : IRequestHandler<DeleteEmailTemplateCommand, bool>
{
    private readonly IEmailTemplateRepository _repo;
    private readonly IUnitOfWork _uow;
    public DeleteEmailTemplateHandler(IEmailTemplateRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<bool> Handle(DeleteEmailTemplateCommand c, CancellationToken ct)
    {
        var template = await _repo.GetByIdAsync(c.Id, ct)
            ?? throw new EmailTemplateNotFoundException(c.Id);
        _repo.Remove(template);
        await _uow.SaveChangesAsync(ct);
        return true;
    }
}
