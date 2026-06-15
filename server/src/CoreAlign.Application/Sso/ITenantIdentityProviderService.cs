namespace CoreAlign.Application.Sso;

public interface ITenantIdentityProviderService
{
    Task<IReadOnlyList<SsoIdentityProviderDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<SsoIdentityProviderDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SsoIdentityProviderDto> CreateAsync(CreateSsoIdentityProviderRequest request, CancellationToken cancellationToken = default);
    Task<SsoIdentityProviderDto> UpdateAsync(Guid id, UpdateSsoIdentityProviderRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SsoTestConnectionResult> TestConnectionAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface ISsoLoginService
{
    Task<SsoLoginResult> CompleteSamlLoginAsync(string tenantSlug, string idpName, SsoAssertionContext assertion, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default);
    Task<SsoLoginResult> CompleteOidcLoginAsync(string tenantSlug, string idpName, SsoAssertionContext assertion, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default);
    Task<string> BuildSamlRedirectUrlAsync(string tenantSlug, string idpName, string returnUrl, CancellationToken cancellationToken = default);
    Task<string> BuildOidcAuthorizeUrlAsync(string tenantSlug, string idpName, string returnUrl, string state, CancellationToken cancellationToken = default);
}

public interface IOidcDiscoveryClient
{
    Task<OidcDiscoveryDocument?> FetchAsync(string discoveryDocumentUrl, CancellationToken cancellationToken = default);
}

public record OidcDiscoveryDocument(
    string Issuer,
    string AuthorizationEndpoint,
    string TokenEndpoint,
    string UserinfoEndpoint,
    string JwksUri);

public interface ISamlMetadataClient
{
    Task<bool> ValidateMetadataAsync(string metadataUrl, CancellationToken cancellationToken = default);
}
