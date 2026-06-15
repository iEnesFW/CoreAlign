using CoreAlign.Domain.Entities;

namespace CoreAlign.Domain.Interfaces;

public interface ICommentRepository
{
    Task<Comment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Comment>> ListByEntityAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default);

    Task AddAsync(Comment comment, CancellationToken cancellationToken = default);
    void Update(Comment comment);
    void Remove(Comment comment);
}

public interface INotificationRepository
{
    Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Notification>> ListByRecipientAsync(Guid recipientUserId, bool unreadOnly, int take, CancellationToken cancellationToken = default);

    Task<int> CountUnreadAsync(Guid recipientUserId, CancellationToken cancellationToken = default);

    Task<bool> ExistsForRecipientAsync(
        Guid recipientUserId,
        string entityType,
        Guid entityId,
        string notificationType,
        CancellationToken cancellationToken = default);

    Task AddAsync(Notification notification, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<Notification> notifications, CancellationToken cancellationToken = default);
    Task AddIfNotExistsAsync(Notification notification, CancellationToken cancellationToken = default);
    void Update(Notification notification);

    Task<int> MarkAllReadAsync(Guid recipientUserId, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Notification> Items, int Total)> SearchByTenantAsync(
        Guid tenantId,
        string? type,
        DateTime? fromUtc,
        DateTime? toUtc,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
