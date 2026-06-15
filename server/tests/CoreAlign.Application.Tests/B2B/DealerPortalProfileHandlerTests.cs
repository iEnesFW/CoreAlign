using CoreAlign.Application.B2B;
using CoreAlign.Application.B2B.DealerPortal;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.B2B;

public class DealerPortalProfileHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid DealerAccountId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    private readonly IPortalScopeService _scope = Substitute.For<IPortalScopeService>();
    private readonly ICurrentUserAccessor _currentUser = Substitute.For<ICurrentUserAccessor>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ITenantRepository _tenants = Substitute.For<ITenantRepository>();
    private readonly IDealerAccountRepository _dealers = Substitute.For<IDealerAccountRepository>();
    private readonly IDealerUserRepository _dealerUsers = Substitute.For<IDealerUserRepository>();
    private readonly ITenantContext _tenant = Substitute.For<ITenantContext>();

    public DealerPortalProfileHandlerTests()
    {
        _scope.GetCurrentDealerAccountIdAsync(Arg.Any<CancellationToken>()).Returns(DealerAccountId);
        _currentUser.UserIdOrThrow().Returns(UserId);
        _tenant.RequireTenantId().Returns(TenantId);
    }

    [Fact]
    public async Task Profile_returns_user_dealer_and_tenant_info()
    {
        var user = new User(TenantId, "demo", "dealer@example.com", "hash")
        {
            Id = UserId,
            FirstName = "Demo",
            LastName = "User",
            PhoneNumber = "+90555",
            LastLoginAtUtc = DateTime.UtcNow,
        };
        _users.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(user);
        var tenant = new Tenant("TestTenant", "tt") { Id = TenantId };
        _tenants.GetByIdAsync(TenantId, Arg.Any<CancellationToken>()).Returns(tenant);
        var dealer = new DealerAccount("BAYI", "Demo Bayi", createdByUserId: null) { Id = DealerAccountId, TenantId = TenantId };
        _dealers.GetByIdAsync(DealerAccountId, Arg.Any<CancellationToken>()).Returns(dealer);
        var membership = new DealerUser(UserId, DealerAccountId, DealerMembershipRole.DealerOwner, invitedByUserId: null)
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
        };
        _dealerUsers.GetByUserAndDealerAsync(UserId, DealerAccountId, Arg.Any<CancellationToken>()).Returns(membership);

        var handler = new GetDealerPortalProfileHandler(_scope, _currentUser, _users, _tenants, _dealers, _dealerUsers, _tenant);
        var result = await handler.Handle(new GetDealerPortalProfileQuery(), default);

        result.UserId.Should().Be(UserId);
        result.Email.Should().Be("dealer@example.com");
        result.FirstName.Should().Be("Demo");
        result.LastName.Should().Be("User");
        result.DealerName.Should().Be("Demo Bayi");
        result.DealerCode.Should().Be("BAYI");
        result.TenantName.Should().Be("TestTenant");
        result.MembershipRole.Should().Be(DealerMembershipRole.DealerOwner);
    }

    [Fact]
    public async Task Profile_throws_when_dealer_account_is_missing()
    {
        _users.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(new User(TenantId, "x", "x@x", "h") { Id = UserId });
        _tenants.GetByIdAsync(TenantId, Arg.Any<CancellationToken>()).Returns(new Tenant("T", "t") { Id = TenantId });
        _dealers.GetByIdAsync(DealerAccountId, Arg.Any<CancellationToken>()).Returns((DealerAccount?)null);

        var handler = new GetDealerPortalProfileHandler(_scope, _currentUser, _users, _tenants, _dealers, _dealerUsers, _tenant);
        var act = async () => await handler.Handle(new GetDealerPortalProfileQuery(), default);

        await act.Should().ThrowAsync<DealerAccountNotFoundException>();
    }
}
