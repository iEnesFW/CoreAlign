namespace CoreAlign.Application.Notifications.Delivery;

public sealed record RateDecision(bool Allowed, DateTime? WindowEndUtc, string? Reason);

public interface INotificationRateLimiter
{
    Task<RateDecision> TryAcquireAsync(Guid tenantId, string providerName, string recipient, CancellationToken cancellationToken = default);
    Task<int> CleanupAsync(DateTime olderThanUtc, CancellationToken cancellationToken = default);
}
