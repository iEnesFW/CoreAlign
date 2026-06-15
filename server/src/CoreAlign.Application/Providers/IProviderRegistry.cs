namespace CoreAlign.Application.Providers;

public interface IProviderRegistry<TProvider> where TProvider : IExternalProvider
{
    TProvider? Find(string name);
    TProvider Require(string name);
    IReadOnlyList<string> Names { get; }
    IReadOnlyList<TProvider> All { get; }
    Task<TProvider> ResolveForTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<TProvider?> TryResolveForTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
