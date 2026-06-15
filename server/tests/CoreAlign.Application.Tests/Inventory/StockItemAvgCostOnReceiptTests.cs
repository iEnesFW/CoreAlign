using CoreAlign.Domain.Entities;

namespace CoreAlign.Application.Tests.Inventory;

/// <summary>
/// INVENTORY T5 — StockItem.AvgCost is the single source of truth for inventory
/// valuation and MUST recompute as the running weighted average on each receipt,
/// not merely carry its seeded value. Each receipt blends the prior inventory value
/// (AvgCost * OnHand) with the incoming value (unitCost * quantity) over the new
/// total quantity, rounded to 4 decimal places.
/// </summary>
public class StockItemAvgCostOnReceiptTests
{
    private static StockItem NewItem() =>
        new(Guid.NewGuid(), Guid.NewGuid()) { Id = Guid.NewGuid(), TenantId = Guid.NewGuid() };

    [Fact]
    public void Receipt_recomputes_avgcost_as_weighted_average()
    {
        var item = NewItem();

        item.ApplyReceipt(quantity: 10m, unitCost: 5m, occurredAtUtc: DateTime.UtcNow);
        item.OnHand.Should().Be(10m);
        item.AvgCost.Should().Be(5m, "first receipt sets AvgCost to the receipt unit cost");

        // (10 * 5 + 10 * 7) / 20 = 120 / 20 = 6
        item.ApplyReceipt(quantity: 10m, unitCost: 7m, occurredAtUtc: DateTime.UtcNow);
        item.OnHand.Should().Be(20m);
        item.AvgCost.Should().Be(6m, "second receipt blends 10@5 with 10@7 to a weighted average of 6");

        // (20 * 6 + 20 * 9) / 40 = 300 / 40 = 7.5
        item.ApplyReceipt(quantity: 20m, unitCost: 9m, occurredAtUtc: DateTime.UtcNow);
        item.OnHand.Should().Be(40m);
        item.AvgCost.Should().Be(7.5m, "third receipt blends 20@6 with 20@9 to a weighted average of 7.5");
    }

    [Fact]
    public void Receipt_rounds_avgcost_to_four_decimal_places()
    {
        var item = NewItem();

        item.ApplyReceipt(quantity: 3m, unitCost: 10m, occurredAtUtc: DateTime.UtcNow);
        // (3 * 10 + 0 ... ) then 1@5: (30 + 5) / 4 = 35 / 4 = 8.75 — exact, so blend an
        // un-terminating ratio: 3@10 + 1@5 over 3 already = 35/3 path below.
        item.ApplyReceipt(quantity: 0.0001m, unitCost: 5m, occurredAtUtc: DateTime.UtcNow);

        // Whatever the exact ratio, the stored AvgCost must be rounded to 4 dp.
        item.AvgCost.Should().Be(Math.Round(item.AvgCost, 4));
    }
}
