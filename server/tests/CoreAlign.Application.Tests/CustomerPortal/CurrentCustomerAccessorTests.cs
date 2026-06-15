using CoreAlign.Application.B2B;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Infrastructure.Services;

namespace CoreAlign.Application.Tests.CustomerPortal;

public class CurrentCustomerAccessorTests
{
    private readonly IPortalScopeService _scope = Substitute.For<IPortalScopeService>();

    [Fact]
    public async Task GetCustomerIdAsync_returns_value_from_portal_scope_when_user_has_customer_membership()
    {
        var expected = Guid.NewGuid();
        _scope.TryGetCurrentCustomerIdAsync(Arg.Any<CancellationToken>()).Returns(expected);

        var sut = new CurrentCustomerAccessor(_scope);
        var actual = await sut.GetCustomerIdAsync();

        actual.Should().Be(expected);
    }

    [Fact]
    public async Task GetCustomerIdAsync_returns_null_when_user_has_no_customer_membership()
    {
        _scope.TryGetCurrentCustomerIdAsync(Arg.Any<CancellationToken>()).Returns((Guid?)null);

        var sut = new CurrentCustomerAccessor(_scope);
        var actual = await sut.GetCustomerIdAsync();

        actual.Should().BeNull();
    }

    [Fact]
    public async Task GetCustomerIdOrThrowAsync_returns_value_when_user_has_customer_membership()
    {
        var expected = Guid.NewGuid();
        _scope.TryGetCurrentCustomerIdAsync(Arg.Any<CancellationToken>()).Returns(expected);

        var sut = new CurrentCustomerAccessor(_scope);
        var actual = await sut.GetCustomerIdOrThrowAsync();

        actual.Should().Be(expected);
    }

    [Fact]
    public async Task GetCustomerIdOrThrowAsync_throws_PortalScopeNotResolvedException_when_no_customer_link()
    {
        _scope.TryGetCurrentCustomerIdAsync(Arg.Any<CancellationToken>()).Returns((Guid?)null);

        var sut = new CurrentCustomerAccessor(_scope);
        var act = async () => await sut.GetCustomerIdOrThrowAsync();

        await act.Should().ThrowAsync<PortalScopeNotResolvedException>();
    }

    [Fact]
    public async Task GetCustomerIdOrThrowAsync_throws_PortalScopeNotResolvedException_when_resolved_to_empty_guid()
    {
        _scope.TryGetCurrentCustomerIdAsync(Arg.Any<CancellationToken>()).Returns(Guid.Empty);

        var sut = new CurrentCustomerAccessor(_scope);
        var act = async () => await sut.GetCustomerIdOrThrowAsync();

        await act.Should().ThrowAsync<PortalScopeNotResolvedException>();
    }

    [Fact]
    public async Task GetCustomerIdAsync_forwards_cancellation_token()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        _scope.TryGetCurrentCustomerIdAsync(cts.Token).Returns((Guid?)null);

        var sut = new CurrentCustomerAccessor(_scope);
        var actual = await sut.GetCustomerIdAsync(cts.Token);

        actual.Should().BeNull();
        await _scope.Received(1).TryGetCurrentCustomerIdAsync(cts.Token);
    }
}
