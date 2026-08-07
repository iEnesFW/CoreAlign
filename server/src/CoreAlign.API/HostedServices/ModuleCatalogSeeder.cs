using Microsoft.Extensions.Hosting;

namespace CoreAlign.API.HostedServices;

/// <summary>
/// Seeds the purchasable module catalog on every boot. It is deliberately NOT part of
/// <see cref="DemoDataSeeder"/>: that one is hard-off in Production, which would leave the module
/// store showing a legitimate-looking empty state that nobody could buy from.
/// </summary>
public sealed class ModuleCatalogSeeder : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ModuleCatalogSeeder> _logger;

    public ModuleCatalogSeeder(IServiceScopeFactory scopeFactory, ILogger<ModuleCatalogSeeder> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            await ModuleCatalogSeed.SeedAsync(scope.ServiceProvider, stoppingToken);
            _logger.LogInformation("Module catalog seed checked.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Module catalog seeding failed; the module store will be empty until it succeeds.");
        }
    }
}
