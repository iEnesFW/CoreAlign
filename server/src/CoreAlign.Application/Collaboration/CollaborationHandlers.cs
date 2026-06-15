using CoreAlign.Application.Common;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Collaboration;

/// <summary>Maps domain entities to transport DTOs, resolving the author display name from a user lookup.</summary>
internal static class CollaborationMapper
{
    public static string DisplayNameFor(User? user)
    {
        if (user is null) return string.Empty;
        var first = user.FirstName?.Trim();
        var last = user.LastName?.Trim();
        if (!string.IsNullOrEmpty(first) || !string.IsNullOrEmpty(last))
        {
            return string.Join(' ', new[] { first, last }.Where(s => !string.IsNullOrEmpty(s)));
        }
        return string.IsNullOrWhiteSpace(user.Username) ? user.Email : user.Username;
    }

    public static CommentDto ToDto(Comment c, IReadOnlyDictionary<Guid, User> users)
    {
        users.TryGetValue(c.AuthorUserId, out var author);
        return new CommentDto(
            c.Id,
            c.EntityType,
            c.EntityId,
            c.AuthorUserId,
            DisplayNameFor(author),
            c.Body,
            c.ParentCommentId,
            c.CreatedAtUtc,
            c.EditedAtUtc);
    }

    public static NotificationDto ToDto(Notification n, IReadOnlyDictionary<Guid, User> users)
    {
        User? actor = null;
        if (n.ActorUserId.HasValue) users.TryGetValue(n.ActorUserId.Value, out actor);
        return new NotificationDto(
            n.Id,
            n.Type,
            n.EntityType,
            n.EntityId,
            n.Title,
            n.Body,
            n.ActorUserId,
            actor is null ? null : DisplayNameFor(actor),
            n.IsRead,
            n.CreatedAtUtc);
    }
}

public class ListCommentsHandler : IRequestHandler<ListCommentsQuery, IReadOnlyList<CommentDto>>
{
    private readonly ICommentRepository _comments;
    private readonly IUserRepository _users;
    private readonly ITenantContext _tenant;

    public ListCommentsHandler(ICommentRepository comments, IUserRepository users, ITenantContext tenant)
    {
        _comments = comments;
        _users = users;
        _tenant = tenant;
    }

    public async Task<IReadOnlyList<CommentDto>> Handle(ListCommentsQuery request, CancellationToken cancellationToken)
    {
        var rows = await _comments.ListByEntityAsync(request.EntityType, request.EntityId, cancellationToken);
        if (rows.Count == 0) return Array.Empty<CommentDto>();

        var tenantId = _tenant.RequireTenantId();
        var tenantUsers = await _users.ListByTenantAsync(tenantId, cancellationToken);
        var lookup = tenantUsers.ToDictionary(u => u.Id);

        return rows.Select(c => CollaborationMapper.ToDto(c, lookup)).ToList();
    }
}

