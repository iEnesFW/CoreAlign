using CoreAlign.Application.Orders.Commands;
using CoreAlign.Application.Orders.Handlers;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Idempotency;

[Collection(IdempotencyTestCollection.Name)]
public class ApproveOrderIdempotencyTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid ApproverId = Guid.NewGuid();

    private readonly IOrderRepository _orders = Substitute.For<IOrderRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly ApproveOrderHandler _sut;

    public ApproveOrderIdempotencyTests()
    {
        _sut = new ApproveOrderHandler(_orders, _uow);
    }

    [Fact]
    public async Task FirstApproval_TransitionsOrderToApproved()
    {
        var order = BuildSubmittedOrder();
        _orders.GetWithLinesAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var result = await _sut.Handle(new ApproveOrderCommand(order.Id, ApproverId), default);

        result.Status.Should().Be(OrderStatus.Approved);
        order.Status.Should().Be(OrderStatus.Approved);
        order.ApprovedByUserId.Should().Be(ApproverId);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SecondApproval_OnSameOrder_IsRejectedByStateMachine()
    {
        var order = BuildSubmittedOrder();
        order.Approve(ApproverId);
        _orders.GetWithLinesAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        Func<Task> act = () => _sut.Handle(new ApproveOrderCommand(order.Id, ApproverId), default);

        await act.Should().ThrowAsync<InvalidOrderStatusTransitionException>(
            "double-approval is prevented by the FSM — naturally idempotent at the aggregate level");
        order.Status.Should().Be(OrderStatus.Approved, "status MUST remain Approved without flipping");
    }

    private static Order BuildSubmittedOrder()
    {
        var order = new Order(
            orderNumber: $"ORD-{Guid.NewGuid():N}",
            customerId: CustomerId,
            orderDate: DateTime.UtcNow,
            currency: "TRY")
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
        };
        var line = new OrderLine(Guid.NewGuid(), "SKU-1", "Widget", 1m, 100m)
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
        };
        order.Lines.Add(line);
        order.Submit();
        return order;
    }
}
