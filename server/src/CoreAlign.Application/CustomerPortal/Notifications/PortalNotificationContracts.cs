using CoreAlign.Application.Collaboration;
using CoreAlign.Application.Common;
using MediatR;

namespace CoreAlign.Application.CustomerPortal.Notifications;

public record ListPortalNotificationsQuery(bool UnreadOnly = false, int Take = 50)
    : IRequest<IReadOnlyList<NotificationDto>>;

public record GetPortalUnreadCountQuery() : IRequest<int>;

public record MarkPortalNotificationReadCommand(Guid NotificationId)
    : IRequest<bool>, ITransactionalRequest;

public record MarkAllPortalNotificationsReadCommand()
    : IRequest<int>, ITransactionalRequest;

public record ListPortalNotificationPreferencesQuery()
    : IRequest<IReadOnlyList<NotificationPreferenceDto>>;

public record UpdatePortalNotificationPreferenceCommand(
    string NotificationKind,
    bool EmailEnabled,
    bool InAppEnabled) : IRequest<NotificationPreferenceDto>, ITransactionalRequest;

public record NotificationPreferenceDto(
    string NotificationKind,
    bool EmailEnabled,
    bool InAppEnabled);
