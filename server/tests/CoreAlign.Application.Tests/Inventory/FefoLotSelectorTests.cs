using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Services;

namespace CoreAlign.Application.Tests.Inventory;

public sealed class FefoLotSelectorTests
{
    private readonly IStockItemRepository _stockItems = Substitute.For<IStockItemRepository>();
    private readonly ILotRepository _lots = Substitute.For<ILotRepository>();
    private readonly FefoLotSelector _sut;

    private readonly Guid _productId = Guid.NewGuid();
    private readonly Guid _warehouseId = Guid.NewGuid();
    private static readonly DateTime AsOf = new(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc);

    private readonly List<StockItem> _stock = new();
    private readonly List<Lot> _lotList = new();

    public FefoLotSelectorTests()
    {
        _sut = new FefoLotSelector(_stockItems, _lots);
        _stockItems.GetByProductAsync(_productId, Arg.Any<CancellationToken>()).Returns(_ => _stock);
        _lots.GetByProductAsync(_productId, Arg.Any<CancellationToken>()).Returns(_ => _lotList);
    }

    private Guid AddLot(decimal available, DateTime? expiry, bool blocked = false, string lotNumber = "L")
    {
        var lot = new Lot(_productId, lotNumber, expiryDate: expiry) { Id = Guid.NewGuid() };
        if (blocked)
        {
            lot.Block("test");
        }
        _lotList.Add(lot);
        var item = new StockItem(_productId, _warehouseId, lot.Id);
        item.ApplyReceipt(available, 1m, AsOf);
        _stock.Add(item);
        return lot.Id;
    }

    [Fact]
    public async Task Selects_lots_earliest_expiry_first_splitting_across_lots()
    {
        var b = AddLot(10m, new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc), lotNumber: "B");
        var a = AddLot(10m, new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc), lotNumber: "A");
        var c = AddLot(10m, new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc), lotNumber: "C");

        var plan = await _sut.SelectAsync(_productId, _warehouseId, 25m, AsOf, default);

        plan.Select(p => p.LotId).Should().ContainInOrder(a, b, c);
        plan.Select(p => p.Quantity).Should().ContainInOrder(10m, 10m, 5m);
    }

    [Fact]
    public async Task Skips_blocked_lots()
    {
        AddLot(10m, new DateTime(2026, 6, 20, 0, 0, 0, DateTimeKind.Utc), blocked: true, lotNumber: "BLOCKED");
        var a = AddLot(10m, new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc), lotNumber: "A");

        var plan = await _sut.SelectAsync(_productId, _warehouseId, 10m, AsOf, default);

        plan.Should().ContainSingle();
        plan[0].LotId.Should().Be(a);
    }

    [Fact]
    public async Task Skips_expired_lots()
    {
        AddLot(10m, new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc), lotNumber: "EXPIRED");
        var a = AddLot(10m, new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc), lotNumber: "A");

        var plan = await _sut.SelectAsync(_productId, _warehouseId, 10m, AsOf, default);

        plan.Should().ContainSingle();
        plan[0].LotId.Should().Be(a);
    }

    [Fact]
    public async Task Dated_lots_are_consumed_before_undated_lots()
    {
        var undated = AddLot(10m, null, lotNumber: "UNDATED");
        var a = AddLot(10m, new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc), lotNumber: "A");

        var plan = await _sut.SelectAsync(_productId, _warehouseId, 15m, AsOf, default);

        plan.Select(p => p.LotId).Should().ContainInOrder(a, undated);
        plan.Select(p => p.Quantity).Should().ContainInOrder(10m, 5m);
    }

    [Fact]
    public async Task Throws_when_unblocked_unexpired_lot_stock_is_insufficient()
    {
        AddLot(5m, new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc), lotNumber: "A");
        AddLot(10m, new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc), lotNumber: "EXPIRED");

        var act = () => _sut.SelectAsync(_productId, _warehouseId, 10m, AsOf, default);

        await act.Should().ThrowAsync<InsufficientStockException>();
    }
}
