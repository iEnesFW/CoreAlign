using System.Net.Http.Json;
using System.Text.Json.Serialization;
using CoreAlign.Application.Auth.Services;
using CoreAlign.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreAlign.Infrastructure.Services;

/// <summary>
/// Google reCAPTCHA v3 server-side verifier. Fails OPEN when CAPTCHA is disabled
/// or unconfigured (dev/test), and CLOSED on a present-but-invalid token or a
/// score below the configured floor. Any transport error is treated as a
/// verification failure (fail-closed) when CAPTCHA is enabled.
/// </summary>
public sealed class GoogleReCaptchaVerifier : ICaptchaVerifier
{
    public const string HttpClientName = "GoogleReCaptcha";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly CaptchaOptions _options;
    private readonly ILogger<GoogleReCaptchaVerifier> _logger;

    public GoogleReCaptchaVerifier(
        IHttpClientFactory httpClientFactory,
        IOptions<CaptchaOptions> options,
        ILogger<GoogleReCaptchaVerifier> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> VerifyAsync(string? token, string action, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.SecretKey))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            using var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("secret", _options.SecretKey),
                new KeyValuePair<string, string>("response", token),
            });

            using var response = await client.PostAsync(_options.VerifyUrl, content, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("reCAPTCHA siteverify returned HTTP {Status}.", (int)response.StatusCode);
                return false;
            }

            var result = await response.Content
                .ReadFromJsonAsync<ReCaptchaResponse>(cancellationToken)
                .ConfigureAwait(false);

            if (result is null || !result.Success)
            {
                return false;
            }

            if (result.Score < _options.MinScore)
            {
                _logger.LogWarning("reCAPTCHA score {Score} below floor {Floor} for action {Action}.", result.Score, _options.MinScore, action);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "reCAPTCHA verification failed for action {Action}.", action);
            return false;
        }
    }

    private sealed record ReCaptchaResponse(
        [property: JsonPropertyName("success")] bool Success,
        [property: JsonPropertyName("score")] double Score,
        [property: JsonPropertyName("action")] string? Action,
        [property: JsonPropertyName("hostname")] string? Hostname);
}
