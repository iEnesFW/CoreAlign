using CoreAlign.Application.B2B;
using CoreAlign.Application.CustomerPortal.Notifications;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.CustomerPortal;

public class PortalNotificationPreferencesHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();

    private readonly IPortalScopeService _scope = Substitute.For<IPortalScopeService>();
    private readonly ICurrentUserAccessor _currentUser = Substitute.For<ICurrentUserAccessor>();
    private readonly IUserNotificationPreferenceRepository _prefs = Substitute.For<IUserNotificationPreferenceRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    public PortalNotificationPreferencesHandlerTests()
    {
        _scope.GetCurrentCustomerIdAsync(Arg.Any<CancellationToken>()).Returns(CustomerId);
        _currentUser.UserIdOrThrow().Returns(UserId);
    }

    [Fact]
    public async Task ListPreferences_returns_defaults_for_kinds_without_stored_row()
    {
        _prefs.ListByUserAsync(UserId, Arg.Any<CancellationToken>()).Returns(Array.Empty<UserNotificationPreference>());

        var handler = new ListPortalNotificationPreferencesHandler(_scope, _currentUser, _prefs);
        var result = await handler.Handle(new ListPortalNotificationPreferencesQuery(), default);

        result.Should().HaveCount(PortalNotificationKinds.All.Count);
        result.Should().OnlyContain(p => p.EmailEnabled && p.InAppEnabled);
    }

    [Fact]
    public async Task ListPreferences_overlays_stored_rows_on_defaults()
    {
        var stored = new UserNotificationPreference(UserId, PortalNotificationKinds.OrderComment, emailEnabled: false, inAppEnabled: true);
        _prefs.ListByUserAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(new List<UserNotificationPreference> { stored });

        var handler = new ListPortalNotificationPreferencesHandler(_scope, _currentUser, _prefs);
        var result = await handler.Handle(new ListPortalNotificationPreferencesQuery(), default);

        var commentPref = result.Single(p => p.NotificationKind == PortalNotificationKinds.OrderComment);
        commentPref.EmailEnabled.Should().BeFalse();
        commentPref.InAppEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task UpdatePreference_creates_when_absent_then_persists()
    {
        _prefs.GetAsync(UserId, PortalNotificationKinds.InvoiceIssued, Arg.Any<CancellationToken>())
            .Returns((UserNotificationPreference?)null);

        var handler = new UpdatePortalNotificationPreferenceHandler(_scope, _currentUser, _prefs, _uow);
        var result = await handler.Handle(
            new UpdatePortalNotificationPreferenceCommand(PortalNotificationKinds.InvoiceIssued, EmailEnabled: false, InAppEnabled: true),
            default);

        result.EmailEnabled.Should().BeFalse();
        result.InAppEnabled.Should().BeTrue();
        await _prefs.Received(1).AddAsync(Arg.Any<UserNotificationPreference>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdatePreference_updates_existing_row()
    {
        var existing = new UserNotificationPreference(UserId, PortalNotificationKinds.OrderStatus, true, true);
        _prefs.GetAsync(UserId, PortalNotificationKinds.OrderStatus, Arg.Any<CancellationToken>()).Returns(existing);

        var handler = new UpdatePortalNotificationPreferenceHandler(_scope, _currentUser, _prefs, _uow);
        await handler.Handle(
            new UpdatePortalNotificationPreferenceCommand(PortalNotificationKinds.OrderStatus, EmailEnabled: false, InAppEnabled: false),
            default);

        existing.EmailEnabled.Should().BeFalse();
        existing.InAppEnabled.Should().BeFalse();
        await _prefs.DidNotReceive().AddAsync(Arg.Any<UserNotificationPreference>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdatePreference_rejects_unknown_kind()
    {
        var handler = new UpdatePortalNotificationPreferenceHandler(_scope, _currentUser, _prefs, _uow);
        var act = async () => await handler.Handle(
            new UpdatePortalNotificationPreferenceCommand("BadKind", true, true), default);

        await act.Should().ThrowAsync<ArgumentException>();
    }
}
