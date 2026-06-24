using CoreAlign.Application.Notifications.Delivery;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Application.Jobs;

public sealed class RateCounterCleanupJob
{
    private static readonly TimeSpan Retention = TimeSpan.FromHours(2);

    private readonly INotificationRateLimiter _rateLimiter;
    private readonly ILogger<RateCounterCleanupJob> _logger;

    public RateCounterCleanupJob(INotificationRateLimiter rateLimiter, ILogger<RateCounterCleanupJob> logger)
    {
        _rateLimiter = rateLimiter;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var removed = await _rateLimiter.CleanupAsync(DateTime.UtcNow.Subtract(Retention), cancellationToken);
        if (removed > 0)
        {
            _logger.LogDebug("Notification rate-counter cleanup removed {Count} expired windows.", removed);
        }
    }
}
