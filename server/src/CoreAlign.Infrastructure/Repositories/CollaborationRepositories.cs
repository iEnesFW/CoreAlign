using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CoreAlign.Infrastructure.Repositories;

public class CommentRepository : ICommentRepository
{
    private readonly CoreAlignDbContext _context;

    public CommentRepository(CoreAlignDbContext context)
    {
        _context = context;
    }

    public Task<Comment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.Comments.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Comment>> ListByEntityAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default)
    {
        return await _context.Comments
            .AsNoTracking()
            .Where(c => c.EntityType == entityType && c.EntityId == entityId)
            .OrderBy(c => c.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Comment comment, CancellationToken cancellationToken = default)
    {
        await _context.Comments.AddAsync(comment, cancellationToken);
    }

    public void Update(Comment comment) => _context.Comments.Update(comment);

    public void Remove(Comment comment) => _context.Comments.Remove(comment);
}

public class NotificationRepository : INotificationRepository
{
    private readonly CoreAlignDbContext _context;

    public NotificationRepository(CoreAlignDbContext context)
    {
        _context = context;
    }

    public Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.Notifications.FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Notification>> ListByRecipientAsync(
        Guid recipientUserId,
        bool unreadOnly,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Notifications
            .AsNoTracking()
            .Where(n => n.RecipientUserId == recipientUserId);

        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        return await query
            .OrderByDescending(n => n.CreatedAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountUnreadAsync(Guid recipientUserId, CancellationToken cancellationToken = default)
        => _context.Notifications.CountAsync(n => n.RecipientUserId == recipientUserId && !n.IsRead, cancellationToken);

    public Task<bool> ExistsForRecipientAsync(
        Guid recipientUserId,
        string entityType,
        Guid entityId,
        string notificationType,
        CancellationToken cancellationToken = default) =>
        _context.Notifications
            .AsNoTracking()
            .AnyAsync(n => n.RecipientUserId == recipientUserId
                        && n.EntityType == entityType
                        && n.EntityId == entityId
                        && n.Type == notificationType, cancellationToken);

    public async Task AddAsync(Notification notification, CancellationToken cancellationToken = default)
        => await _context.Notifications.AddAsync(notification, cancellationToken);

    public async Task AddRangeAsync(IEnumerable<Notification> notifications, CancellationToken cancellationToken = default)
        => await _context.Notifications.AddRangeAsync(notifications, cancellationToken);

    public async Task AddIfNotExistsAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        if (notification is null) throw new ArgumentNullException(nameof(notification));

        var exists = await ExistsForRecipientAsync(
            notification.RecipientUserId,
            notification.EntityType,
            notification.EntityId,
            notification.Type,
            cancellationToken);
        if (exists) return;

        await _context.Notifications.AddAsync(notification, cancellationToken);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            _context.Entry(notification).State = EntityState.Detached;
        }
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
        => ex.InnerException is PostgresException pg && pg.SqlState == "23505";

    public void Update(Notification notification) => _context.Notifications.Update(notification);

    public async Task<int> MarkAllReadAsync(Guid recipientUserId, CancellationToken cancellationToken = default)
    {
        var unread = await _context.Notifications
            .Where(n => n.RecipientUserId == recipientUserId && !n.IsRead)
            .ToListAsync(cancellationToken);
        foreach (var n in unread)
        {
            n.MarkRead();
        }
        return unread.Count;
    }

    public async Task<(IReadOnlyList<Notification> Items, int Total)> SearchByTenantAsync(
        Guid tenantId,
        string? type,
        DateTime? fromUtc,
        DateTime? toUtc,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Notifications
            .AsNoTracking()
            .Where(n => n.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(type))
        {
            var trimmed = type.Trim();
            query = query.Where(n => n.Type == trimmed);
        }
        if (fromUtc.HasValue)
        {
            query = query.Where(n => n.CreatedAtUtc >= fromUtc.Value);
        }
        if (toUtc.HasValue)
        {
            query = query.Where(n => n.CreatedAtUtc <= toUtc.Value);
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(n => n.CreatedAtUtc)
            .ThenBy(n => n.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}
