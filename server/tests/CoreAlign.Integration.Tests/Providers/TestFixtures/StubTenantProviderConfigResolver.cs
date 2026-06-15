using CoreAlign.Application.Providers;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Integration.Tests.Providers.TestFixtures;

/// <summary>
/// In-memory <see cref="ITenantProviderConfigResolver"/> that returns a canned
/// "encrypted" credential blob — the paired <see cref="StubProviderCredentialProtector"/>
/// passes the blob through as plain JSON, so tests stay free of DataProtection keyring concerns.
/// </summary>
public sealed class StubTenantProviderConfigResolver : ITenantProviderConfigResolver
{
    private readonly Dictionary<string, string> _credentials = new(StringComparer.Ordinal);
    private readonly Dictionary<(Guid, ProviderCategory), string> _defaults = new();

    public void Configure(Guid tenantId, ProviderCategory category, string providerName, string credentialJson)
    {
        var key = BuildKey(tenantId, category, providerName);
        _credentials[key] = credentialJson;
        _defaults[(tenantId, category)] = providerName;
    }

    public Task<string?> GetDefaultProviderNameAsync(Guid tenantId, ProviderCategory category, CancellationToken cancellationToken = default)
    {
        _defaults.TryGetValue((tenantId, category), out var name);
        return Task.FromResult<string?>(name);
    }

    public Task<string?> GetEncryptedCredentialsAsync(Guid tenantId, ProviderCategory category, string providerName, CancellationToken cancellationToken = default)
    {
        _credentials.TryGetValue(BuildKey(tenantId, category, providerName), out var json);
        return Task.FromResult<string?>(json);
    }

    public Task InvalidateCacheAsync(Guid tenantId, ProviderCategory? category = null) => Task.CompletedTask;

    private static string BuildKey(Guid tenantId, ProviderCategory category, string providerName) =>
        $"{tenantId:N}|{category}|{providerName}";
}
