using CoreAlign.Application.Common;
using MediatR;

namespace CoreAlign.Application.Collaboration;

public record CommentDto(
    Guid Id,
    string EntityType,
    Guid EntityId,
    Guid AuthorUserId,
    string AuthorName,
    string Body,
    Guid? ParentCommentId,
    DateTime CreatedAtUtc,
    DateTime? EditedAtUtc);

public record NotificationDto(
    Guid Id,
    string Type,
    string EntityType,
    Guid EntityId,
    string Title,
    string Body,
    Guid? ActorUserId,
    string? ActorName,
    bool IsRead,
    DateTime CreatedAtUtc);

public record ListCommentsQuery(string EntityType, Guid EntityId)
    : IRequest<IReadOnlyList<CommentDto>>;

public record CreateCommentCommand(
    string EntityType,
    Guid EntityId,
    string Body,
    Guid? ParentCommentId = null,
    Guid AuthorUserId = default) : IRequest<CommentDto>, ITransactionalRequest;

public record EditCommentCommand(
    Guid Id,
    string Body,
    Guid CurrentUserId = default) : IRequest<CommentDto>, ITransactionalRequest;

public record DeleteCommentCommand(
    Guid Id,
    Guid CurrentUserId = default,
    bool IsAdmin = false) : IRequest<bool>, ITransactionalRequest;

public record ListNotificationsQuery(
    bool UnreadOnly = false,
    int Take = 50,
    Guid CurrentUserId = default) : IRequest<IReadOnlyList<NotificationDto>>;

public record ListTenantActivityQuery(
    string? Type = null,
    DateTime? From = null,
    DateTime? To = null,
    int Page = 1,
    int PageSize = 30) : IRequest<PagedResult<NotificationDto>>;

public record UnreadNotificationCountQuery(Guid CurrentUserId = default)
    : IRequest<int>;

public record MarkNotificationReadCommand(
    Guid Id,
    Guid CurrentUserId = default) : IRequest<bool>, ITransactionalRequest;

public record MarkAllNotificationsReadCommand(Guid CurrentUserId = default)
    : IRequest<int>, ITransactionalRequest;
