using CoreAlign.Domain.Entities.Notifications;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Notifications.Repositories;

public interface INotificationMessageRepository
{
    Task<NotificationMessage?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken ct);
    Task<IReadOnlyList<NotificationMessage>> ListAsync(Guid tenantId, NotificationStatus? status, string? categoryKey, NotificationChannel? channel, int skip, int take, CancellationToken ct);
    Task<IReadOnlyList<NotificationMessage>> ListForUserAsync(Guid tenantId, Guid userId, bool unreadOnly, int skip, int take, CancellationToken ct);
    Task<int> CountUnreadAsync(Guid tenantId, Guid userId, CancellationToken ct);
    Task<NotificationMessage?> GetByProviderMessageIdAsync(Guid tenantId, string providerName, string providerMessageId, CancellationToken ct);
    Task<NotificationMessage?> GetByHashAsync(Guid tenantId, string idempotencyHash, CancellationToken ct);
    Task AddAsync(NotificationMessage entity, CancellationToken ct);
    Task UpsertAsync(NotificationMessage entity, CancellationToken ct);
}