public class CreateCommentHandler : IRequestHandler<CreateCommentCommand, CommentDto>
{
    private static readonly HashSet<string> AllowedEntityTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Order", "VendorBill", "Shipment",
    };

    private readonly ICommentRepository _comments;
    private readonly IUserRepository _users;
    private readonly ICommentPostedOutbox _outbox;
    private readonly IUnitOfWork _uow;

    public CreateCommentHandler(
        ICommentRepository comments,
        IUserRepository users,
        ICommentPostedOutbox outbox,
        IUnitOfWork uow)
    {
        _comments = comments;
        _users = users;
        _outbox = outbox;
        _uow = uow;
    }

    public async Task<CommentDto> Handle(CreateCommentCommand request, CancellationToken cancellationToken)
    {
        if (!AllowedEntityTypes.Contains(request.EntityType))
        {
            throw new ArgumentException($"Unsupported EntityType '{request.EntityType}'.", nameof(request.EntityType));
        }
        if (request.AuthorUserId == Guid.Empty) throw new ArgumentException("AuthorUserId is required.", nameof(request.AuthorUserId));
        if (string.IsNullOrWhiteSpace(request.Body)) throw new ArgumentException("Body is required.", nameof(request.Body));

        // Reject second-level threading: replies must point to a top-level comment in the same entity scope.
        if (request.ParentCommentId is { } parentId)
        {
            var parent = await _comments.GetByIdAsync(parentId, cancellationToken)
                ?? throw new ArgumentException("Parent comment not found.", nameof(request.ParentCommentId));
            if (parent.ParentCommentId is not null)
            {
                throw new ArgumentException("Replies cannot be nested deeper than one level.", nameof(request.ParentCommentId));
            }
            if (!string.Equals(parent.EntityType, request.EntityType, StringComparison.OrdinalIgnoreCase) || parent.EntityId != request.EntityId)
            {
                throw new ArgumentException("Parent comment belongs to a different entity.", nameof(request.ParentCommentId));
            }
        }

        var canonicalType = AllowedEntityTypes.First(t => string.Equals(t, request.EntityType, StringComparison.OrdinalIgnoreCase));
        var comment = new Comment(canonicalType, request.EntityId, request.AuthorUserId, request.Body, request.ParentCommentId);
        await _comments.AddAsync(comment, cancellationToken);

        await _outbox.EnqueueAsync(new CommentPostedPayload(
            comment.Id,
            comment.EntityType,
            comment.EntityId,
            comment.AuthorUserId,
            comment.Body,
            comment.ParentCommentId), cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);

        var author = await _users.GetByIdAsync(comment.AuthorUserId, cancellationToken);
        var lookup = author is null ? new Dictionary<Guid, User>() : new Dictionary<Guid, User> { [author.Id] = author };
        return CollaborationMapper.ToDto(comment, lookup);
    }
}

public class EditCommentHandler : IRequestHandler<EditCommentCommand, CommentDto>
{
    private readonly ICommentRepository _comments;
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _uow;

    public EditCommentHandler(ICommentRepository comments, IUserRepository users, IUnitOfWork uow)
    {
        _comments = comments;
        _users = users;
        _uow = uow;
    }

    public async Task<CommentDto> Handle(EditCommentCommand request, CancellationToken cancellationToken)
    {
        var comment = await _comments.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new CommentNotFoundException();

        if (comment.AuthorUserId != request.CurrentUserId)
        {
            throw new CommentEditForbiddenException();
        }

        comment.Edit(request.Body);
        _comments.Update(comment);
        await _uow.SaveChangesAsync(cancellationToken);

        var author = await _users.GetByIdAsync(comment.AuthorUserId, cancellationToken);
        var lookup = author is null ? new Dictionary<Guid, User>() : new Dictionary<Guid, User> { [author.Id] = author };
        return CollaborationMapper.ToDto(comment, lookup);
    }
}

public class DeleteCommentHandler : IRequestHandler<DeleteCommentCommand, bool>
{
    private readonly ICommentRepository _comments;
    private readonly IUnitOfWork _uow;

    public DeleteCommentHandler(ICommentRepository comments, IUnitOfWork uow)
    {
        _comments = comments;
        _uow = uow;
    }

