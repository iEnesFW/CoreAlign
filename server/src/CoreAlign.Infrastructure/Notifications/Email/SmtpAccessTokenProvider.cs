using System.Globalization;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CoreAlign.Application.Notifications.Smtp;
using CoreAlign.Domain.Exceptions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Infrastructure.Notifications.Email;

public sealed class SmtpAccessTokenProvider : ISmtpAccessTokenProvider
{
    public const string HttpClientName = "SmtpOAuth";

    private static readonly TimeSpan RenewMargin = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan MinimumLifetime = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan FallbackLifetime = TimeSpan.FromMinutes(5);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SmtpAccessTokenProvider> _logger;

    public SmtpAccessTokenProvider(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        ILogger<SmtpAccessTokenProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _logger = logger;
    }

    public async Task<string> GetAccessTokenAsync(SmtpOAuthSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var cacheKey = BuildCacheKey(settings);
        if (_cache.TryGetValue<string>(cacheKey, out var cached) && !string.IsNullOrEmpty(cached))
        {
            return cached;
        }

        var endpoint = SmtpOAuthResolver.ValidateEndpoint(settings.TokenEndpoint);
        var client = _httpClientFactory.CreateClient(HttpClientName);

        using var content = new FormUrlEncodedContent(BuildForm(settings));
        using var response = await client
            .PostAsync(new Uri(endpoint), content, cancellationToken)
            .ConfigureAwait(false);

        var payload = await ReadPayloadAsync(response, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var reason = Describe(payload) ?? $"HTTP {(int)response.StatusCode}";
            _logger.LogWarning("SMTP OAuth token endpoint rejected the request: {Reason}", reason);
            throw new SmtpOAuthTokenException(reason);
        }

        if (payload is null || string.IsNullOrWhiteSpace(payload.AccessToken))
        {
            throw new SmtpOAuthTokenException("The token endpoint returned no access token.");
        }

        _cache.Set(cacheKey, payload.AccessToken, ComputeCacheTtl(payload.ExpiresIn));
        return payload.AccessToken;
    }

    public static TimeSpan ComputeCacheTtl(int? expiresInSeconds)
    {
        var lifetime = expiresInSeconds is > 0
            ? TimeSpan.FromSeconds(expiresInSeconds.Value)
            : FallbackLifetime;
        var ttl = lifetime - RenewMargin;
        return ttl < MinimumLifetime ? MinimumLifetime : ttl;
    }

    private static IEnumerable<KeyValuePair<string, string>> BuildForm(SmtpOAuthSettings settings)
    {
        var form = new List<KeyValuePair<string, string>>
        {
            new("grant_type", settings.GrantType),
            new("client_id", settings.ClientId),
        };
        if (!string.IsNullOrWhiteSpace(settings.ClientSecret))
        {
            form.Add(new KeyValuePair<string, string>("client_secret", settings.ClientSecret));
        }
        if (settings.GrantType == SmtpOAuthGrantTypes.RefreshToken)
        {
            form.Add(new KeyValuePair<string, string>("refresh_token", settings.RefreshToken ?? string.Empty));
        }
        if (!string.IsNullOrWhiteSpace(settings.Scope))
        {
            form.Add(new KeyValuePair<string, string>("scope", settings.Scope));
        }
        return form;
    }

    private static async Task<TokenPayload?> ReadPayloadAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<TokenPayload>(ct).ConfigureAwait(false);
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static string? Describe(TokenPayload? payload)
    {
        if (payload is null) return null;
        if (!string.IsNullOrWhiteSpace(payload.ErrorDescription)) return payload.ErrorDescription;
        return string.IsNullOrWhiteSpace(payload.Error) ? null : payload.Error;
    }

    private static string BuildCacheKey(SmtpOAuthSettings settings)
    {
        var material = string.Join(
            '\u001f',
            settings.TokenEndpoint,
            settings.GrantType,
            settings.ClientId,
            settings.ClientSecret ?? string.Empty,
            settings.RefreshToken ?? string.Empty,
            settings.Scope ?? string.Empty);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(material));
        return string.Create(
            CultureInfo.InvariantCulture,
            $"smtp-oauth:{Convert.ToHexString(hash)}");
    }

    private sealed record TokenPayload(
        [property: JsonPropertyName("access_token")] string? AccessToken,
        [property: JsonPropertyName("expires_in")] int? ExpiresIn,
        [property: JsonPropertyName("error")] string? Error,
        [property: JsonPropertyName("error_description")] string? ErrorDescription);
}
