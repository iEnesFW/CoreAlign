using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Providers;

public interface ITenantProviderConfigResolver
{
    Task<string?> GetDefaultProviderNameAsync(Guid tenantId, ProviderCategory category, CancellationToken cancellationToken = default);
    Task<string?> GetEncryptedCredentialsAsync(Guid tenantId, ProviderCategory category, string providerName, CancellationToken cancellationToken = default);
    Task InvalidateCacheAsync(Guid tenantId, ProviderCategory? category = null);
}
