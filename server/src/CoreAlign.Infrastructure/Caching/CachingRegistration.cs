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
            services.AddSingleton<IDistributedCacheService, RedisDistributedCacheService>();
        }
        else
        {
            services.AddSingleton<IDistributedCacheService, InMemoryDistributedCacheService>();
        }

        services.RemoveAll<IDashboardCacheService>();
        services.RemoveAll<ILookupCacheService>();

        services.AddSingleton<IDashboardCacheService, DistributedDashboardCacheService>();
        services.AddScoped<ILookupCacheService, DistributedLookupCacheService>();

        return services;
    }
}
