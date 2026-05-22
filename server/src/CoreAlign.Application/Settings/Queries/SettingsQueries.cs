using CoreAlign.Application.Settings.DTOs;
using MediatR;

namespace CoreAlign.Application.Settings.Queries;

public record GetCompanyProfileQuery() : IRequest<CompanyProfileDto?>;

public record GetTenantSettingsQuery(string? Category = null) : IRequest<IReadOnlyList<TenantSettingDto>>;

public record GetEmailTemplatesQuery() : IRequest<IReadOnlyList<EmailTemplateDto>>;

public record GetEmailTemplateByIdQuery(Guid Id) : IRequest<EmailTemplateDto?>;
