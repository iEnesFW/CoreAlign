using CoreAlign.Application.B2B;
using CoreAlign.Application.Collaboration;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.CustomerPortal.Notifications;

public static class PortalNotificationKinds
{
    public const string OrderComment = "OrderComment";
    public const string OrderStatus = "OrderStatus";
    public const string InvoiceIssued = "InvoiceIssued";
    public const string InvoicePaymentReceived = "InvoicePaymentReceived";
    public const string DealerApprovalRequest = "DealerApprovalRequest";

    public static readonly IReadOnlyList<string> All = new[]
    {
        OrderComment,
        OrderStatus,
        InvoiceIssued,
        InvoicePaymentReceived,
        DealerApprovalRequest,
    };
}

public class ListPortalNotificationsHandler : IRequestHandler<ListPortalNotificationsQuery, IReadOnlyList<NotificationDto>>
{
    private readonly IPortalScopeService _scope;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IMediator _mediator;

    public ListPortalNotificationsHandler(IPortalScopeService scope, ICurrentUserAccessor currentUser, IMediator mediator)
    {
        _scope = scope;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public async Task<IReadOnlyList<NotificationDto>> Handle(ListPortalNotificationsQuery request, CancellationToken cancellationToken)
    {
        await _scope.GetCurrentCustomerIdAsync(cancellationToken);
        var userId = _currentUser.UserIdOrThrow();
        return await _mediator.Send(new ListNotificationsQuery(request.UnreadOnly, request.Take, userId), cancellationToken);
    }
}

public class GetPortalUnreadCountHandler : IRequestHandler<GetPortalUnreadCountQuery, int>
{
    private readonly IPortalScopeService _scope;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IMediator _mediator;

    public GetPortalUnreadCountHandler(IPortalScopeService scope, ICurrentUserAccessor currentUser, IMediator mediator)
    {
        _scope = scope;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public async Task<int> Handle(GetPortalUnreadCountQuery request, CancellationToken cancellationToken)
    {
        await _scope.GetCurrentCustomerIdAsync(cancellationToken);
        var userId = _currentUser.UserIdOrThrow();
        return await _mediator.Send(new UnreadNotificationCountQuery(userId), cancellationToken);
    }
}

public class MarkPortalNotificationReadHandler : IRequestHandler<MarkPortalNotificationReadCommand, bool>
{
    private readonly IPortalScopeService _scope;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IMediator _mediator;

    public MarkPortalNotificationReadHandler(IPortalScopeService scope, ICurrentUserAccessor currentUser, IMediator mediator)
    {
        _scope = scope;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public async Task<bool> Handle(MarkPortalNotificationReadCommand request, CancellationToken cancellationToken)
    {
        await _scope.GetCurrentCustomerIdAsync(cancellationToken);
        var userId = _currentUser.UserIdOrThrow();
        return await _mediator.Send(new MarkNotificationReadCommand(request.NotificationId, userId), cancellationToken);
    }
}

public class MarkAllPortalNotificationsReadHandler : IRequestHandler<MarkAllPortalNotificationsReadCommand, int>
{
    private readonly IPortalScopeService _scope;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IMediator _mediator;

    public MarkAllPortalNotificationsReadHandler(IPortalScopeService scope, ICurrentUserAccessor currentUser, IMediator mediator)
    {
        _scope = scope;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public async Task<int> Handle(MarkAllPortalNotificationsReadCommand request, CancellationToken cancellationToken)
    {
        await _scope.GetCurrentCustomerIdAsync(cancellationToken);
        var userId = _currentUser.UserIdOrThrow();
        return await _mediator.Send(new MarkAllNotificationsReadCommand(userId), cancellationToken);
    }
}

public class ListPortalNotificationPreferencesHandler : IRequestHandler<ListPortalNotificationPreferencesQuery, IReadOnlyList<NotificationPreferenceDto>>
{
    private readonly IPortalScopeService _scope;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IUserNotificationPreferenceRepository _preferences;

    public ListPortalNotificationPreferencesHandler(
        IPortalScopeService scope,
        ICurrentUserAccessor currentUser,
        IUserNotificationPreferenceRepository preferences)
    {
        _scope = scope;
        _currentUser = currentUser;
        _preferences = preferences;
    }

    public async Task<IReadOnlyList<NotificationPreferenceDto>> Handle(ListPortalNotificationPreferencesQuery request, CancellationToken cancellationToken)
    {
        await _scope.GetCurrentCustomerIdAsync(cancellationToken);
        var userId = _currentUser.UserIdOrThrow();
        var stored = await _preferences.ListByUserAsync(userId, cancellationToken);
        var byKind = stored.ToDictionary(p => p.NotificationKind, StringComparer.OrdinalIgnoreCase);

        return PortalNotificationKinds.All
            .Select(kind => byKind.TryGetValue(kind, out var pref)
                ? new NotificationPreferenceDto(kind, pref.EmailEnabled, pref.InAppEnabled)
                : new NotificationPreferenceDto(kind, EmailEnabled: true, InAppEnabled: true))
            .ToList();
    }
}

public class UpdatePortalNotificationPreferenceHandler : IRequestHandler<UpdatePortalNotificationPreferenceCommand, NotificationPreferenceDto>
{
    private readonly IPortalScopeService _scope;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IUserNotificationPreferenceRepository _preferences;
    private readonly IUnitOfWork _uow;

    public UpdatePortalNotificationPreferenceHandler(
        IPortalScopeService scope,
        ICurrentUserAccessor currentUser,
        IUserNotificationPreferenceRepository preferences,
        IUnitOfWork uow)
    {
        _scope = scope;
        _currentUser = currentUser;
        _preferences = preferences;
        _uow = uow;
    }

    public async Task<NotificationPreferenceDto> Handle(UpdatePortalNotificationPreferenceCommand request, CancellationToken cancellationToken)
    {
        await _scope.GetCurrentCustomerIdAsync(cancellationToken);
        var userId = _currentUser.UserIdOrThrow();

        if (!PortalNotificationKinds.All.Contains(request.NotificationKind, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Unknown notification kind '{request.NotificationKind}'.", nameof(request.NotificationKind));
        }

        var existing = await _preferences.GetAsync(userId, request.NotificationKind, cancellationToken);
        if (existing is null)
        {
            existing = new UserNotificationPreference(userId, request.NotificationKind, request.EmailEnabled, request.InAppEnabled);
            await _preferences.AddAsync(existing, cancellationToken);
        }
        else
        {
            existing.Update(request.EmailEnabled, request.InAppEnabled);
            _preferences.Update(existing);
        }

        await _uow.SaveChangesAsync(cancellationToken);
        return new NotificationPreferenceDto(existing.NotificationKind, existing.EmailEnabled, existing.InAppEnabled);
    }
}
