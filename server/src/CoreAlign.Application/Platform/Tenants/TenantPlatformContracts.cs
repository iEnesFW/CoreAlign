using CoreAlign.Application.Common;
using MediatR;

namespace CoreAlign.Application.Platform.Tenants;

public sealed record PlatformTenantDto(
    Guid Id,
    string Name,
    string Slug,
    string? LegalName,
    string? DpoContactName,
    string? DpoContactEmail,
    bool IsActive,
    bool IsArchived,
    DateTime? ArchivedAtUtc,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record ListPlatformTenantsQuery(string? Search, int Page = 1, int PageSize = 20, bool IncludeArchived = false)
    : IRequest<PagedResult<PlatformTenantDto>>;

public sealed record GetPlatformTenantQuery(Guid Id) : IRequest<PlatformTenantDto?>;

public sealed record UpdatePlatformTenantCommand(Guid Id, string Name, string Slug, string? DpoContactName, string? DpoContactEmail)
    : IRequest<PlatformTenantDto>, ITransactionalRequest;

public sealed record ArchivePlatformTenantCommand(Guid Id)
    : IRequest<bool>, ITransactionalRequest;

public sealed record RestorePlatformTenantCommand(Guid Id)
    : IRequest<bool>, ITransactionalRequest;
