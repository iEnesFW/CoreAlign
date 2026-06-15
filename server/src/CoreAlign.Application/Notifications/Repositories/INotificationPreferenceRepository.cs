using CoreAlign.Domain.Entities.Notifications;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Notifications.Repositories;

public interface INotificationPreferenceRepository
{
    Task<NotificationPreference?> GetAsync(Guid tenantId, Guid userId, string categoryKey, NotificationChannel channel, CancellationToken ct);
    Task<IReadOnlyList<NotificationPreference>> ListForUserAsync(Guid tenantId, Guid userId, CancellationToken ct);
    Task AddAsync(NotificationPreference entity, CancellationToken ct);
}
