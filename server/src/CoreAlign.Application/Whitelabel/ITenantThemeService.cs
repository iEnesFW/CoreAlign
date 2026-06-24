using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Whitelabel;

public interface ITenantThemeService
{
    Task<TenantThemeDto> GetThemeAsync(Guid tenantId, CancellationToken ct);

    Task<TenantThemeDto> UpdateThemeAsync(Guid tenantId, UpdateTenantThemePayload payload, CancellationToken ct);

    Task<TenantThemeAssetDto> UploadAssetAsync(
        Guid tenantId,
        TenantThemeAssetKind kind,
        string fileName,
        string contentType,
        Stream content,
        CancellationToken ct);

    Task<PublicTenantThemeDto?> GetPublicThemeBySubdomainAsync(string subdomain, CancellationToken ct);

    Task<PublicTenantThemeDto?> GetPublicThemeByCustomDomainAsync(string domain, CancellationToken ct);
}
