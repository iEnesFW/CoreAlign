namespace CoreAlign.Application.Providers.EFatura;

public interface IWebhookSignatureVerifier
{
    Task<bool> VerifyAsync(
        string providerName,
        string rawBody,
        IReadOnlyDictionary<string, string> headers,
        Guid tenantId,
        CancellationToken cancellationToken = default);
}

public interface IProviderWebhookVerifier
{
    string ProviderName { get; }

    Task<bool> VerifyAsync(
        string rawBody,
        IReadOnlyDictionary<string, string> headers,
        Guid tenantId,
        CancellationToken cancellationToken = default);
}
