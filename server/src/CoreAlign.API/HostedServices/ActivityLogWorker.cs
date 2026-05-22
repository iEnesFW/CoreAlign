using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.API.HostedServices;

/// <summary>
/// Drains <see cref="IActivityLogChannel"/> off-thread and batch-inserts rows
/// into the database. Each batch runs in its own scope so the DbContext stays
/// short-lived; this keeps the request pipeline free of audit-log DB cost.
/// </summary>
public class ActivityLogWorker : BackgroundService
{
    private const int BatchSize = 64;
    private static readonly TimeSpan BatchFlushDelay = TimeSpan.FromSeconds(2);

    private readonly IActivityLogChannel _channel;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ActivityLogWorker> _logger;

    public ActivityLogWorker(
        IActivityLogChannel channel,
        IServiceScopeFactory scopeFactory,
        ILogger<ActivityLogWorker> logger)
    {
        _channel = channel;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var batch = new List<ActivityLog>(BatchSize);

        try
        {
            await foreach (var log in _channel.ReadAllAsync(stoppingToken))
            {
                batch.Add(log);
                if (batch.Count >= BatchSize)
                {
                    await FlushAsync(batch, stoppingToken);
                    batch.Clear();
                }
                else
                {
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                    cts.CancelAfter(BatchFlushDelay);
                    try
                    {
                        await Task.Delay(BatchFlushDelay, cts.Token);
                    }
                    catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
                    {
                        // Timer fired — flush partial batch.
                    }

                    if (batch.Count > 0)
                    {
                        await FlushAsync(batch, stoppingToken);
                        batch.Clear();
                    }
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // graceful shutdown
        }

        if (batch.Count > 0)
        {
            await FlushAsync(batch, CancellationToken.None);
        }
    }

    private async Task FlushAsync(List<ActivityLog> batch, CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var repo = scope.ServiceProvider.GetRequiredService<IActivityLogRepository>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            foreach (var log in batch)
            {
                await repo.AddAsync(log, cancellationToken);
            }
            await uow.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ActivityLog batch flush failed (batchSize={Count}).", batch.Count);
        }
    }
}
