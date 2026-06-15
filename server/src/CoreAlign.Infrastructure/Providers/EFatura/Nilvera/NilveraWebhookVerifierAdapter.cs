using System.Text;
using CoreAlign.Application.Providers.EFatura;

namespace CoreAlign.Infrastructure.Providers.EFatura.Nilvera;

public sealed class NilveraWebhookVerifierAdapter : IProviderWebhookVerifier
{
    private readonly NilveraWebhookVerifier _innerVerifier;

    public NilveraWebhookVerifierAdapter(NilveraWebhookVerifier innerVerifier)
    {
        _innerVerifier = innerVerifier;
    }

    public string ProviderName => NilveraEFaturaProvider.ProviderKey;

    public Task<bool> VerifyAsync(
        string rawBody,
        IReadOnlyDictionary<string, string> headers,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var signatureHeader = headers.TryGetValue(NilveraWebhookVerifier.SignatureHeaderName, out var value) ? value : null;
        var bodyBytes = Encoding.UTF8.GetBytes(rawBody ?? string.Empty);
        return _innerVerifier.VerifyAsync(tenantId, bodyBytes, signatureHeader, cancellationToken);
    }
}
