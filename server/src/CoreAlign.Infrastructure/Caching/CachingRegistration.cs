using CoreAlign.Application.Common.Caching;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CoreAlign.Infrastructure.Caching;

public static class CachingRegistration
{
    public static IServiceCollection AddDistributedCaching(IServiceCollection services, IConfiguration configuration)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        if (configuration is null) throw new ArgumentNullException(nameof(configuration));

        services.AddOptions<RedisOptions>()
            .Bind(configuration.GetSection(RedisOptions.SectionName));

        services.AddOptions<CacheRegionOptions>()
            .Bind(configuration.GetSection(CacheRegionOptions.SectionName));

        var redisEnabled = configuration.GetValue<bool?>($"{RedisOptions.SectionName}:Enabled") ?? false;
        if (redisEnabled)
        {
            // RedisDistributedCacheService is a package-missing stub that throws on every call
            // (the Redis client is not included in this build). Fail fast at startup rather than
            // silently falling back to an in-memory cache (which would be incoherent across
            // instances) or throwing on the first cache access.
            throw new InvalidOperationException(
                $"Distributed cache provider Redis is enabled ({RedisOptions.SectionName}:Enabled=true) " +
                "but is not implemented in this build. Disable it (the in-memory cache is used by default) " +
                "or complete the Redis adapter before enabling it.");
        }

        services.AddSingleton<IDistributedCacheService, InMemoryDistributedCacheService>();

        services.RemoveAll<IDashboardCacheService>();
        services.RemoveAll<ILookupCacheService>();

        services.AddSingleton<IDashboardCacheService, DistributedDashboardCacheService>();
        services.AddScoped<ILookupCacheService, DistributedLookupCacheService>();

        return services;
    }
}
