using CoreAlign.Application.B2B;
using CoreAlign.Application.Returns;
using CoreAlign.Application.Returns.Commands;
using CoreAlign.Application.Returns.Handlers;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Returns;

// OrderLine.QuantityReturned only advances when the goods are RECEIVED, so the returnable-quantity
// guard on a new request could not see a request that was already open. Two requests for the same
// shipped units were both accepted and both received: the stock went back twice and COGS was
// reversed twice. Same shape as the open-shipment claim on the dispatch side.
public class OverlappingReturnRequestTests
{
    private static readonly Guid OrderId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();

    private readonly IReturnRequestRepository _returns = Substitute.For<IReturnRequestRepository>();
    private readonly IOrderRepository _orders = Substitute.For<IOrderRepository>();
    private readonly IDocumentSequenceRepository _sequences = Substitute.For<IDocumentSequenceRepository>();
    private readonly ICurrentUserAccessor _currentUser = Substitute.For<ICurrentUserAccessor>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly CreateReturnRequestCommandHandler _sut;

    public OverlappingReturnRequestTests()
    {
        _sequences.ConsumeAsync(DocumentSequenceType.ReturnRequestNumber, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns("RMA-2026-000001");
        _returns.GetByOrderAsync(OrderId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ReturnRequest>());
        _sut = new CreateReturnRequestCommandHandler(_returns, _orders, _sequences, _currentUser, _uow);
    }

    private static (Order Order, OrderLine Line) ShippedOrder(decimal quantity, decimal shipped)
    {
        var order = new Order("SO-1", CustomerId, DateTime.UtcNow, "TRY") { Id = OrderId, TenantId = Guid.NewGuid() };
        var line = new OrderLine(Guid.NewGuid(), "SKU-A", "Widget", quantity, 100m) { Id = Guid.NewGuid() };
        order.Lines.Add(line);
        order.Submit();
        order.Approve(Guid.NewGuid());
        order.MarkAllocated(null);
        order.ChangeStatus(OrderStatus.Shipped);
        line.RecordShipment(shipped);
        return (order, line);
    }

    private ReturnRequest OpenRequestFor(Order order, OrderLine line, decimal quantity, ReturnRequestStatus status)
    {
        var request = new ReturnRequest("RMA-OPEN", order, ReturnReasonCode.DamagedInTransit, null, null, null, null)
        {
            Id = Guid.NewGuid(),
            TenantId = order.TenantId,
        };
        request.ReplaceLines(new[] { new ReturnRequestLine(line, quantity, restockable: true, lineNotes: null) });
        if (status == ReturnRequestStatus.Approved) request.Approve(Guid.NewGuid());
        if (status == ReturnRequestStatus.Rejected) request.Reject(Guid.NewGuid(), "no");
        if (status == ReturnRequestStatus.Cancelled) request.Cancel();
        return request;
    }

    private CreateReturnRequestCommand Command(OrderLine line, decimal quantity) =>
        new(OrderId, ReturnReasonCode.DamagedInTransit, null,
            new List<CreateReturnRequestLineInput> { new(line.Id, quantity, true, null) });

    [Theory]
    [InlineData(ReturnRequestStatus.Requested)]
    [InlineData(ReturnRequestStatus.Approved)]
    public async Task An_open_request_reserves_the_quantity_it_claimed(ReturnRequestStatus status)
    {
        var (order, line) = ShippedOrder(quantity: 10m, shipped: 10m);
        _orders.GetWithLinesAsync(OrderId, Arg.Any<CancellationToken>()).Returns(order);
        _returns.GetByOrderAsync(OrderId, Arg.Any<CancellationToken>())
            .Returns(new[] { OpenRequestFor(order, line, 7m, status) });

        var act = () => _sut.Handle(Command(line, 5m), default);

        await act.Should().ThrowAsync<ReturnExceedsShippedException>();
    }

    [Fact]
    public async Task What_the_open_request_left_over_can_still_be_returned()
    {
        var (order, line) = ShippedOrder(quantity: 10m, shipped: 10m);
        _orders.GetWithLinesAsync(OrderId, Arg.Any<CancellationToken>()).Returns(order);
        _returns.GetByOrderAsync(OrderId, Arg.Any<CancellationToken>())
            .Returns(new[] { OpenRequestFor(order, line, 7m, ReturnRequestStatus.Requested) });

        var result = await _sut.Handle(Command(line, 3m), default);

        result.Should().NotBeNull();
        await _returns.Received(1).AddAsync(Arg.Any<ReturnRequest>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(ReturnRequestStatus.Rejected)]
    [InlineData(ReturnRequestStatus.Cancelled)]
    public async Task A_closed_request_releases_the_quantity_it_had_claimed(ReturnRequestStatus status)
    {
        var (order, line) = ShippedOrder(quantity: 10m, shipped: 10m);
        _orders.GetWithLinesAsync(OrderId, Arg.Any<CancellationToken>()).Returns(order);
        _returns.GetByOrderAsync(OrderId, Arg.Any<CancellationToken>())
            .Returns(new[] { OpenRequestFor(order, line, 10m, status) });

        var result = await _sut.Handle(Command(line, 10m), default);

        result.Should().NotBeNull();
    }

    // Defence in depth: whatever route reaches the receive, the goods coming back can never exceed
    // the goods that went out.
    [Fact]
    public void Recording_a_return_beyond_the_shipped_quantity_is_refused()
    {
        var (_, line) = ShippedOrder(quantity: 10m, shipped: 6m);
        line.RecordReturn(4m);

        var act = () => line.RecordReturn(3m);

        act.Should().Throw<ReturnExceedsShippedException>();
        line.QuantityReturned.Should().Be(4m);
    }

    [Fact]
    public void Recording_a_return_up_to_the_shipped_quantity_is_allowed()
    {
        var (_, line) = ShippedOrder(quantity: 10m, shipped: 6m);

        line.RecordReturn(6m);

        line.QuantityReturned.Should().Be(6m);
        line.Status.Should().Be(OrderLineStatus.Returned);
    }
}
