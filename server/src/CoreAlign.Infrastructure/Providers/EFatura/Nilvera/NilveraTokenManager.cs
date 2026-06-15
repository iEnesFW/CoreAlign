using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Infrastructure.Providers.EFatura.Nilvera;

public sealed class NilveraTokenManager
{
    private const string TokenEndpoint = "/api/v1/oauth/token";
    private const int ExpiryBufferSeconds = 60;

    private readonly IMemoryCache _cache;
    private readonly ILogger<NilveraTokenManager> _logger;

    public NilveraTokenManager(IMemoryCache cache, ILogger<NilveraTokenManager> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<string> GetAccessTokenAsync(
        HttpClient httpClient,
        Guid tenantId,
        NilveraCredentials credentials,
        string baseUrl,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(credentials);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);

        var cacheKey = BuildCacheKey(tenantId, credentials.ClientId, credentials.IsSandbox);
        if (_cache.TryGetValue<string>(cacheKey, out var cached) && !string.IsNullOrEmpty(cached))
        {
            return cached;
        }

        var token = await RequestTokenAsync(httpClient, credentials, baseUrl, refreshToken: null, cancellationToken);
        StoreToken(cacheKey, token);
        return token.AccessToken;
    }

    public async Task<string> RefreshTokenAsync(
        HttpClient httpClient,
        Guid tenantId,
        NilveraCredentials credentials,
        string baseUrl,
        string? refreshToken,
        CancellationToken cancellationToken)
    {
        var cacheKey = BuildCacheKey(tenantId, credentials.ClientId, credentials.IsSandbox);
        _cache.Remove(cacheKey);

        var token = await RequestTokenAsync(httpClient, credentials, baseUrl, refreshToken, cancellationToken);
        StoreToken(cacheKey, token);
        return token.AccessToken;
    }

    public void InvalidateToken(Guid tenantId, string clientId, bool isSandbox)
    {
        _cache.Remove(BuildCacheKey(tenantId, clientId, isSandbox));
    }

    private async Task<NilveraOAuthToken> RequestTokenAsync(
        HttpClient httpClient,
        NilveraCredentials credentials,
        string baseUrl,
        string? refreshToken,
        CancellationToken cancellationToken)
    {
        var url = baseUrl.TrimEnd('/') + TokenEndpoint;
        var requestBody = !string.IsNullOrEmpty(refreshToken)
            ? new NilveraOAuthTokenRequest("refresh_token", credentials.ClientId, credentials.ClientSecret, null, null, refreshToken)
            : !string.IsNullOrEmpty(credentials.Username)
                ? new NilveraOAuthTokenRequest("password", credentials.ClientId, credentials.ClientSecret, credentials.Username, credentials.Password, null)
                : new NilveraOAuthTokenRequest("client_credentials", credentials.ClientId, credentials.ClientSecret, null, null, null);

        using var response = await httpClient.PostAsJsonAsync(url, requestBody, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var raw = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogWarning("Nilvera OAuth token request failed: {Status} {Body}", response.StatusCode, raw);
            throw new NilveraProviderException(
                "OAUTH_FAILED",
                $"Nilvera OAuth token endpoint returned {(int)response.StatusCode}.");
        }

        var token = await response.Content.ReadFromJsonAsync<NilveraOAuthToken>(cancellationToken: cancellationToken).ConfigureAwait(false);
        if (token is null || string.IsNullOrWhiteSpace(token.AccessToken))
        {
            throw new NilveraProviderException("OAUTH_PARSE", "Nilvera OAuth response was empty or malformed.");
        }

        return token;
    }

    private void StoreToken(string cacheKey, NilveraOAuthToken token)
    {
        var ttl = TimeSpan.FromSeconds(Math.Max(token.ExpiresIn - ExpiryBufferSeconds, ExpiryBufferSeconds));
        var entry = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl,
            Size = 1,
        };
        _cache.Set(cacheKey, token.AccessToken, entry);
    }

    private static string BuildCacheKey(Guid tenantId, string clientId, bool isSandbox) =>
        $"nilvera:token:{tenantId:N}:{clientId}:{(isSandbox ? "sbx" : "prod")}";
}

public sealed class NilveraProviderException : Exception
{
    public string ErrorCode { get; }
    public string? TraceId { get; }

    public NilveraProviderException(string errorCode, string message, string? traceId = null, Exception? inner = null)
        : base(message, inner)
    {
        ErrorCode = errorCode;
        TraceId = traceId;
    }

    public static NilveraProviderException FromBody(int statusCode, string body)
    {
        try
        {
            var parsed = JsonSerializer.Deserialize<NilveraErrorResponse>(body);
            return new NilveraProviderException(
                parsed?.ErrorCode ?? $"HTTP_{statusCode}",
                parsed?.Message ?? $"Nilvera responded with status {statusCode}.",
                parsed?.TraceId);
        }
        catch (JsonException)
        {
            return new NilveraProviderException($"HTTP_{statusCode}", $"Nilvera responded with status {statusCode}: {body}");
        }
    }
}
