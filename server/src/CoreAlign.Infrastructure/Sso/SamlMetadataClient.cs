using CoreAlign.Application.Sso;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Infrastructure.Sso;

public class SamlMetadataClient : ISamlMetadataClient
{
    public const string HttpClientName = "CoreAlign.Sso.Saml";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SamlMetadataClient> _logger;

    public SamlMetadataClient(IHttpClientFactory httpClientFactory, ILogger<SamlMetadataClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<bool> ValidateMetadataAsync(string metadataUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(metadataUrl)) return false;

        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            using var response = await client.GetAsync(metadataUrl, cancellationToken);
            if (!response.IsSuccessStatusCode) return false;
            var contentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
            return contentType.Contains("xml", StringComparison.OrdinalIgnoreCase)
                || contentType.Contains("text", StringComparison.OrdinalIgnoreCase);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "SAML metadata fetch failed for {Url}", metadataUrl);
            return false;
        }
    }
}
