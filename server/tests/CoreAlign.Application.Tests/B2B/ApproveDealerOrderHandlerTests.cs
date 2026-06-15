using CoreAlign.Application.B2B;
using CoreAlign.Application.B2B.DealerOrderFlow;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.B2B;

public class ApproveDealerOrderHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid DealerAccountId = Guid.NewGuid();
    private static readonly Guid ProductId = Guid.NewGuid();

    private readonly IPortalScopeService _scope = Substitute.For<IPortalScopeService>();
    private readonly ICurrentUserAccessor _currentUser = Substitute.For<ICurrentUserAccessor>();
    private readonly ITenantContext _tenant = Substitute.For<ITenantContext>();
    private readonly IOrderRepository _orders = Substitute.For<IOrderRepository>();
    private readonly ICustomerRepository _customers = Substitute.For<ICustomerRepository>();
    private readonly IDealerAccountRepository _dealers = Substitute.For<IDealerAccountRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IDealerOrderApprovalOutbox _outbox = Substitute.For<IDealerOrderApprovalOutbox>();

    private readonly ApproveDealerOrderHandler _sut;

    public ApproveDealerOrderHandlerTests()
    {
        _scope.GetCurrentCustomerIdAsync(Arg.Any<CancellationToken>()).Returns(CustomerId);
        _currentUser.UserIdOrThrow().Returns(UserId);
        _tenant.RequireTenantId().Returns(TenantId);

        _sut = new ApproveDealerOrderHandler(
            _scope, _currentUser, _tenant, _orders, _customers, _dealers, _uow, _outbox);
    }

    [Fact]
    public async Task Approve_flips_to_Submitted_and_enqueues_outbox()
    {
        var order = BuildPendingOrder();
        _orders.GetWithLinesAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        _customers.GetByIdAsync(CustomerId, Arg.Any<CancellationToken>())
            .Returns(new Customer("Acme Holding") { Id = CustomerId, TenantId = TenantId });
        _dealers.GetByIdAsync(DealerAccountId, Arg.Any<CancellationToken>())
            .Returns(new DealerAccount("BAYI", "Demo Bayi", createdByUserId: null) { Id = DealerAccountId, TenantId = TenantId });

        var dto = await _sut.Handle(new ApproveDealerOrderCommand(order.Id), default);

        order.Status.Should().Be(OrderStatus.Submitted);
        order.DealerApprovalStatus.Should().Be(DealerOrderApprovalStatuses.Approved);
        order.DealerApprovedByUserId.Should().Be(UserId);
        dto.Status.Should().Be(OrderStatus.Submitted);

        await _outbox.Received(1).EnqueueApprovedAsync(
            Arg.Is<DealerOrderApprovedByCustomerPayload>(p =>
                p.OrderId == order.Id
                && p.CustomerId == CustomerId
                && p.ApprovedByUserId == UserId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cross_customer_access_returns_404_with_no_info_leak()
    {
        var otherCustomerId = Guid.NewGuid();
        var foreign = new Order("ORD-FOREIGN", otherCustomerId, DateTime.UtcNow, "TRY") { Id = Guid.NewGuid(), TenantId = TenantId };
        _orders.GetWithLinesAsync(foreign.Id, Arg.Any<CancellationToken>()).Returns(foreign);

        var act = async () => await _sut.Handle(new ApproveDealerOrderCommand(foreign.Id), default);
        await act.Should().ThrowAsync<OrderNotFoundException>();

        await _outbox.DidNotReceive().EnqueueApprovedAsync(
            Arg.Any<DealerOrderApprovedByCustomerPayload>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Approving_already_approved_order_throws()
    {
        var order = BuildPendingOrder();
        order.ApproveDealerSubmission(Guid.NewGuid());
        _orders.GetWithLinesAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var act = async () => await _sut.Handle(new ApproveDealerOrderCommand(order.Id), default);
        await act.Should().ThrowAsync<InvalidOrderApprovalStateException>();
    }

    private Order BuildPendingOrder()
    {
        var order = new Order("ORD-1", CustomerId, DateTime.UtcNow, "TRY") { Id = Guid.NewGuid(), TenantId = TenantId };
        var line = new OrderLine(ProductId, "SKU-A", "Widget", 1m, 100m) { Id = Guid.NewGuid(), TenantId = TenantId };
        order.ReplaceLines(new[] { line });
        order.MarkOrigin(OrderOriginPersona.Dealer, null, DealerAccountId, Guid.NewGuid());
        return order;
    }
}

public class RejectDealerOrderHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid DealerAccountId = Guid.NewGuid();
    private static readonly Guid ProductId = Guid.NewGuid();

    private readonly IPortalScopeService _scope = Substitute.For<IPortalScopeService>();
    private readonly ICurrentUserAccessor _currentUser = Substitute.For<ICurrentUserAccessor>();
    private readonly ITenantContext _tenant = Substitute.For<ITenantContext>();
    private readonly IOrderRepository _orders = Substitute.For<IOrderRepository>();
    private readonly ICustomerRepository _customers = Substitute.For<ICustomerRepository>();
    private readonly IDealerAccountRepository _dealers = Substitute.For<IDealerAccountRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IDealerOrderApprovalOutbox _outbox = Substitute.For<IDealerOrderApprovalOutbox>();

    private readonly RejectDealerOrderHandler _sut;

    public RejectDealerOrderHandlerTests()
    {
        _scope.GetCurrentCustomerIdAsync(Arg.Any<CancellationToken>()).Returns(CustomerId);
        _currentUser.UserIdOrThrow().Returns(UserId);
        _tenant.RequireTenantId().Returns(TenantId);

        _sut = new RejectDealerOrderHandler(
            _scope, _currentUser, _tenant, _orders, _customers, _dealers, _uow, _outbox);
    }

    [Fact]
    public async Task Reject_cancels_order_records_reason_and_enqueues_outbox()
    {
        var order = BuildPendingOrder();
        _orders.GetWithLinesAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        _customers.GetByIdAsync(CustomerId, Arg.Any<CancellationToken>())
            .Returns(new Customer("Acme Holding") { Id = CustomerId, TenantId = TenantId });

        var dto = await _sut.Handle(new RejectDealerOrderCommand(order.Id, "Fiyat yüksek"), default);

        order.Status.Should().Be(OrderStatus.Cancelled);
        order.DealerApprovalStatus.Should().Be(DealerOrderApprovalStatuses.Rejected);
        order.DealerRejectionReason.Should().Be("Fiyat yüksek");
        dto.DealerApprovalStatus.Should().Be(DealerOrderApprovalStatuses.Rejected);

        await _outbox.Received(1).EnqueueRejectedAsync(
            Arg.Is<DealerOrderRejectedByCustomerPayload>(p =>
                p.OrderId == order.Id
                && p.Reason == "Fiyat yüksek"),
            Arg.Any<CancellationToken>());
    }

    private Order BuildPendingOrder()
    {
        var order = new Order("ORD-1", CustomerId, DateTime.UtcNow, "TRY") { Id = Guid.NewGuid(), TenantId = TenantId };
        var line = new OrderLine(ProductId, "SKU-A", "Widget", 1m, 100m) { Id = Guid.NewGuid(), TenantId = TenantId };
        order.ReplaceLines(new[] { line });
        order.MarkOrigin(OrderOriginPersona.Dealer, null, DealerAccountId, Guid.NewGuid());
        return order;
    }
}
