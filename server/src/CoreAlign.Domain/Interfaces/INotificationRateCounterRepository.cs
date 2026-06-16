using CoreAlign.Domain.Entities.Notifications;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Interfaces;

public interface INotificationRateCounterRepository
{
    Task<NotificationRateCounter?> GetAsync(Guid tenantId, string providerName, RateScope scope, string scopeKey, DateTime windowStartUtc, CancellationToken cancellationToken = default);
    Task AddAsync(NotificationRateCounter counter, CancellationToken cancellationToken = default);
    Task<int> DeleteOlderThanAsync(DateTime thresholdUtc, CancellationToken cancellationToken = default);
}
