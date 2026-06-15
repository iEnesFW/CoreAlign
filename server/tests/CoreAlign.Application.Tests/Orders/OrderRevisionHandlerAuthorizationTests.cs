using CoreAlign.Application.B2B;
using CoreAlign.Application.Orders.Revisions;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.Sales;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Orders;

public class OrderRevisionHandlerAuthorizationTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CallerUserId = Guid.NewGuid();
    private static readonly Guid OrderOwnerCustomerId = Guid.NewGuid();
    private static readonly Guid AnotherCustomerId = Guid.NewGuid();
    private static readonly Guid DealerAccountId = Guid.NewGuid();
    private static readonly Guid AnotherDealerAccountId = Guid.NewGuid();

    private readonly IOrderRepository _orders = Substitute.For<IOrderRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserAccessor _currentUser = Substitute.For<ICurrentUserAccessor>();
    private readonly IPortalScopeService _portalScope = Substitute.For<IPortalScopeService>();
    private readonly ITenantContext _tenant = Substitute.For<ITenantContext>();
    private readonly IOrderRevisionOutbox _outbox = Substitute.For<IOrderRevisionOutbox>();

    public OrderRevisionHandlerAuthorizationTests()
    {
        _currentUser.UserIdOrThrow().Returns(CallerUserId);
        _tenant.RequireTenantId().Returns(TenantId);
    }

    private static Order BuildDealerOriginOrder()
    {
        var order = new Order("ORD-9", OrderOwnerCustomerId, DateTime.UtcNow, "TRY")
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
        };
        var line = new OrderLine(Guid.NewGuid(), "SKU", "Widget", 5m, 100m)
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
        };
        line.SetLineNumber(1);
        order.ReplaceLines(new[] { line });
        order.MarkOrigin(OrderOriginPersona.Dealer, null, DealerAccountId, Guid.NewGuid());
        order.Submit();
        return order;
    }

    private static Order BuildTenantOriginOrder()
    {
        var order = new Order("ORD-10", OrderOwnerCustomerId, DateTime.UtcNow, "TRY")
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
        };
        var line = new OrderLine(Guid.NewGuid(), "SKU", "Widget", 5m, 100m)
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
        };
        line.SetLineNumber(1);
        order.ReplaceLines(new[] { line });
        order.Submit();
        return order;
    }

    private static OrderRevision SeedRevision(Order order, string requesterPersona)
    {
        return order.RequestRevision(
            Guid.NewGuid(),
            requesterPersona,
            order.BuildCurrentLineSnapshot(),
            null,
            DateTime.UtcNow);
    }

    [Fact]
    public async Task GetRevisions_throws_OrderNotFound_when_caller_is_a_different_customer()
    {
        var order = BuildTenantOriginOrder();
        _orders.GetWithLinesAndRevisionsAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        _portalScope.TryGetCurrentCustomerIdAsync(Arg.Any<CancellationToken>()).Returns(AnotherCustomerId);

        var sut = new GetOrderRevisionsHandler(_orders, _currentUser, _portalScope);

        var act = async () => await sut.Handle(new GetOrderRevisionsQuery(order.Id), default);

        await act.Should().ThrowAsync<OrderNotFoundException>();
    }

    [Fact]
    public async Task GetRevisions_throws_OrderNotFound_when_caller_is_a_different_dealer()
    {
        var order = BuildDealerOriginOrder();
        _orders.GetWithLinesAndRevisionsAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        _portalScope.TryGetCurrentCustomerIdAsync(Arg.Any<CancellationToken>()).Returns((Guid?)null);
        _portalScope.TryGetCurrentDealerAccountIdAsync(Arg.Any<CancellationToken>()).Returns(AnotherDealerAccountId);

        var sut = new GetOrderRevisionsHandler(_orders, _currentUser, _portalScope);

        var act = async () => await sut.Handle(new GetOrderRevisionsQuery(order.Id), default);

        await act.Should().ThrowAsync<OrderNotFoundException>();
    }

    [Fact]
    public async Task GetRevisions_allows_tenant_caller()
    {
        var order = BuildTenantOriginOrder();
        SeedRevision(order, OrderOriginPersona.Tenant);
        _orders.GetWithLinesAndRevisionsAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        _portalScope.TryGetCurrentCustomerIdAsync(Arg.Any<CancellationToken>()).Returns((Guid?)null);
        _portalScope.TryGetCurrentDealerAccountIdAsync(Arg.Any<CancellationToken>()).Returns((Guid?)null);

        var sut = new GetOrderRevisionsHandler(_orders, _currentUser, _portalScope);

        var result = await sut.Handle(new GetOrderRevisionsQuery(order.Id), default);

        result.Revisions.Should().HaveCount(1);
    }

    [Fact]
    public async Task Approve_does_NOT_fall_through_to_tenant_when_caller_is_a_different_customer()
    {
        var order = BuildDealerOriginOrder();
        SeedRevision(order, OrderOriginPersona.Dealer);
        _orders.GetWithLinesAndRevisionsAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        _portalScope.TryGetCurrentCustomerIdAsync(Arg.Any<CancellationToken>()).Returns(AnotherCustomerId);

        var sut = new ApproveOrderRevisionHandler(_orders, _uow, _currentUser, _portalScope, _tenant, _outbox);

        var act = async () => await sut.Handle(new ApproveOrderRevisionCommand(order.Id, order.Revisions.Single().Id), default);

        await act.Should().ThrowAsync<OrderNotFoundException>();
        await _outbox.DidNotReceiveWithAnyArgs().EnqueueApprovedAsync(default!, default);
    }

    [Fact]
    public async Task Approve_rejects_dealer_caller_when_dealer_requested_the_revision()
    {
        var order = BuildDealerOriginOrder();
        SeedRevision(order, OrderOriginPersona.Dealer);
        _orders.GetWithLinesAndRevisionsAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        _portalScope.TryGetCurrentCustomerIdAsync(Arg.Any<CancellationToken>()).Returns((Guid?)null);
        _portalScope.TryGetCurrentDealerAccountIdAsync(Arg.Any<CancellationToken>()).Returns(DealerAccountId);

        var sut = new ApproveOrderRevisionHandler(_orders, _uow, _currentUser, _portalScope, _tenant, _outbox);

        var act = async () => await sut.Handle(new ApproveOrderRevisionCommand(order.Id, order.Revisions.Single().Id), default);

        await act.Should().ThrowAsync<RevisionPersonaNotAuthorizedException>();
    }

    [Fact]
    public async Task Approve_allows_customer_to_approve_dealer_requested_revision_for_their_order()
    {
        var order = BuildDealerOriginOrder();
        SeedRevision(order, OrderOriginPersona.Dealer);
        _orders.GetWithLinesAndRevisionsAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        _portalScope.TryGetCurrentCustomerIdAsync(Arg.Any<CancellationToken>()).Returns(OrderOwnerCustomerId);
        _portalScope.TryGetCurrentDealerAccountIdAsync(Arg.Any<CancellationToken>()).Returns((Guid?)null);

        var sut = new ApproveOrderRevisionHandler(_orders, _uow, _currentUser, _portalScope, _tenant, _outbox);

        var dto = await sut.Handle(new ApproveOrderRevisionCommand(order.Id, order.Revisions.Single().Id), default);

        dto.Status.Should().Be(CoreAlign.Domain.Enums.RevisionStatus.Approved);
        await _outbox.Received(1).EnqueueApprovedAsync(Arg.Any<OrderRevisionApprovedPayload>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reject_throws_OrderNotFound_when_caller_is_a_different_customer()
    {
        var order = BuildDealerOriginOrder();
        SeedRevision(order, OrderOriginPersona.Dealer);
        _orders.GetWithLinesAndRevisionsAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        _portalScope.TryGetCurrentCustomerIdAsync(Arg.Any<CancellationToken>()).Returns(AnotherCustomerId);

        var sut = new RejectOrderRevisionHandler(_orders, _uow, _currentUser, _portalScope, _tenant, _outbox);

        var act = async () => await sut.Handle(
            new RejectOrderRevisionCommand(order.Id, order.Revisions.Single().Id, "no"), default);

        await act.Should().ThrowAsync<OrderNotFoundException>();
        await _outbox.DidNotReceiveWithAnyArgs().EnqueueRejectedAsync(default!, default);
    }

    [Fact]
    public async Task Cancel_throws_OrderNotFound_when_caller_is_a_different_customer()
    {
        var order = BuildDealerOriginOrder();
        SeedRevision(order, OrderOriginPersona.Dealer);
        _orders.GetWithLinesAndRevisionsAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        _portalScope.TryGetCurrentCustomerIdAsync(Arg.Any<CancellationToken>()).Returns(AnotherCustomerId);

        var sut = new CancelOrderRevisionHandler(_orders, _uow, _currentUser, _portalScope);

        var act = async () => await sut.Handle(
            new CancelOrderRevisionCommand(order.Id, order.Revisions.Single().Id), default);

        await act.Should().ThrowAsync<OrderNotFoundException>();
    }

    [Fact]
    public async Task RequestRevision_stamps_persona_as_Customer_when_caller_owns_the_order()
    {
        var order = BuildTenantOriginOrder();
        _orders.GetWithLinesAndRevisionsAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        _portalScope.TryGetCurrentCustomerIdAsync(Arg.Any<CancellationToken>()).Returns(OrderOwnerCustomerId);
        _portalScope.TryGetCurrentDealerAccountIdAsync(Arg.Any<CancellationToken>()).Returns((Guid?)null);

        var sut = new RequestOrderRevisionHandler(_orders, _uow, _currentUser, _portalScope, _tenant, _outbox);

        var firstLineProductId = order.Lines.First().ProductId;
        var dto = await sut.Handle(
            new RequestOrderRevisionCommand(
                order.Id,
                new[] { new RevisionLineInput(firstLineProductId, 2m, 100m) }),
            default);

        dto.RequestedByPersona.Should().Be(OrderOriginPersona.Customer);
    }
}
