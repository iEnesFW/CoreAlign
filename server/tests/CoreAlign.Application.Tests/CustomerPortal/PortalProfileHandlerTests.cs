using CoreAlign.Application.B2B;
using CoreAlign.Application.CustomerPortal.Profile;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.CustomerPortal;

public class PortalProfileHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();

    private readonly IPortalScopeService _scope = Substitute.For<IPortalScopeService>();
    private readonly ICurrentUserAccessor _currentUser = Substitute.For<ICurrentUserAccessor>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ITenantRepository _tenants = Substitute.For<ITenantRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IUserSessionRepository _sessions = Substitute.For<IUserSessionRepository>();
    private readonly IRefreshTokenRepository _refreshTokens = Substitute.For<IRefreshTokenRepository>();

    public PortalProfileHandlerTests()
    {
        _scope.GetCurrentCustomerIdAsync(Arg.Any<CancellationToken>()).Returns(CustomerId);
        _currentUser.UserIdOrThrow().Returns(UserId);
    }

    [Fact]
    public async Task GetProfile_returns_current_user_details()
    {
        var user = BuildUser(firstName: "Ada", lastName: "Lovelace", twoFactorEnabled: true);
        var tenant = BuildTenant();
        _users.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(user);
        _tenants.GetByIdAsync(user.TenantId, Arg.Any<CancellationToken>()).Returns(tenant);

        var handler = new GetPortalProfileHandler(_scope, _currentUser, _users, _tenants);
        var result = await handler.Handle(new GetPortalProfileQuery(), default);

        result.UserId.Should().Be(UserId);
        result.FirstName.Should().Be("Ada");
        result.LastName.Should().Be("Lovelace");
        result.IsTwoFactorEnabled.Should().BeTrue();
        result.TenantId.Should().Be(tenant.Id);
    }

    [Fact]
    public async Task UpdateProfile_persists_normalised_locale()
    {
        var user = BuildUser();
        var tenant = BuildTenant();
        _users.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(user);
        _tenants.GetByIdAsync(user.TenantId, Arg.Any<CancellationToken>()).Returns(tenant);

        var handler = new UpdatePortalProfileHandler(_scope, _currentUser, _users, _tenants, _uow);
        var result = await handler.Handle(
            new UpdatePortalProfileCommand("Grace", "Hopper", "+905001112233", "TR"),
            default);

        result.FirstName.Should().Be("Grace");
        result.LastName.Should().Be("Hopper");
        result.PhoneNumber.Should().Be("+905001112233");
        result.PreferredLocale.Should().Be("tr");
        user.PreferredLocale.Should().Be("tr");
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateProfile_throws_when_user_missing()
    {
        _users.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns((User?)null);

        var handler = new UpdatePortalProfileHandler(_scope, _currentUser, _users, _tenants, _uow);
        var act = async () => await handler.Handle(new UpdatePortalProfileCommand("a", null, null, null), default);
        await act.Should().ThrowAsync<UserNotFoundException>();
    }

    [Fact]
    public async Task ListSessions_orders_by_last_activity_desc()
    {
        var older = new UserSession(UserId, "h1", DateTime.UtcNow.AddDays(7), "old-device", "1.1.1.1")
        {
            LastActivityAtUtc = DateTime.UtcNow.AddHours(-3),
        };
        var newer = new UserSession(UserId, "h2", DateTime.UtcNow.AddDays(7), "new-device", "2.2.2.2")
        {
            LastActivityAtUtc = DateTime.UtcNow.AddMinutes(-1),
        };
        _sessions.GetActiveByUserIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(new List<UserSession> { older, newer });

        var handler = new ListPortalSessionsHandler(_scope, _currentUser, _sessions);
        var result = await handler.Handle(new ListPortalSessionsQuery(), default);

        result.Should().HaveCount(2);
        result[0].DeviceInfo.Should().Be("new-device");
        result[1].DeviceInfo.Should().Be("old-device");
    }

    [Fact]
    public async Task RevokeAllSessions_revokes_sessions_and_refresh_tokens()
    {
        _sessions.GetActiveByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(new List<UserSession> { new(UserId, "h", DateTime.UtcNow.AddDays(7), "d", "i") });

        var handler = new RevokeAllPortalSessionsHandler(_scope, _currentUser, _sessions, _refreshTokens, _uow);
        var count = await handler.Handle(new RevokeAllPortalSessionsCommand(), default);

        count.Should().Be(1);
        await _sessions.Received(1).RevokeAllByUserIdAsync(UserId, Arg.Any<CancellationToken>());
        await _refreshTokens.Received(1).RevokeAllByUserIdAsync(UserId, Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static User BuildUser(string? firstName = "Test", string? lastName = "User", bool twoFactorEnabled = false)
    {
        var user = new User(TenantId, "tester", "tester@example.com", "hash")
        {
            Id = UserId,
            FirstName = firstName,
            LastName = lastName,
            IsTwoFactorEnabled = twoFactorEnabled,
        };
        return user;
    }

    private static Tenant BuildTenant()
    {
        var tenant = new Tenant("Acme", "acme");
        typeof(Tenant).GetProperty(nameof(Tenant.Id))!.SetValue(tenant, Guid.NewGuid());
        return tenant;
    }
}
