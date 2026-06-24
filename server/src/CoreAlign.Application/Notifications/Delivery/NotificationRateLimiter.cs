using CoreAlign.Domain.Entities.Notifications;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace CoreAlign.Application.Notifications.Delivery;

public sealed class NotificationRateLimiter : INotificationRateLimiter
{
    private const string CrossProviderKey = "*";

    private readonly INotificationRateCounterRepository _repository;
    private readonly NotificationDeliveryOptions _options;

    public NotificationRateLimiter(
        INotificationRateCounterRepository repository,
        IOptions<NotificationDeliveryOptions> options)
    {
        _repository = repository;
        _options = options.Value;
    }

    public async Task<RateDecision> TryAcquireAsync(Guid tenantId, string providerName, string recipient, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var windowStart = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, DateTimeKind.Utc);
        var windowEnd = windowStart.AddMinutes(1);
        var recipientKey = (recipient ?? string.Empty).Trim().ToLowerInvariant();

        var limits = new (RateScope Scope, string Provider, string Key, int Limit, string Reason)[]
        {
            (RateScope.Tenant, CrossProviderKey, string.Empty, _options.PerTenantPerMinute, "Tenant send rate limit reached"),
            (RateScope.Provider, providerName, string.Empty, _options.PerProviderPerMinute, "Provider send rate limit reached"),
            (RateScope.Recipient, CrossProviderKey, recipientKey, _options.PerRecipientPerMinute, "Recipient send rate limit reached"),
        };

        var acquired = new List<NotificationRateCounter>(limits.Length);
        foreach (var limit in limits)
        {
            var counter = await _repository.GetAsync(tenantId, limit.Provider, limit.Scope, limit.Key, windowStart, cancellationToken).ConfigureAwait(false);
            if (counter is null)
            {
                counter = new NotificationRateCounter(tenantId, limit.Provider, limit.Scope, limit.Key, windowStart);
                await _repository.AddAsync(counter, cancellationToken).ConfigureAwait(false);
            }

            if (counter.Count >= limit.Limit)
            {
                return new RateDecision(false, windowEnd, limit.Reason);
            }

            acquired.Add(counter);
        }

        foreach (var counter in acquired)
        {
            counter.Increment();
        }

        return new RateDecision(true, windowEnd, null);
    }

    public Task<int> CleanupAsync(DateTime olderThanUtc, CancellationToken cancellationToken = default) =>
        _repository.DeleteOlderThanAsync(olderThanUtc, cancellationToken);
}
