using CoreAlign.Application.B2B;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Profile.Notifications;

public sealed class ListProfileNotificationPreferencesHandler
    : IRequestHandler<ListProfileNotificationPreferencesQuery, IReadOnlyList<ProfileNotificationPreferenceDto>>
{
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IUserNotificationPreferenceRepository _preferences;

    public ListProfileNotificationPreferencesHandler(
        ICurrentUserAccessor currentUser,
        IUserNotificationPreferenceRepository preferences)
    {
        _currentUser = currentUser;
        _preferences = preferences;
    }

    public async Task<IReadOnlyList<ProfileNotificationPreferenceDto>> Handle(
        ListProfileNotificationPreferencesQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserIdOrThrow();
        var stored = await _preferences.ListByUserAsync(userId, cancellationToken);
        var byKind = stored.ToDictionary(p => p.NotificationKind, StringComparer.OrdinalIgnoreCase);

        return ProfileNotificationKinds.All
            .Select(kind => byKind.TryGetValue(kind, out var pref)
                ? new ProfileNotificationPreferenceDto(kind, pref.EmailEnabled, pref.InAppEnabled)
                : new ProfileNotificationPreferenceDto(kind, EmailEnabled: true, InAppEnabled: true))
            .ToList();
    }
}

public sealed class UpdateProfileNotificationPreferencesHandler
    : IRequestHandler<UpdateProfileNotificationPreferencesCommand, IReadOnlyList<ProfileNotificationPreferenceDto>>
{
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IUserNotificationPreferenceRepository _preferences;
    private readonly IUnitOfWork _uow;

    public UpdateProfileNotificationPreferencesHandler(
        ICurrentUserAccessor currentUser,
        IUserNotificationPreferenceRepository preferences,
        IUnitOfWork uow)
    {
        _currentUser = currentUser;
        _preferences = preferences;
        _uow = uow;
    }

    public async Task<IReadOnlyList<ProfileNotificationPreferenceDto>> Handle(
        UpdateProfileNotificationPreferencesCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserIdOrThrow();

        var trackedList = await _preferences.ListByUserTrackedAsync(userId, cancellationToken);
        var existingByKind = trackedList.ToDictionary(p => p.NotificationKind, StringComparer.OrdinalIgnoreCase);

        foreach (var item in request.Items)
        {
            if (!ProfileNotificationKinds.All.Contains(item.NotificationKind, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }
            if (existingByKind.TryGetValue(item.NotificationKind, out var existing))
            {
                existing.Update(item.EmailEnabled, item.InAppEnabled);
                _preferences.Update(existing);
            }
            else
            {
                var created = new UserNotificationPreference(userId, item.NotificationKind, item.EmailEnabled, item.InAppEnabled);
                await _preferences.AddAsync(created, cancellationToken);
                existingByKind[item.NotificationKind] = created;
            }
        }

        await _uow.SaveChangesAsync(cancellationToken);

        return ProfileNotificationKinds.All
            .Select(kind => existingByKind.TryGetValue(kind, out var pref)
                ? new ProfileNotificationPreferenceDto(kind, pref.EmailEnabled, pref.InAppEnabled)
                : new ProfileNotificationPreferenceDto(kind, EmailEnabled: true, InAppEnabled: true))
            .ToList();
    }
}
