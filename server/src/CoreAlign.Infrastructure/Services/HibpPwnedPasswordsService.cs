using System.Security.Cryptography;
using System.Text;
using CoreAlign.Application.Auth.Services;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Infrastructure.Services;

public sealed class HibpPwnedPasswordsService : IPwnedPasswordsService
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(3);

    private readonly HttpClient _httpClient;
    private readonly ILogger<HibpPwnedPasswordsService> _logger;

    public HibpPwnedPasswordsService(HttpClient httpClient, ILogger<HibpPwnedPasswordsService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> IsPwnedAsync(string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(password)) return false;

        var (prefix, suffix) = HashAndSplit(password);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(RequestTimeout);

        try
        {
            using var response = await _httpClient.GetAsync($"range/{prefix}", timeoutCts.Token);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("HIBP range lookup returned {StatusCode}; treating password as not pwned.", (int)response.StatusCode);
                return false;
            }

            var body = await response.Content.ReadAsStringAsync(timeoutCts.Token);
            return ContainsSuffix(body, suffix);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            _logger.LogWarning(ex, "HIBP range lookup failed; treating password as not pwned.");
            return false;
        }
    }

    private static (string Prefix, string Suffix) HashAndSplit(string password)
    {
        var bytes = SHA1.HashData(Encoding.UTF8.GetBytes(password));
        var hex = Convert.ToHexString(bytes);
        return (hex[..5], hex[5..]);
    }

    private static bool ContainsSuffix(string body, string suffix)
    {
        var span = body.AsSpan();
        var lineStart = 0;
        for (var i = 0; i <= span.Length; i++)
        {
            if (i == span.Length || span[i] == '\n')
            {
                var line = span[lineStart..i].TrimEnd('\r');
                var colon = line.IndexOf(':');
                var hash = colon >= 0 ? line[..colon] : line;
                if (hash.Equals(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                lineStart = i + 1;
            }
        }
        return false;
    }
}
