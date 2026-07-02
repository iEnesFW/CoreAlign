using CoreAlign.Application.Orders.Commands;
using CoreAlign.Application.Orders.DTOs;
using CoreAlign.Domain.Exceptions;
using MediatR;

namespace CoreAlign.Application.Tests.Orders;

public class BulkOrderActionCommandHandlerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();

    [Fact]
    public async Task Bulk_action_reports_partial_success_and_continues_past_business_failures()
    {
        var ok1 = Guid.NewGuid();
        var failing = Guid.NewGuid();
        var ok2 = Guid.NewGuid();

        _mediator.Send(Arg.Any<IRequest<OrderDto>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var cmd = (ApproveOrderCommand)call.Arg<IRequest<OrderDto>>();
                if (cmd.Id == failing)
                {
                    throw new OrderNotFoundException();
                }
                return Task.FromResult<OrderDto>(null!);
            });

        var handler = new BulkOrderActionCommandHandler(_mediator);

        var result = await handler.Handle(
            new BulkOrderActionCommand(new List<Guid> { ok1, failing, ok2 }, BulkOrderActionType.Approve),
            CancellationToken.None);

        result.SucceededCount.Should().Be(2);
        result.FailedCount.Should().Be(1);
        result.Items.Should().HaveCount(3);
        result.Items.Single(i => i.OrderId == failing).Success.Should().BeFalse();
        result.Items.Single(i => i.OrderId == failing).Error.Should().NotBeNullOrWhiteSpace();
        await _mediator.Received(3).Send(Arg.Any<IRequest<OrderDto>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Bulk_cancel_forwards_the_reason_to_each_order()
    {
        var id = Guid.NewGuid();
        CancelOrderCommand? captured = null;
        _mediator.Send(Arg.Any<IRequest<OrderDto>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                captured = call.Arg<IRequest<OrderDto>>() as CancelOrderCommand;
                return Task.FromResult<OrderDto>(null!);
            });

        var handler = new BulkOrderActionCommandHandler(_mediator);
        await handler.Handle(
            new BulkOrderActionCommand(new List<Guid> { id }, BulkOrderActionType.Cancel, "duplicate"),
            CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Reason.Should().Be("duplicate");
    }
}
