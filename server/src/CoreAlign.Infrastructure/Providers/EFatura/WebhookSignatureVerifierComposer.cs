using CoreAlign.Application.Providers.EFatura;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Infrastructure.Providers.EFatura;

public sealed class WebhookSignatureVerifierComposer : IWebhookSignatureVerifier
{
    private readonly IReadOnlyDictionary<string, IProviderWebhookVerifier> _verifiers;
    private readonly ILogger<WebhookSignatureVerifierComposer> _logger;

    public WebhookSignatureVerifierComposer(
        IEnumerable<IProviderWebhookVerifier> verifiers,
        ILogger<WebhookSignatureVerifierComposer> logger)
    {
        _verifiers = (verifiers ?? Array.Empty<IProviderWebhookVerifier>())
            .ToDictionary(v => v.ProviderName, StringComparer.OrdinalIgnoreCase);
        _logger = logger;
    }

    public Task<bool> VerifyAsync(
        string providerName,
        string rawBody,
        IReadOnlyDictionary<string, string> headers,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(providerName))
        {
            return Task.FromResult(false);
        }

        if (!_verifiers.TryGetValue(providerName, out var verifier))
        {
            _logger.LogWarning(
                "No webhook verifier registered for provider {Provider}; rejecting webhook for tenant {TenantId}.",
                providerName,
                tenantId);
            return Task.FromResult(false);
        }

        return verifier.VerifyAsync(rawBody ?? string.Empty, headers ?? new Dictionary<string, string>(), tenantId, cancellationToken);
    }
}
