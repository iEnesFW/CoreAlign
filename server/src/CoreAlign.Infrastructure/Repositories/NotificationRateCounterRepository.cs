using CoreAlign.Domain.Entities.Notifications;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class NotificationRateCounterRepository : INotificationRateCounterRepository
{
    private readonly CoreAlignDbContext _context;
    public NotificationRateCounterRepository(CoreAlignDbContext context) => _context = context;

    public Task<NotificationRateCounter?> GetAsync(Guid tenantId, string providerName, RateScope scope, string scopeKey, DateTime windowStartUtc, CancellationToken cancellationToken = default) =>
        _context.NotificationRateCounters
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c =>
                c.TenantId == tenantId
                && c.ProviderName == providerName
                && c.Scope == scope
                && c.ScopeKey == scopeKey
                && c.WindowStartUtc == windowStartUtc,
                cancellationToken);

    public async Task AddAsync(NotificationRateCounter counter, CancellationToken cancellationToken = default) =>
        await _context.NotificationRateCounters.AddAsync(counter, cancellationToken);

    public Task<int> DeleteOlderThanAsync(DateTime thresholdUtc, CancellationToken cancellationToken = default) =>
        _context.NotificationRateCounters
            .IgnoreQueryFilters()
            .Where(c => c.WindowStartUtc < thresholdUtc)
            .ExecuteDeleteAsync(cancellationToken);
}
