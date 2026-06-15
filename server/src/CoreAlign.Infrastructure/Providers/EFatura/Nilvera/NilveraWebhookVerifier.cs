using System.Security.Cryptography;
using System.Text;
using CoreAlign.Application.Providers;
using CoreAlign.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Infrastructure.Providers.EFatura.Nilvera;

public sealed class NilveraWebhookVerifier
{
    public const string SignatureHeaderName = "X-Nilvera-Signature";
    private const string ProviderName = "nilvera";

    private readonly ITenantProviderConfigResolver _configResolver;
    private readonly IProviderCredentialProtector _credentialProtector;
    private readonly ILogger<NilveraWebhookVerifier> _logger;

    public NilveraWebhookVerifier(
        ITenantProviderConfigResolver configResolver,
        IProviderCredentialProtector credentialProtector,
        ILogger<NilveraWebhookVerifier> logger)
    {
        _configResolver = configResolver;
        _credentialProtector = credentialProtector;
        _logger = logger;
    }

    public async Task<bool> VerifyAsync(
        Guid tenantId,
        ReadOnlyMemory<byte> body,
        string? signatureHeader,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(signatureHeader))
        {
            return false;
        }

        var encrypted = await _configResolver
            .GetEncryptedCredentialsAsync(tenantId, ProviderCategory.EFatura, ProviderName, cancellationToken)
            .ConfigureAwait(false);

        var credentials = _credentialProtector.UnprotectAs<NilveraCredentials>(tenantId, ProviderCategory.EFatura, encrypted);
        if (credentials is null || string.IsNullOrWhiteSpace(credentials.WebhookSecret))
        {
            _logger.LogWarning("Nilvera webhook secret missing for tenant {TenantId}.", tenantId);
            return false;
        }

        return Verify(body.Span, signatureHeader!, credentials.WebhookSecret);
    }

    public static bool Verify(ReadOnlySpan<byte> body, string signatureHeader, string secret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(signatureHeader);
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);

        var providedBytes = TryDecodeSignature(signatureHeader);
        if (providedBytes is null)
        {
            return false;
        }

        Span<byte> computed = stackalloc byte[32];
        var secretBytes = Encoding.UTF8.GetBytes(secret);
        if (!HMACSHA256.TryHashData(secretBytes, body, computed, out var written) || written != 32)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(computed, providedBytes);
    }

    private static byte[]? TryDecodeSignature(string header)
    {
        var value = header.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase) ? header[7..] : header;
        try
        {
            if (value.Length == 64 && IsHex(value))
            {
                return Convert.FromHexString(value);
            }
            return Convert.FromBase64String(value);
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static bool IsHex(string s)
    {
        foreach (var c in s)
        {
            var isHex = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
            if (!isHex) return false;
        }
        return true;
    }
}
