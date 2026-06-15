using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Integration.Tests.Providers.TestFixtures;

/// <summary>In-memory provider config repository to satisfy <see cref="PaymentDispatcher"/> dependency.</summary>
public sealed class InMemoryTenantProviderConfigRepository : ITenantProviderConfigRepository
{
    private readonly List<TenantProviderConfig> _configs = new();

    public void Add(Guid tenantId, ProviderCategory category, string providerName, bool isDefault = true, bool isEnabled = true)
    {
        var config = new TenantProviderConfig(category, providerName, providerName, isDefault, isEnabled)
        {
            TenantId = tenantId,
        };
        _configs.Add(config);
    }

    public Task<TenantProviderConfig?> GetByTenantAndCategoryAsync(Guid tenantId, ProviderCategory category, string providerName, CancellationToken cancellationToken = default) =>
        Task.FromResult(_configs.FirstOrDefault(c =>
            c.TenantId == tenantId
            && c.Category == category
            && string.Equals(c.ProviderName, providerName, StringComparison.OrdinalIgnoreCase)));

    public Task<TenantProviderConfig?> GetDefaultForTenantAsync(Guid tenantId, ProviderCategory category, CancellationToken cancellationToken = default) =>
        Task.FromResult(_configs.FirstOrDefault(c =>
            c.TenantId == tenantId
            && c.Category == category
            && c.IsDefault
            && c.IsEnabled));

    public Task<IReadOnlyList<TenantProviderConfig>> ListByTenantAsync(Guid tenantId, ProviderCategory? category, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<TenantProviderConfig> result = _configs
            .Where(c => c.TenantId == tenantId && (category is null || c.Category == category))
            .ToList();
        return Task.FromResult(result);
    }

    public Task AddAsync(TenantProviderConfig config, CancellationToken cancellationToken = default)
    {
        _configs.Add(config);
        return Task.CompletedTask;
    }

    public void Update(TenantProviderConfig config) { }

    public void Remove(TenantProviderConfig config) => _configs.Remove(config);
}
