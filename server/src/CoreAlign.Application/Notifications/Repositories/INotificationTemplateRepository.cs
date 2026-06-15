using CoreAlign.Domain.Entities.Notifications;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Notifications.Repositories;

public interface INotificationTemplateRepository
{
    Task<NotificationTemplate?> GetByKeyLocaleAsync(Guid? tenantId, string key, NotificationChannel channel, string locale, CancellationToken ct);
    Task<IReadOnlyList<NotificationTemplate>> ListAsync(Guid? tenantId, CancellationToken ct);
    Task AddAsync(NotificationTemplate entity, CancellationToken ct);
    Task<bool> ExistsAsync(Guid? tenantId, string key, NotificationChannel channel, string locale, CancellationToken ct);
}