    public async Task<bool> Handle(DeleteCommentCommand request, CancellationToken cancellationToken)
    {
        var comment = await _comments.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new CommentNotFoundException();

        if (comment.AuthorUserId != request.CurrentUserId && !request.IsAdmin)
        {
            throw new CommentDeleteForbiddenException();
        }

        _comments.Remove(comment);
        await _uow.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public class ListNotificationsHandler : IRequestHandler<ListNotificationsQuery, IReadOnlyList<NotificationDto>>
{
    private readonly INotificationRepository _notifications;
    private readonly IUserRepository _users;
    private readonly ITenantContext _tenant;

    public ListNotificationsHandler(INotificationRepository notifications, IUserRepository users, ITenantContext tenant)
    {
        _notifications = notifications;
        _users = users;
        _tenant = tenant;
    }

    public async Task<IReadOnlyList<NotificationDto>> Handle(ListNotificationsQuery request, CancellationToken cancellationToken)
    {
        if (request.CurrentUserId == Guid.Empty) throw new ArgumentException("CurrentUserId is required.", nameof(request.CurrentUserId));

        var take = Math.Clamp(request.Take, 1, 200);
        var rows = await _notifications.ListByRecipientAsync(request.CurrentUserId, request.UnreadOnly, take, cancellationToken);
        if (rows.Count == 0) return Array.Empty<NotificationDto>();

        var tenantId = _tenant.RequireTenantId();
        var tenantUsers = await _users.ListByTenantAsync(tenantId, cancellationToken);
        var lookup = tenantUsers.ToDictionary(u => u.Id);

        return rows.Select(n => CollaborationMapper.ToDto(n, lookup)).ToList();
    }
}

public class UnreadNotificationCountHandler : IRequestHandler<UnreadNotificationCountQuery, int>
{
    private readonly INotificationRepository _notifications;

    public UnreadNotificationCountHandler(INotificationRepository notifications)
    {
        _notifications = notifications;
    }

    public Task<int> Handle(UnreadNotificationCountQuery request, CancellationToken cancellationToken)
    {
        if (request.CurrentUserId == Guid.Empty) throw new ArgumentException("CurrentUserId is required.", nameof(request.CurrentUserId));
        return _notifications.CountUnreadAsync(request.CurrentUserId, cancellationToken);
    }
}

public class MarkNotificationReadHandler : IRequestHandler<MarkNotificationReadCommand, bool>
{
    private readonly INotificationRepository _notifications;
    private readonly ITenantContext _tenant;
    private readonly IUnitOfWork _uow;

    public MarkNotificationReadHandler(INotificationRepository notifications, ITenantContext tenant, IUnitOfWork uow)
    {
        _notifications = notifications;
        _tenant = tenant;
        _uow = uow;
    }

    public async Task<bool> Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        var notification = await _notifications.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotificationNotFoundException();

        _tenant.EnsureSameTenant(notification.TenantId);
        if (notification.RecipientUserId != request.CurrentUserId)
        {
            throw new NotificationAccessForbiddenException();
        }

        if (notification.IsRead) return true;

        notification.MarkRead();
        _notifications.Update(notification);
        await _uow.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public class MarkAllNotificationsReadHandler : IRequestHandler<MarkAllNotificationsReadCommand, int>
{
    private readonly INotificationRepository _notifications;
    private readonly IUnitOfWork _uow;

    public MarkAllNotificationsReadHandler(INotificationRepository notifications, IUnitOfWork uow)
    {
        _notifications = notifications;
        _uow = uow;
    }

    public async Task<int> Handle(MarkAllNotificationsReadCommand request, CancellationToken cancellationToken)
    {
        if (request.CurrentUserId == Guid.Empty) throw new ArgumentException("CurrentUserId is required.", nameof(request.CurrentUserId));

        var count = await _notifications.MarkAllReadAsync(request.CurrentUserId, cancellationToken);
        if (count > 0) await _uow.SaveChangesAsync(cancellationToken);
        return count;
    }
}

public class ListTenantActivityHandler : IRequestHandler<ListTenantActivityQuery, PagedResult<NotificationDto>>
{
    private readonly INotificationRepository _notifications;
    private readonly IUserRepository _users;
    private readonly ITenantContext _tenant;

    public ListTenantActivityHandler(
        INotificationRepository notifications,
        IUserRepository users,
        ITenantContext tenant)
    {
        _notifications = notifications;
        _users = users;
        _tenant = tenant;
    }

    public async Task<PagedResult<NotificationDto>> Handle(ListTenantActivityQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);

        var (rows, total) = await _notifications.SearchByTenantAsync(
            tenantId, request.Type, request.From, request.To, page, pageSize, cancellationToken);

        IReadOnlyDictionary<Guid, User> lookup;
        if (rows.Count == 0)
        {
            lookup = new Dictionary<Guid, User>();
        }
        else
        {
            var tenantUsers = await _users.ListByTenantAsync(tenantId, cancellationToken);
            lookup = tenantUsers.ToDictionary(u => u.Id);
        }

        var items = rows.Select(n => CollaborationMapper.ToDto(n, lookup)).ToList();

        return new PagedResult<NotificationDto>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize,
        };
    }
}
