using CoreAlign.Application.Inventory.Services;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Services;

namespace CoreAlign.Application.Tests.Inventory;

/// <summary>
/// INVENTORY T3 — inter-warehouse transfer execution. A transfer is one transaction
/// of two ledger legs: a TransferOut issue at the source (valued at the source
/// AvgCost, honouring the no-oversell guard) and a TransferIn receipt at the
/// destination valued at that same source cost. It is globally value-neutral and
/// stock-neutral: total physical quantity and total inventory value are unchanged,
/// and the two SyncProductStockAsync deltas (−q then +q) net to zero on the global
/// Product.StockQuantity scalar.
/// </summary>
public class StockTransferTests
{
    private static readonly Guid ProductId = Guid.NewGuid();
    private static readonly Guid WarehouseA = Guid.NewGuid();
    private static readonly Guid WarehouseB = Guid.NewGuid();
    private static readonly Guid TenantId = Guid.NewGuid();

    private readonly IStockItemRepository _stockItems = Substitute.For<IStockItemRepository>();
    private readonly IStockMovementRepository _movements = Substitute.For<IStockMovementRepository>();
    private readonly IStockAllocationRepository _allocations = Substitute.For<IStockAllocationRepository>();
    private readonly IWarehouseRepository _warehouses = Substitute.For<IWarehouseRepository>();
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();

    private AllocationService BuildService() =>
        new(_stockItems, _movements, _allocations, _warehouses, _products,
            new StockOpeningBalanceBridge(_stockItems, _products, _movements),
            new InventoryCostingService(Substitute.For<CoreAlign.Domain.Interfaces.IStockCostLayerRepository>(), Substitute.For<CoreAlign.Domain.Interfaces.IStockItemRepository>()));

    private static StockItem StockAt(Guid warehouseId, decimal onHand, decimal avgCost)
    {
        var item = new StockItem(ProductId, warehouseId) { Id = Guid.NewGuid(), TenantId = TenantId };
        if (onHand > 0m) item.SeedOpeningBalance(onHand, avgCost, DateTime.UtcNow);
        return item;
    }

    /// <summary>Wires the global Product.StockQuantity rollup to a starting value.</summary>
    private Product SeedProduct(decimal stockQuantity)
    {
        var product = new Product("SKU-T", "Transferable", "pcs", 10m, "TRY", initialStock: stockQuantity)
        {
            Id = ProductId,
            TenantId = TenantId,
        };
        _products.GetByIdAsync(ProductId, Arg.Any<CancellationToken>()).Returns(product);
        return product;
    }

