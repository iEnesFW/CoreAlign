using CoreAlign.Application.Providers;

namespace CoreAlign.Integration.Tests.Providers.TestFixtures;

/// <summary>In-memory <see cref="IProviderRegistry{TProvider}"/> wired with a single provider for integration tests.</summary>
public sealed class InMemoryProviderRegistry<TProvider> : IProviderRegistry<TProvider>
    where TProvider : IExternalProvider
{
    private readonly Dictionary<string, TProvider> _providers;

    public InMemoryProviderRegistry(params TProvider[] providers)
    {
        _providers = providers.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
    }

    public TProvider? Find(string name) =>
        _providers.TryGetValue(name, out var provider) ? provider : default;

    public TProvider Require(string name) =>
        Find(name) ?? throw new InvalidOperationException($"Provider '{name}' is not registered.");

    public IReadOnlyList<string> Names => _providers.Keys.ToList();

    public IReadOnlyList<TProvider> All => _providers.Values.ToList();

    public Task<TProvider> ResolveForTenantAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_providers.Values.First());

    public Task<TProvider?> TryResolveForTenantAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
        Task.FromResult<TProvider?>(_providers.Values.FirstOrDefault());
}
