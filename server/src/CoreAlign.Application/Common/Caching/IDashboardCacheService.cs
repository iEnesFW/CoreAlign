namespace CoreAlign.Application.Common.Caching;

public interface IDashboardCacheService
{
    Task<T> GetOrAddAsync<T>(string cacheKey, Func<CancellationToken, Task<T>> factory, TimeSpan? ttl = null, CancellationToken cancellationToken = default);
    void InvalidateTenant(Guid tenantId);
    string BuildKey(Guid tenantId, string suffix);
}
