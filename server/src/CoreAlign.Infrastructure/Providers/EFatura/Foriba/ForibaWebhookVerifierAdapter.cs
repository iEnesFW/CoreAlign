using CoreAlign.Application.Providers;
using CoreAlign.Application.Providers.EFatura;
using CoreAlign.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Infrastructure.Providers.EFatura.Foriba;

public sealed class ForibaWebhookVerifierAdapter : IProviderWebhookVerifier
{
    private readonly ITenantProviderConfigResolver _configResolver;
    private readonly IProviderCredentialProtector _credentialProtector;
    private readonly ILogger<ForibaWebhookVerifierAdapter> _logger;

    public ForibaWebhookVerifierAdapter(
        ITenantProviderConfigResolver configResolver,
        IProviderCredentialProtector credentialProtector,
        ILogger<ForibaWebhookVerifierAdapter> logger)
    {
        _configResolver = configResolver;
        _credentialProtector = credentialProtector;
        _logger = logger;
    }

    public string ProviderName => ForibaEFaturaProvider.ProviderName;

    public async Task<bool> VerifyAsync(
        string rawBody,
        IReadOnlyDictionary<string, string> headers,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        if (!headers.TryGetValue(ForibaWebhookVerifier.SignatureHeader, out var signature) || string.IsNullOrWhiteSpace(signature))
        {
            return false;
        }

        var encrypted = await _configResolver
            .GetEncryptedCredentialsAsync(tenantId, ProviderCategory.EFatura, ForibaEFaturaProvider.ProviderName, cancellationToken)
            .ConfigureAwait(false);

        var credentials = _credentialProtector.UnprotectAs<ForibaCredentials>(tenantId, ProviderCategory.EFatura, encrypted);
        if (credentials is null || string.IsNullOrWhiteSpace(credentials.WebhookSecret))
        {
            _logger.LogWarning("Foriba webhook secret missing for tenant {TenantId}.", tenantId);
            return false;
        }

        return ForibaWebhookVerifier.Verify(rawBody ?? string.Empty, signature, credentials.WebhookSecret);
    }
}
