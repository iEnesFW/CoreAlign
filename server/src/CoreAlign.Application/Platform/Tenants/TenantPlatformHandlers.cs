using CoreAlign.Application.B2B;
using CoreAlign.Application.Common;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Platform.Tenants;

internal static class PlatformTenantMapper
{
    public static PlatformTenantDto ToDto(Domain.Entities.Tenant t) => new(
        t.Id,
        t.Name,
        t.Slug,
        t.LegalName,
        t.DpoContactName,
        t.DpoContactEmail,
        t.IsActive,
        t.IsArchived,
        t.ArchivedAtUtc,
        t.CreatedAtUtc,
        t.UpdatedAtUtc);
}

public sealed class ListPlatformTenantsHandler : IRequestHandler<ListPlatformTenantsQuery, PagedResult<PlatformTenantDto>>
{
    private readonly IPlatformTenantRepository _repo;
    public ListPlatformTenantsHandler(IPlatformTenantRepository repo) => _repo = repo;

    public async Task<PagedResult<PlatformTenantDto>> Handle(ListPlatformTenantsQuery query, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, query.Page);
        var size = Math.Clamp(query.PageSize, 1, 200);
        var (items, total) = await _repo.SearchAsync(query.Search, page, size, query.IncludeArchived, cancellationToken);
        return new PagedResult<PlatformTenantDto>
        {
            Items = items.Select(PlatformTenantMapper.ToDto).ToArray(),
            Total = total,
            Page = page,
            PageSize = size,
        };
    }
}

public sealed class GetPlatformTenantHandler : IRequestHandler<GetPlatformTenantQuery, PlatformTenantDto?>
{
    private readonly IPlatformTenantRepository _repo;
    public GetPlatformTenantHandler(IPlatformTenantRepository repo) => _repo = repo;

    public async Task<PlatformTenantDto?> Handle(GetPlatformTenantQuery query, CancellationToken cancellationToken)
    {
        var entity = await _repo.GetByIdAsync(query.Id, cancellationToken);
        return entity is null ? null : PlatformTenantMapper.ToDto(entity);
    }
}

public sealed class UpdatePlatformTenantHandler : IRequestHandler<UpdatePlatformTenantCommand, PlatformTenantDto>
{
    private readonly IPlatformTenantRepository _repo;
    private readonly ITenantRepository _baseRepo;
    private readonly IUnitOfWork _uow;

    public UpdatePlatformTenantHandler(IPlatformTenantRepository repo, ITenantRepository baseRepo, IUnitOfWork uow)
    {
        _repo = repo;
        _baseRepo = baseRepo;
        _uow = uow;
    }

    public async Task<PlatformTenantDto> Handle(UpdatePlatformTenantCommand cmd, CancellationToken cancellationToken)
    {
        var entity = await _repo.GetByIdAsync(cmd.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Tenant {cmd.Id} not found.");

        var normalizedSlug = (cmd.Slug ?? string.Empty).Trim().ToLowerInvariant();
        if (await _repo.SlugExistsAsync(normalizedSlug, cmd.Id, cancellationToken))
        {
            throw new InvalidOperationException("Slug already in use by another tenant.");
        }

        entity.UpdateAdminProfile(cmd.Name, normalizedSlug, cmd.DpoContactName, cmd.DpoContactEmail, DateTime.UtcNow);
        _baseRepo.Update(entity);
        await _uow.SaveChangesAsync(cancellationToken);
        return PlatformTenantMapper.ToDto(entity);
    }
}

public sealed class ArchivePlatformTenantHandler : IRequestHandler<ArchivePlatformTenantCommand, bool>
{
    private readonly IPlatformTenantRepository _repo;
    private readonly ITenantRepository _baseRepo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserAccessor _currentUser;

    public ArchivePlatformTenantHandler(IPlatformTenantRepository repo, ITenantRepository baseRepo, IUnitOfWork uow, ICurrentUserAccessor currentUser)
    {
        _repo = repo;
        _baseRepo = baseRepo;
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<bool> Handle(ArchivePlatformTenantCommand cmd, CancellationToken cancellationToken)
    {
        var entity = await _repo.GetByIdAsync(cmd.Id, cancellationToken);
        if (entity is null) return false;
        if (entity.IsArchived) return true;
        entity.Archive(_currentUser.UserId, DateTime.UtcNow);
        _baseRepo.Update(entity);
        await _uow.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public sealed class RestorePlatformTenantHandler : IRequestHandler<RestorePlatformTenantCommand, bool>
{
    private readonly IPlatformTenantRepository _repo;
    private readonly ITenantRepository _baseRepo;
    private readonly IUnitOfWork _uow;

    public RestorePlatformTenantHandler(IPlatformTenantRepository repo, ITenantRepository baseRepo, IUnitOfWork uow)
    {
        _repo = repo;
        _baseRepo = baseRepo;
        _uow = uow;
    }

    public async Task<bool> Handle(RestorePlatformTenantCommand cmd, CancellationToken cancellationToken)
    {
        var entity = await _repo.GetByIdAsync(cmd.Id, cancellationToken);
        if (entity is null) return false;
        if (!entity.IsArchived) return true;
        entity.RestoreFromArchive(DateTime.UtcNow);
        _baseRepo.Update(entity);
        await _uow.SaveChangesAsync(cancellationToken);
        return true;
    }
}
