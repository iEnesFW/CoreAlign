using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;

namespace CoreAlign.API.Configuration;

public static class VaultConfigurationExtensions
{
    public const string VaultProviderKey = "Configuration:VaultProvider";
    public const string AzureKeyVaultUriKey = "Configuration:AzureKeyVaultUri";
    public const string AwsSsmPathKey = "Configuration:AwsSsmPath";
    public const string AwsRegionKey = "Configuration:AwsRegion";

    public static IConfigurationBuilder AddVaultConfiguration(
        this IConfigurationBuilder builder,
        IConfiguration baseConfig,
        ILogger? logger = null)
    {
        var provider = baseConfig[VaultProviderKey];

        if (string.IsNullOrWhiteSpace(provider)
            || string.Equals(provider, "None", StringComparison.OrdinalIgnoreCase))
        {
            logger?.LogInformation("Vault provider: None — using default configuration sources");
            return builder;
        }

        if (string.Equals(provider, "AzureKeyVault", StringComparison.OrdinalIgnoreCase))
        {
            var uri = baseConfig[AzureKeyVaultUriKey];
            if (string.IsNullOrWhiteSpace(uri))
            {
                logger?.LogWarning(
                    "Vault provider AzureKeyVault selected but {Key} is empty; skipping vault wiring",
                    AzureKeyVaultUriKey);
                return builder;
            }

            builder.AddAzureKeyVault(new Uri(uri), new DefaultAzureCredential(), new AzureKeyVaultConfigurationOptions
            {
                ReloadInterval = TimeSpan.FromMinutes(15),
            });
            logger?.LogInformation("Vault provider: AzureKeyVault wired to {Uri}", uri);
            return builder;
        }

        if (string.Equals(provider, "AwsSsm", StringComparison.OrdinalIgnoreCase))
        {
            var path = baseConfig[AwsSsmPathKey];
            if (string.IsNullOrWhiteSpace(path))
            {
                logger?.LogWarning(
                    "Vault provider AwsSsm selected but {Key} is empty; skipping vault wiring",
                    AwsSsmPathKey);
                return builder;
            }

            var region = baseConfig[AwsRegionKey];
            builder.AddSystemsManager(configureSource =>
            {
                configureSource.Path = path;
                configureSource.ReloadAfter = TimeSpan.FromMinutes(15);
                if (!string.IsNullOrWhiteSpace(region))
                {
                    configureSource.AwsOptions = new Amazon.Extensions.NETCore.Setup.AWSOptions
                    {
                        Region = Amazon.RegionEndpoint.GetBySystemName(region),
                    };
                }
            });
            logger?.LogInformation("Vault provider: AwsSsm wired to {Path}", path);
            return builder;
        }

        logger?.LogWarning(
            "Vault provider '{Provider}' is not recognized; using default configuration sources",
            provider);
        return builder;
    }
}
