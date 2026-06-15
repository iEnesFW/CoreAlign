using CoreAlign.Domain.Entities;

namespace CoreAlign.Application.Platform.Tenants;

public interface IPlatformTenantRepository
{
    Task<(IReadOnlyList<Tenant> Items, int Total)> SearchAsync(string? search, int page, int pageSize, bool includeArchived, CancellationToken ct);
    Task<Tenant?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<bool> SlugExistsAsync(string slug, Guid excludingId, CancellationToken ct);
}
