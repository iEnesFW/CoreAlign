using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Application.Tests.Orders;

public class OrderScrapTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid ProductId = Guid.NewGuid();

    private static (Order order, Guid lineId) BuildOrder()
    {
        var order = new Order("ORD-1", CustomerId, DateTime.UtcNow, "USD")
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
        };
        var line = new OrderLine(ProductId, "SKU-A", "Widget", 10m, 5m)
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
        };
        order.ReplaceLines(new[] { line });
        return (order, line.Id);
    }

    [Fact]
    public void Scrap_records_quantity_and_reduces_remaining_to_ship()
    {
        var (order, lineId) = BuildOrder();

        order.RecordLineScrap(lineId, 3m);

        var line = order.Lines.First();
        line.QuantityScrapped.Should().Be(3m);
        line.QuantityRemainingToShip.Should().Be(7m);
    }

    [Fact]
    public void Scrap_exceeding_remaining_quantity_throws()
    {
        var (order, lineId) = BuildOrder();

        var act = () => order.RecordLineScrap(lineId, 11m);

        act.Should().Throw<InvalidOrderLineException>();
    }

    [Fact]
    public void Scrap_on_a_cancelled_order_throws()
    {
        var (order, lineId) = BuildOrder();
        order.Cancel("test");

        var act = () => order.RecordLineScrap(lineId, 1m);

        act.Should().Throw<InvalidOrderStatusTransitionException>();
    }
}
