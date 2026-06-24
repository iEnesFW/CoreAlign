using Microsoft.Extensions.Hosting;

namespace CoreAlign.API.HostedServices;

public sealed class PayrollSystemDataSeeder : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PayrollSystemDataSeeder> _logger;

    public PayrollSystemDataSeeder(IServiceScopeFactory scopeFactory, ILogger<PayrollSystemDataSeeder> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            await PayrollParametersSeed.SeedGlobalAsync(scope.ServiceProvider, stoppingToken);
            _logger.LogInformation("Payroll system parameters seed checked.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Payroll system parameters seeding failed; payroll runs will not resolve until parameters exist.");
        }
    }
}
