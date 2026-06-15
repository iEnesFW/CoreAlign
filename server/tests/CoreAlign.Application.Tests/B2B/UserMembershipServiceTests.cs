using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Services;

namespace CoreAlign.Application.Tests.B2B;

public class UserMembershipServiceTests
{
    private readonly IDealerUserRepository _dealerUsers = Substitute.For<IDealerUserRepository>();
    private readonly ICustomerUserRepository _customerUsers = Substitute.For<ICustomerUserRepository>();
    private readonly UserMembershipService _sut;

    public UserMembershipServiceTests()
    {
        _sut = new UserMembershipService(_dealerUsers, _customerUsers);
    }

    [Fact]
    public async Task ResolvePersonaAsync_returns_dealer_when_dealer_membership_exists()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        _dealerUsers.AnyActiveForUserAsync(userId, tenantId, Arg.Any<CancellationToken>()).Returns(true);
        _customerUsers.AnyActiveForUserAsync(userId, tenantId, Arg.Any<CancellationToken>()).Returns(true);

        var persona = await _sut.ResolvePersonaAsync(userId, tenantId);

        persona.Should().Be(UserPersona.Dealer);
        await _customerUsers.DidNotReceive().AnyActiveForUserAsync(userId, tenantId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResolvePersonaAsync_returns_customer_when_only_customer_membership_exists()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        _dealerUsers.AnyActiveForUserAsync(userId, tenantId, Arg.Any<CancellationToken>()).Returns(false);
        _customerUsers.AnyActiveForUserAsync(userId, tenantId, Arg.Any<CancellationToken>()).Returns(true);

        var persona = await _sut.ResolvePersonaAsync(userId, tenantId);

        persona.Should().Be(UserPersona.Customer);
    }

    [Fact]
    public async Task ResolvePersonaAsync_returns_tenant_when_no_memberships_exist()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        _dealerUsers.AnyActiveForUserAsync(userId, tenantId, Arg.Any<CancellationToken>()).Returns(false);
        _customerUsers.AnyActiveForUserAsync(userId, tenantId, Arg.Any<CancellationToken>()).Returns(false);

        var persona = await _sut.ResolvePersonaAsync(userId, tenantId);

        persona.Should().Be(UserPersona.Tenant);
    }
}
