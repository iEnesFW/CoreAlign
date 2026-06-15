using CoreAlign.Application.Collaboration;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.B2B;

public class ListTenantActivityHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private readonly INotificationRepository _notifications = Substitute.For<INotificationRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ITenantContext _tenant = Substitute.For<ITenantContext>();

    private readonly ListTenantActivityHandler _sut;

    public ListTenantActivityHandlerTests()
    {
        _tenant.RequireTenantId().Returns(TenantId);
        _users.ListByTenantAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(new List<User>());
        _sut = new ListTenantActivityHandler(_notifications, _users, _tenant);
    }

    [Fact]
    public async Task Forwards_type_filter_to_repository_and_maps_results()
    {
        var n1 = new Notification(
            recipientUserId: Guid.NewGuid(),
            actorUserId: null,
            type: "DealerOrderSubmitted",
            entityType: "Order",
            entityId: Guid.NewGuid(),
            title: "x",
            body: "y");
        var n2 = new Notification(
            recipientUserId: Guid.NewGuid(),
            actorUserId: null,
            type: "DealerOrderSubmitted",
            entityType: "Order",
            entityId: Guid.NewGuid(),
            title: "x",
            body: "y");

        _notifications.SearchByTenantAsync(
                TenantId, "DealerOrderSubmitted", null, null, 1, 30, Arg.Any<CancellationToken>())
            .Returns((new List<Notification> { n1, n2 }, 2));

        var result = await _sut.Handle(
            new ListTenantActivityQuery(Type: "DealerOrderSubmitted"), default);

        result.Total.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items.All(d => d.Type == "DealerOrderSubmitted").Should().BeTrue();
        await _notifications.Received(1).SearchByTenantAsync(
            TenantId, "DealerOrderSubmitted", null, null, 1, 30, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Clamps_page_and_pageSize_then_paginates()
    {
        _notifications.SearchByTenantAsync(
                TenantId, null, null, null, 1, 1, Arg.Any<CancellationToken>())
            .Returns((new List<Notification>(), 0));

        var result = await _sut.Handle(
            new ListTenantActivityQuery(Page: 0, PageSize: 0), default);

        result.Page.Should().Be(1);
        result.PageSize.Should().Be(1);
        result.Total.Should().Be(0);
        result.Items.Should().BeEmpty();
        await _notifications.Received(1).SearchByTenantAsync(
            TenantId, null, null, null, 1, 1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Forwards_from_and_to_date_filters()
    {
        var from = DateTime.UtcNow.AddDays(-7);
        var to = DateTime.UtcNow;

        _notifications.SearchByTenantAsync(
                TenantId, null, from, to, 2, 50, Arg.Any<CancellationToken>())
            .Returns((new List<Notification>(), 0));

        await _sut.Handle(
            new ListTenantActivityQuery(From: from, To: to, Page: 2, PageSize: 50), default);

        await _notifications.Received(1).SearchByTenantAsync(
            TenantId, null, from, to, 2, 50, Arg.Any<CancellationToken>());
    }
}
