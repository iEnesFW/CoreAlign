using CoreAlign.Domain.Entities.Sso;

namespace CoreAlign.Application.Sso;

public record SsoIdentityProviderDto(
    Guid Id,
    Guid TenantId,
    string Name,
    SsoProtocol Protocol,
    string EntityIdOrClientId,
    string? MetadataUrl,
    string? DiscoveryDocumentUrl,
    string AttributeMappingsJson,
    bool IsActive,
    DateTime? LastUsedAtUtc,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public record CreateSsoIdentityProviderRequest(
    string Name,
    SsoProtocol Protocol,
    string EntityIdOrClientId,
    string? MetadataUrl,
    string? DiscoveryDocumentUrl,
    string? ClientSecret,
    string? AttributeMappingsJson);

public record UpdateSsoIdentityProviderRequest(
    string Name,
    string EntityIdOrClientId,
    string? MetadataUrl,
    string? DiscoveryDocumentUrl,
    string? ClientSecret,
    string? AttributeMappingsJson,
    bool IsActive);

public record SsoAssertionContext(
    string ExternalUserId,
    string Email,
    string? FirstName,
    string? LastName,
    IReadOnlyDictionary<string, string> RawClaims);

public record SsoLoginResult(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAtUtc,
    Guid UserId,
    Guid TenantId,
    string Email,
    IReadOnlyList<string> Roles);

public record SsoTestConnectionResult(bool Success, string? Message);
