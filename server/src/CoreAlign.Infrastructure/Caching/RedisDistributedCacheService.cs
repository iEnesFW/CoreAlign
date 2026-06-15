using CoreAlign.Application.Common.Caching;

namespace CoreAlign.Infrastructure.Caching;

public sealed class RedisDistributedCacheService : IDistributedCacheService
{
    private const string PackageMissingMessage =
        "StackExchange.Redis is not referenced by CoreAlign.Infrastructure. " +
        "Set Redis:Enabled=false (default) to fall back to InMemoryDistributedCacheService, " +
        "or add the package per docs/sprint9-blockers.md.";

    public string BuildKey(string region, Guid tenantId, string suffix)
        => throw new NotSupportedException(PackageMissingMessage);

    public TimeSpan ResolveTtl(string region, TimeSpan? requestedTtl)
        => throw new NotSupportedException(PackageMissingMessage);

    public Task<T?> GetAsync<T>(string region, string key, CancellationToken cancellationToken = default)
        => throw new NotSupportedException(PackageMissingMessage);

    public Task SetAsync<T>(string region, string key, T value, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
        => throw new NotSupportedException(PackageMissingMessage);

    public Task<T> GetOrAddAsync<T>(
        string region,
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException(PackageMissingMessage);

    public Task RemoveAsync(string region, string key, CancellationToken cancellationToken = default)
        => throw new NotSupportedException(PackageMissingMessage);

    public Task RemoveByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException(PackageMissingMessage);

    public Task RemoveByRegionTenantAsync(string region, Guid tenantId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException(PackageMissingMessage);

    public Task RemoveByPrefixAsync(string region, Guid tenantId, string prefix, CancellationToken cancellationToken = default)
        => throw new NotSupportedException(PackageMissingMessage);
}
