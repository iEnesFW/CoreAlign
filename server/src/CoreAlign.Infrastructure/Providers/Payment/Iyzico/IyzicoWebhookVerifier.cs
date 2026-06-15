using System.Security.Cryptography;
using System.Text;
using CoreAlign.Application.Providers;
using CoreAlign.Application.Providers.EFatura;
using CoreAlign.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Infrastructure.Providers.Payment.Iyzico;

public sealed class IyzicoWebhookVerifier : IProviderWebhookVerifier
{
    public const string SignatureHeader = "X-Iyzico-Signature";
    public const string LegacySignatureHeader = "x-iyzi-signature";

    private readonly ITenantProviderConfigResolver _configResolver;
    private readonly IProviderCredentialProtector _credentialProtector;
    private readonly ILogger<IyzicoWebhookVerifier> _logger;

    public IyzicoWebhookVerifier(
        ITenantProviderConfigResolver configResolver,
        IProviderCredentialProtector credentialProtector,
        ILogger<IyzicoWebhookVerifier> logger)
    {
        _configResolver = configResolver;
        _credentialProtector = credentialProtector;
        _logger = logger;
    }

    public string ProviderName => IyzicoPaymentProvider.ProviderKey;

    public async Task<bool> VerifyAsync(
        string rawBody,
        IReadOnlyDictionary<string, string> headers,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(headers);

        if (!TryGetSignature(headers, out var signature) || string.IsNullOrWhiteSpace(signature))
        {
            _logger.LogWarning("Iyzico webhook signature header missing for tenant {TenantId}.", tenantId);
            return false;
        }

        var encrypted = await _configResolver
            .GetEncryptedCredentialsAsync(tenantId, ProviderCategory.Payment, IyzicoPaymentProvider.ProviderKey, cancellationToken)
            .ConfigureAwait(false);

        var credentials = _credentialProtector.UnprotectAs<IyzicoCredentials>(tenantId, ProviderCategory.Payment, encrypted);
        if (credentials is null || string.IsNullOrWhiteSpace(credentials.WebhookSecret))
        {
            _logger.LogWarning("Iyzico webhook secret missing for tenant {TenantId}.", tenantId);
            return false;
        }

        return Verify(rawBody ?? string.Empty, signature, credentials.WebhookSecret);
    }

    public static bool Verify(string body, string signature, string secret)
    {
        if (string.IsNullOrWhiteSpace(signature) || string.IsNullOrWhiteSpace(secret))
        {
            return false;
        }

        var payloadBytes = Encoding.UTF8.GetBytes(body ?? string.Empty);
        var secretBytes = Encoding.UTF8.GetBytes(secret);

        using var hmac = new HMACSHA256(secretBytes);
        var computed = hmac.ComputeHash(payloadBytes);

        var normalized = signature.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase)
            ? signature["sha256=".Length..]
            : signature;

        if (TryFromHex(normalized, out var providedHex))
        {
            return providedHex.Length == computed.Length
                && CryptographicOperations.FixedTimeEquals(providedHex, computed);
        }

        if (TryFromBase64(normalized, out var providedBase64))
        {
            return providedBase64.Length == computed.Length
                && CryptographicOperations.FixedTimeEquals(providedBase64, computed);
        }

        return false;
    }

    private static bool TryGetSignature(IReadOnlyDictionary<string, string> headers, out string? signature)
    {
        if (headers.TryGetValue(SignatureHeader, out var primary) && !string.IsNullOrWhiteSpace(primary))
        {
            signature = primary;
            return true;
        }

        if (headers.TryGetValue(LegacySignatureHeader, out var legacy) && !string.IsNullOrWhiteSpace(legacy))
        {
            signature = legacy;
            return true;
        }

        signature = null;
        return false;
    }

    private static bool TryFromHex(string value, out byte[] bytes)
    {
        try
        {
            bytes = Convert.FromHexString(value.Trim());
            return true;
        }
        catch (FormatException)
        {
            bytes = Array.Empty<byte>();
            return false;
        }
    }

    private static bool TryFromBase64(string value, out byte[] bytes)
    {
        try
        {
            bytes = Convert.FromBase64String(value.Trim());
            return true;
        }
        catch (FormatException)
        {
            bytes = Array.Empty<byte>();
            return false;
        }
    }
}
