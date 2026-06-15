using CoreAlign.Application.Inventory.Services;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Inventory;

public class StockOpeningBalanceBridgeTests
{
    private readonly IStockItemRepository _stockItems = Substitute.For<IStockItemRepository>();
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly IStockMovementRepository _movements = Substitute.For<IStockMovementRepository>();
    private readonly StockOpeningBalanceBridge _sut;

    private static readonly Guid ProductId = Guid.NewGuid();
    private static readonly Guid WarehouseId = Guid.NewGuid();

    public StockOpeningBalanceBridgeTests()
    {
        _stockItems.GetByProductAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new List<StockItem>());
        _sut = new StockOpeningBalanceBridge(_stockItems, _products, _movements);
    }

    [Fact]
    public async Task Seeds_fresh_item_from_global_stock_quantity()
    {
        var product = new Product("SKU-A", "Widget", "pcs", 10m, "USD", initialStock: 80) { Id = ProductId };
        _products.GetByIdAsync(ProductId, Arg.Any<CancellationToken>()).Returns(product);
        var item = new StockItem(ProductId, WarehouseId) { Id = Guid.NewGuid() };

        await _sut.EnsureMaterializedAsync(item, default);

        item.OnHand.Should().Be(80m);
        await _movements.Received(1).AddAsync(
            Arg.Is<StockMovement>(m => m.Type == StockMovementType.OpeningBalance && m.Quantity == 80m),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Skips_item_that_already_has_a_movement()
    {
        var item = new StockItem(ProductId, WarehouseId) { Id = Guid.NewGuid() };
        item.SeedOpeningBalance(5m, 10m, DateTime.UtcNow);

        await _sut.EnsureMaterializedAsync(item, default);

        item.OnHand.Should().Be(5m);
        await _movements.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Skips_when_stock_exists_in_a_sibling_warehouse()
    {
        var sibling = new StockItem(ProductId, Guid.NewGuid()) { Id = Guid.NewGuid() };
        sibling.SeedOpeningBalance(40m, 10m, DateTime.UtcNow);
        _stockItems.GetByProductAsync(ProductId, Arg.Any<CancellationToken>())
            .Returns(new List<StockItem> { sibling });
        var item = new StockItem(ProductId, WarehouseId) { Id = Guid.NewGuid() };

        await _sut.EnsureMaterializedAsync(item, default);

        item.OnHand.Should().Be(0m);
        await _movements.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Skips_when_product_has_no_global_stock()
    {
        var product = new Product("SKU-A", "Widget", "pcs", 10m, "USD", initialStock: 0) { Id = ProductId };
        _products.GetByIdAsync(ProductId, Arg.Any<CancellationToken>()).Returns(product);
        var item = new StockItem(ProductId, WarehouseId) { Id = Guid.NewGuid() };

        await _sut.EnsureMaterializedAsync(item, default);

        item.OnHand.Should().Be(0m);
        await _movements.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }
}
