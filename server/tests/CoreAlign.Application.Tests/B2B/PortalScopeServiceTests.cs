using System.Security.Claims;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Services;
using Microsoft.AspNetCore.Http;

namespace CoreAlign.Application.Tests.B2B;

public class PortalScopeServiceTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private readonly ITenantContext _tenant = Substitute.For<ITenantContext>();
    private readonly IHttpContextAccessor _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
    private readonly ICustomerUserRepository _customerUsers = Substitute.For<ICustomerUserRepository>();
    private readonly IDealerUserRepository _dealerUsers = Substitute.For<IDealerUserRepository>();
    private readonly IDealerCustomerLinkRepository _links = Substitute.For<IDealerCustomerLinkRepository>();
    private readonly PortalScopeService _sut;

    public PortalScopeServiceTests()
    {
        _tenant.RequireTenantId().Returns(TenantId);
        _sut = new PortalScopeService(_tenant, _httpContextAccessor, _customerUsers, _dealerUsers, _links);
    }

    [Fact]
    public async Task GetCurrentCustomerIdAsync_returns_customer_for_customer_user()
    {
        var userId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        SetAuthenticatedUser(userId);

        var membership = new CustomerUser(userId, customerId, CustomerMembershipRole.CustomerOwner, invitedByUserId: null);
        _customerUsers
            .ListActiveByUserAsync(userId, TenantId, Arg.Any<CancellationToken>())
            .Returns(new[] { membership });

        var resolved = await _sut.GetCurrentCustomerIdAsync();

        resolved.Should().Be(customerId);
    }

    [Fact]
    public async Task GetCurrentCustomerIdAsync_throws_for_user_without_customer_membership()
    {
        var userId = Guid.NewGuid();
        SetAuthenticatedUser(userId);

        _customerUsers
            .ListActiveByUserAsync(userId, TenantId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<CustomerUser>());

        var act = async () => await _sut.GetCurrentCustomerIdAsync();

        await act.Should().ThrowAsync<PortalScopeNotResolvedException>();
    }

    [Fact]
    public async Task GetCurrentDealerAccountIdAsync_returns_dealer_for_dealer_user()
    {
        var userId = Guid.NewGuid();
        var dealerId = Guid.NewGuid();
        SetAuthenticatedUser(userId);

        var membership = new DealerUser(userId, dealerId, DealerMembershipRole.DealerOwner, invitedByUserId: null);
        _dealerUsers
            .ListActiveByUserAsync(userId, TenantId, Arg.Any<CancellationToken>())
            .Returns(new[] { membership });

        var resolved = await _sut.GetCurrentDealerAccountIdAsync();

        resolved.Should().Be(dealerId);
    }

    [Fact]
    public async Task GetDealerAllowedCustomerIdsAsync_returns_only_active_links()
    {
        var userId = Guid.NewGuid();
        var dealerId = Guid.NewGuid();
        var customerActive = Guid.NewGuid();
        var customerArchived = Guid.NewGuid();
        SetAuthenticatedUser(userId);

        _dealerUsers
            .ListActiveByUserAsync(userId, TenantId, Arg.Any<CancellationToken>())
            .Returns(new[] { new DealerUser(userId, dealerId, DealerMembershipRole.DealerOwner, null) });

        var activeLink = new DealerCustomerLink(dealerId, customerActive, null);
        var archivedLink = new DealerCustomerLink(dealerId, customerArchived, null);
        archivedLink.Revoke(null, "no longer active");

        _links
            .ListByDealerAsync(dealerId, Arg.Any<CancellationToken>())
            .Returns(new[] { activeLink, archivedLink });

        var allowed = await _sut.GetDealerAllowedCustomerIdsAsync();

        allowed.Should().BeEquivalentTo(new[] { customerActive });
    }

    private void SetAuthenticatedUser(Guid userId)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim("sub", userId.ToString()),
        }, authenticationType: "Test");
        var principal = new ClaimsPrincipal(identity);
        var context = new DefaultHttpContext { User = principal };
        _httpContextAccessor.HttpContext.Returns(context);
    }
}
