using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Infrastructure.Fx;

[Obsolete("F3.3 TcmbFxSyncJob is retired; Phase 40 TcmbFxIngestJob (Hangfire Cron.Daily(8) UTC) owns the canonical TCMB pipeline. Type retained for binary compatibility — not registered as a HostedService.")]
public sealed class TcmbFxSyncJob : BackgroundService
{
    private static readonly TimeSpan TurkeyOffset = TimeSpan.FromHours(3);
    private static readonly TimeOnly DailyRunTime = new(10, 30);

    private readonly ILogger<TcmbFxSyncJob> _logger;

    public TcmbFxSyncJob(IServiceProvider serviceProvider, ILogger<TcmbFxSyncJob> logger)
    {
        _ = serviceProvider;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "TcmbFxSyncJob (F3.3) is deprecated and no longer scheduled. Phase 40 TcmbFxIngestJob owns the canonical TCMB pipeline.");
        return Task.CompletedTask;
    }

    public static TimeSpan ComputeDelayUntilNextRun(DateTime utcNow)
    {
        var trNow = utcNow + TurkeyOffset;
        var todayRunTr = trNow.Date.Add(DailyRunTime.ToTimeSpan());
        var nextTr = trNow < todayRunTr ? todayRunTr : todayRunTr.AddDays(1);
        var nextUtc = nextTr - TurkeyOffset;
        var delay = nextUtc - utcNow;
        return delay < TimeSpan.Zero ? TimeSpan.Zero : delay;
    }
}
