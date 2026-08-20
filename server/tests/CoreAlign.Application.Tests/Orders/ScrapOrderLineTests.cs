using CoreAlign.Application.Orders.Commands;
using CoreAlign.Application.Orders.Handlers;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Orders;

public class ScrapOrderLineTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();

    private readonly IOrderRepository _orders = Substitute.For<IOrderRepository>();
    private readonly IAllocationService _allocator = Substitute.For<IAllocationService>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly ScrapOrderLineHandler _sut;

    public ScrapOrderLineTests()
    {
        _sut = new ScrapOrderLineHandler(_orders, _allocator, _uow);
    }

    [Fact]
    public async Task Scrapping_a_line_gives_its_reservation_back()
    {
        var order = BuildOrderWithOneLine(quantity: 10m);
        var line = order.Lines.Single();
        _orders.GetWithLinesAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        await _sut.Handle(new ScrapOrderLineCommand(order.Id, line.Id, 3m, "broken in picking"), default);

        await _allocator.Received(1).ReleaseForOrderLineAsync(
            order.Id, line.Id, 3m, Arg.Any<CancellationToken>());
        line.QuantityScrapped.Should().Be(3m);
        line.QuantityRemainingToShip.Should().Be(7m);
    }

    [Fact]
    public async Task A_line_whose_remainder_is_scrapped_counts_as_fully_shipped()
    {
        var order = BuildOrderWithOneLine(quantity: 10m);
        var line = order.Lines.Single();
        _orders.GetWithLinesAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        line.RecordShipment(7m);
        await _sut.Handle(new ScrapOrderLineCommand(order.Id, line.Id, 3m, null), default);

        line.QuantityRemainingToShip.Should().Be(0m);
        line.IsFullyShipped.Should().BeTrue();
    }

    [Fact]
    public async Task A_scrapped_unit_is_not_invoiceable()
    {
        var order = BuildOrderWithOneLine(quantity: 10m);
        var line = order.Lines.Single();
        _orders.GetWithLinesAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        await _sut.Handle(new ScrapOrderLineCommand(order.Id, line.Id, 3m, null), default);

        line.QuantityRemainingToInvoice.Should().Be(7m);
    }

    [Fact]
    public async Task The_scrap_reason_is_kept_instead_of_discarded()
    {
        var order = BuildOrderWithOneLine(quantity: 10m);
        var line = order.Lines.Single();
        _orders.GetWithLinesAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        await _sut.Handle(new ScrapOrderLineCommand(order.Id, line.Id, 3m, "broken in picking"), default);

        line.ScrapReason.Should().Be("3: broken in picking");
    }

    [Fact]
    public async Task A_second_scrap_appends_its_reason_rather_than_erasing_the_first()
    {
        var order = BuildOrderWithOneLine(quantity: 10m);
        var line = order.Lines.Single();
        _orders.GetWithLinesAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        await _sut.Handle(new ScrapOrderLineCommand(order.Id, line.Id, 3m, "broken in picking"), default);
        await _sut.Handle(new ScrapOrderLineCommand(order.Id, line.Id, 2m, "water damage"), default);

        line.ScrapReason.Should().Be("3: broken in picking | 2: water damage");
        line.QuantityScrapped.Should().Be(5m);
    }

    [Fact]
    public async Task A_scrap_without_a_reason_leaves_the_earlier_reason_alone()
    {
        var order = BuildOrderWithOneLine(quantity: 10m);
        var line = order.Lines.Single();
        _orders.GetWithLinesAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        await _sut.Handle(new ScrapOrderLineCommand(order.Id, line.Id, 3m, "broken in picking"), default);
        await _sut.Handle(new ScrapOrderLineCommand(order.Id, line.Id, 1m, null), default);

        line.ScrapReason.Should().Be("3: broken in picking");
    }

    private static Order BuildOrderWithOneLine(decimal quantity)
    {
        var order = new Order("ORD-1", CustomerId, DateTime.UtcNow, "TRY", null)
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
        };
        var line = new OrderLine(Guid.NewGuid(), "SKU-A", "Widget", quantity, 100m)
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
        };
        order.ReplaceLines(new[] { line });
        order.ChangeStatus(OrderStatus.Submitted);
        order.ChangeStatus(OrderStatus.Approved);
        order.ChangeStatus(OrderStatus.Allocated);
        return order;
    }
}
