using CoreAlign.Application.Common.Storage;
using CoreAlign.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CoreAlign.Infrastructure.Storage;

public static class StorageRegistration
{
    private const string ProviderKey = "VirusScan:Provider";
    private const string ClamAvProvider = "ClamAv";

    public static IServiceCollection AddVirusScanningStorage(IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration[ProviderKey] ?? NoOpVirusScanner.ProviderName;
        if (string.Equals(provider, ClamAvProvider, StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IVirusScanner, ClamAvVirusScanner>();
        }
        else
        {
            services.AddSingleton<IVirusScanner, NoOpVirusScanner>();
        }

        services.TryAddScoped<LocalFileSystemStorage>();
        return services;
    }
}
