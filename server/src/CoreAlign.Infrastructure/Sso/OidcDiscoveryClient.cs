using System.Text.Json;
using CoreAlign.Application.Sso;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Infrastructure.Sso;

public class OidcDiscoveryClient : IOidcDiscoveryClient
{
    public const string HttpClientName = "CoreAlign.Sso.Oidc";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OidcDiscoveryClient> _logger;

    public OidcDiscoveryClient(IHttpClientFactory httpClientFactory, ILogger<OidcDiscoveryClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<OidcDiscoveryDocument?> FetchAsync(string discoveryDocumentUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(discoveryDocumentUrl)) return null;

        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            using var response = await client.GetAsync(discoveryDocumentUrl, cancellationToken);
            if (!response.IsSuccessStatusCode) return null;

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = doc.RootElement;
            return new OidcDiscoveryDocument(
                Issuer: GetString(root, "issuer"),
                AuthorizationEndpoint: GetString(root, "authorization_endpoint"),
                TokenEndpoint: GetString(root, "token_endpoint"),
                UserinfoEndpoint: GetString(root, "userinfo_endpoint"),
                JwksUri: GetString(root, "jwks_uri"));
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "OIDC discovery fetch failed for {Url}", discoveryDocumentUrl);
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "OIDC discovery JSON parse failed for {Url}", discoveryDocumentUrl);
            return null;
        }
    }

    private static string GetString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;
}