    [Fact]
    public async Task Transfer_moves_quantity_between_warehouses_at_source_cost_and_writes_both_legs()
    {
        var source = StockAt(WarehouseA, onHand: 100m, avgCost: 7m);
        var dest = StockAt(WarehouseB, onHand: 0m, avgCost: 0m);
        _stockItems.GetAsync(ProductId, WarehouseA, null, Arg.Any<CancellationToken>()).Returns(source);
        _stockItems.GetOrCreateAsync(ProductId, WarehouseB, null, Arg.Any<CancellationToken>()).Returns(dest);
        var product = SeedProduct(stockQuantity: 100m);

        var result = await BuildService().ApplyTransferAsync(ProductId, WarehouseA, WarehouseB, 30m);

        source.OnHand.Should().Be(70m);
        dest.OnHand.Should().Be(30m);
        dest.AvgCost.Should().Be(7m, "the dest is valued at the source AvgCost so value is conserved");
        result.UnitCost.Should().Be(7m);
        result.FromOnHandAfter.Should().Be(70m);
        result.ToOnHandAfter.Should().Be(30m);
        result.MovementsCreated.Should().Be(2);

        // Product.StockQuantity nets to zero: issue −30 then receipt +30.
        product.StockQuantity.Should().Be(100m);

        await _movements.Received(2).AddAsync(Arg.Any<StockMovement>(), Arg.Any<CancellationToken>());
        await _movements.Received(1).AddAsync(
            Arg.Is<StockMovement>(m => m.Type == StockMovementType.TransferOut
                && m.WarehouseId == WarehouseA
                && m.Quantity == 30m
                && m.SourceDocumentType == StockSourceDocumentType.Transfer
                && m.SourceDocumentId == result.SourceDocumentId),
            Arg.Any<CancellationToken>());
        await _movements.Received(1).AddAsync(
            Arg.Is<StockMovement>(m => m.Type == StockMovementType.TransferIn
                && m.WarehouseId == WarehouseB
                && m.Quantity == 30m
                && m.UnitCost == 7m
                && m.SourceDocumentType == StockSourceDocumentType.Transfer
                && m.SourceDocumentId == result.SourceDocumentId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Transfer_into_warehouse_with_existing_stock_recomputes_weighted_average_cost()
    {
        var source = StockAt(WarehouseA, onHand: 100m, avgCost: 8m);
        var dest = StockAt(WarehouseB, onHand: 20m, avgCost: 5m);
        _stockItems.GetAsync(ProductId, WarehouseA, null, Arg.Any<CancellationToken>()).Returns(source);
        _stockItems.GetOrCreateAsync(ProductId, WarehouseB, null, Arg.Any<CancellationToken>()).Returns(dest);
        SeedProduct(stockQuantity: 120m);

        await BuildService().ApplyTransferAsync(ProductId, WarehouseA, WarehouseB, 40m);

        // dest: (20 * 5 + 40 * 8) / 60 = 420 / 60 = 7
        dest.OnHand.Should().Be(60m);
        dest.AvgCost.Should().Be(7m);
        source.OnHand.Should().Be(60m);
    }

    [Fact]
    public async Task Transfer_rejects_self_transfer()
    {
        var product = SeedProduct(stockQuantity: 100m);
        _stockItems.GetAsync(ProductId, WarehouseA, null, Arg.Any<CancellationToken>())
            .Returns(StockAt(WarehouseA, 100m, 7m));

        Func<Task> act = () => BuildService().ApplyTransferAsync(ProductId, WarehouseA, WarehouseA, 10m);

        await act.Should().ThrowAsync<StockMovementValidationException>();
        await _movements.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        product.StockQuantity.Should().Be(100m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task Transfer_rejects_non_positive_quantity(decimal quantity)
    {
        SeedProduct(stockQuantity: 100m);

        Func<Task> act = () => BuildService().ApplyTransferAsync(ProductId, WarehouseA, WarehouseB, quantity);

        await act.Should().ThrowAsync<StockMovementValidationException>();
        await _movements.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Transfer_blocked_when_source_stock_insufficient_no_partial_move()
    {
        var source = StockAt(WarehouseA, onHand: 100m, avgCost: 7m);
        var dest = StockAt(WarehouseB, onHand: 0m, avgCost: 0m);
        _stockItems.GetAsync(ProductId, WarehouseA, null, Arg.Any<CancellationToken>()).Returns(source);
        _stockItems.GetOrCreateAsync(ProductId, WarehouseB, null, Arg.Any<CancellationToken>()).Returns(dest);
        var product = SeedProduct(stockQuantity: 100m);

        Func<Task> act = () => BuildService().ApplyTransferAsync(ProductId, WarehouseA, WarehouseB, 200m);

        await act.Should().ThrowAsync<StockMovementValidationException>();
        source.OnHand.Should().Be(100m, "the no-oversell guard blocks the issue leg before any mutation");
        dest.OnHand.Should().Be(0m);
        product.StockQuantity.Should().Be(100m);
        await _movements.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Transfer_blocked_when_source_has_no_stock_item()
    {
        _stockItems.GetAsync(ProductId, WarehouseA, null, Arg.Any<CancellationToken>())
            .Returns((StockItem?)null);
        SeedProduct(stockQuantity: 0m);

        Func<Task> act = () => BuildService().ApplyTransferAsync(ProductId, WarehouseA, WarehouseB, 10m);

        await act.Should().ThrowAsync<StockMovementValidationException>();
        await _movements.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }
}
