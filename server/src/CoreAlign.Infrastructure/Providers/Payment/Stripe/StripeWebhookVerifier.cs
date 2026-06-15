using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using CoreAlign.Application.Providers;
using CoreAlign.Application.Providers.EFatura;
using CoreAlign.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Infrastructure.Providers.Payment.Stripe;

/// <summary>
/// Verifies Stripe webhook signatures using the
/// <c>Stripe-Signature: t=&lt;timestamp&gt;,v1=&lt;sig&gt;[,v1=&lt;sig&gt;...]</c> scheme.
/// Computes HMAC-SHA256 over <c>"{timestamp}.{rawBody}"</c> with the tenant's webhook
/// signing secret and accepts the request only if any v1 signature matches in constant
/// time and the timestamp is within the replay tolerance window.
/// </summary>
public sealed class StripeWebhookVerifier : IProviderWebhookVerifier
{
    public const string SignatureHeaderName = "Stripe-Signature";
    public static readonly TimeSpan DefaultTolerance = TimeSpan.FromMinutes(5);

    private readonly ITenantProviderConfigResolver _configResolver;
    private readonly IProviderCredentialProtector _credentialProtector;
    private readonly ILogger<StripeWebhookVerifier> _logger;
    private readonly TimeSpan _tolerance;
    private readonly Func<DateTimeOffset> _clock;

    public StripeWebhookVerifier(
        ITenantProviderConfigResolver configResolver,
        IProviderCredentialProtector credentialProtector,
        ILogger<StripeWebhookVerifier> logger)
        : this(configResolver, credentialProtector, logger, DefaultTolerance, () => DateTimeOffset.UtcNow)
    {
    }

    internal StripeWebhookVerifier(
        ITenantProviderConfigResolver configResolver,
        IProviderCredentialProtector credentialProtector,
        ILogger<StripeWebhookVerifier> logger,
        TimeSpan tolerance,
        Func<DateTimeOffset> clock)
    {
        _configResolver = configResolver;
        _credentialProtector = credentialProtector;
        _logger = logger;
        _tolerance = tolerance;
        _clock = clock;
    }

    public string ProviderName => StripePaymentProvider.ProviderKey;

    public async Task<bool> VerifyAsync(
        string rawBody,
        IReadOnlyDictionary<string, string> headers,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        if (headers is null || !headers.TryGetValue(SignatureHeaderName, out var signatureHeader) || string.IsNullOrWhiteSpace(signatureHeader))
        {
            return false;
        }

        var encrypted = await _configResolver
            .GetEncryptedCredentialsAsync(tenantId, ProviderCategory.Payment, StripePaymentProvider.ProviderKey, cancellationToken)
            .ConfigureAwait(false);

        var credentials = _credentialProtector.UnprotectAs<StripeCredentials>(tenantId, ProviderCategory.Payment, encrypted);
        if (credentials is null || string.IsNullOrWhiteSpace(credentials.WebhookSigningSecret))
        {
            _logger.LogWarning("Stripe webhook signing secret missing for tenant {TenantId}.", tenantId);
            return false;
        }

        return Verify(rawBody ?? string.Empty, signatureHeader, credentials.WebhookSigningSecret, _tolerance, _clock());
    }

    public static bool Verify(string rawBody, string signatureHeader, string signingSecret, TimeSpan tolerance, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(signatureHeader);
        ArgumentException.ThrowIfNullOrWhiteSpace(signingSecret);
        rawBody ??= string.Empty;

        if (!TryParseHeader(signatureHeader, out var timestamp, out var signatures) || signatures.Count == 0)
        {
            return false;
        }

        var skew = now - DateTimeOffset.FromUnixTimeSeconds(timestamp);
        if (skew.Duration() > tolerance)
        {
            return false;
        }

        var payload = Encoding.UTF8.GetBytes(timestamp.ToString(CultureInfo.InvariantCulture) + "." + rawBody);
        var key = Encoding.UTF8.GetBytes(signingSecret);

        Span<byte> computed = stackalloc byte[32];
        if (!HMACSHA256.TryHashData(key, payload, computed, out var written) || written != 32)
        {
            return false;
        }

        foreach (var sig in signatures)
        {
            if (!TryFromHex(sig, out var providedBytes) || providedBytes.Length != 32)
            {
                continue;
            }
            if (CryptographicOperations.FixedTimeEquals(computed, providedBytes))
            {
                return true;
            }
        }
        return false;
    }

    private static bool TryParseHeader(string header, out long timestamp, out IReadOnlyList<string> signatures)
    {
        timestamp = 0;
        signatures = Array.Empty<string>();
        var collected = new List<string>();
        var parts = header.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var sawTimestamp = false;

        foreach (var part in parts)
        {
            var equalsIndex = part.IndexOf('=');
            if (equalsIndex <= 0 || equalsIndex == part.Length - 1)
            {
                continue;
            }
            var key = part[..equalsIndex].Trim();
            var value = part[(equalsIndex + 1)..].Trim();

            if (key.Equals("t", StringComparison.OrdinalIgnoreCase))
            {
                if (!long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out timestamp) || timestamp <= 0)
                {
                    return false;
                }
                sawTimestamp = true;
            }
            else if (key.Equals("v1", StringComparison.OrdinalIgnoreCase))
            {
                collected.Add(value);
            }
        }

        signatures = collected;
        return sawTimestamp;
    }

    private static bool TryFromHex(string value, out byte[] bytes)
    {
        bytes = Array.Empty<byte>();
        if (string.IsNullOrEmpty(value) || (value.Length % 2) != 0)
        {
            return false;
        }
        try
        {
            bytes = Convert.FromHexString(value);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
