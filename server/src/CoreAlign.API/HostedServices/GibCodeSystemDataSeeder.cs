namespace CoreAlign.API.HostedServices;

public sealed class GibCodeSystemDataSeeder : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<GibCodeSystemDataSeeder> _logger;

    public GibCodeSystemDataSeeder(IServiceScopeFactory scopeFactory, ILogger<GibCodeSystemDataSeeder> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            await GibCodeSeed.SeedGlobalAsync(scope.ServiceProvider, stoppingToken);
            _logger.LogInformation("GİB withholding/exemption code seed checked.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GİB code seeding failed; lookups may be empty until next startup.");
        }
    }
}
