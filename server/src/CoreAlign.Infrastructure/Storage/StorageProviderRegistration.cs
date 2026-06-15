using CoreAlign.Application.Common.Storage;
using CoreAlign.Infrastructure.Options;
using CoreAlign.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Infrastructure.Storage;

public static class StorageProviderRegistration
{
    public static IServiceCollection AddStorageProvider(IServiceCollection services, IConfiguration configuration)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        if (configuration is null) throw new ArgumentNullException(nameof(configuration));

        services.AddOptions<StorageProviderOptions>()
            .Bind(configuration.GetSection(StorageProviderOptions.SectionName));

        services.AddOptions<FileStorageOptions>()
            .Bind(configuration.GetSection(FileStorageOptions.SectionName));

        services.TryAddScoped<LocalFileSystemStorage>();
        services.TryAddScoped<S3FileStorage>();
        services.TryAddScoped<AzureBlobFileStorage>();

        services.RemoveAll<IFileStorage>();

        var provider = configuration[$"{StorageProviderOptions.SectionName}:Provider"] ?? StorageProviderNames.Local;

        services.AddScoped<IFileStorage>(sp =>
        {
            IFileStorage inner = provider switch
            {
                var p when string.Equals(p, StorageProviderNames.S3, StringComparison.OrdinalIgnoreCase)
                    => sp.GetRequiredService<S3FileStorage>(),
                var p when string.Equals(p, StorageProviderNames.AzureBlob, StringComparison.OrdinalIgnoreCase)
                    => sp.GetRequiredService<AzureBlobFileStorage>(),
                _ => sp.GetRequiredService<LocalFileSystemStorage>(),
            };

            var scanner = sp.GetService<IVirusScanner>();
            if (scanner is null) return inner;
            var logger = sp.GetRequiredService<ILogger<VirusScanFileStorage>>();
            return new VirusScanFileStorage(inner, scanner, logger);
        });

        return services;
    }

    public static string ResolveProviderName(IConfiguration configuration)
        => configuration[$"{StorageProviderOptions.SectionName}:Provider"] ?? StorageProviderNames.Local;
}
