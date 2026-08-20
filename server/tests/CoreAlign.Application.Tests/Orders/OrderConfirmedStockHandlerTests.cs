using CoreAlign.Application.Common.Outbox;
using CoreAlign.Application.Inventory.Services;
using CoreAlign.Application.Orders.EventHandlers;
using CoreAlign.Infrastructure.Services;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Events;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Orders;

public class OrderConfirmedStockHandlerTests
{
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly IProductComponentRepository _componentRepository = Substitute.For<IProductComponentRepository>();
    private readonly IStockTransactionRepository _stockTransactionRepository = Substitute.For<IStockTransactionRepository>();
    private readonly IWarehouseRepository _warehouseRepository = Substitute.For<IWarehouseRepository>();
    private readonly IStockItemRepository _stockItemRepository = Substitute.For<IStockItemRepository>();
    private readonly IStockMovementRepository _stockMovementRepository = Substitute.For<IStockMovementRepository>();
    private readonly IGLPostingOutbox _glOutbox = Substitute.For<IGLPostingOutbox>();
    private readonly IStockOpeningBalanceBridge _openingBalanceBridge = Substitute.For<IStockOpeningBalanceBridge>();
    private readonly OrderConfirmedStockHandler _sut;

    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid OrderId = Guid.NewGuid();
    private static readonly Guid ProductId = Guid.NewGuid();

    public OrderConfirmedStockHandlerTests()
    {
        _componentRepository.GetTreeForProductsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, IReadOnlyList<(Guid, decimal)>>());
        _warehouseRepository.GetDefaultAsync(Arg.Any<CancellationToken>()).Returns((Warehouse?)null);
        _sut = new OrderConfirmedStockHandler(
            _productRepository,
            _componentRepository,
            _stockTransactionRepository,
            _warehouseRepository,
            _stockItemRepository,
            _stockMovementRepository,
            _glOutbox,
            _openingBalanceBridge,
            new InventoryCostingService(Substitute.For<CoreAlign.Domain.Interfaces.IStockCostLayerRepository>(), Substitute.For<CoreAlign.Domain.Interfaces.IStockItemRepository>()));
    }

    [Fact]
    public async Task Decrements_stock_and_writes_sale_transaction()
    {
        var product = new Product("SKU-A", "Widget", "pcs", 10m, "USD", initialStock: 50)
        {
            Id = ProductId,
            TenantId = TenantId
        };
        _productRepository.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, Product> { [product.Id] = product });

        var ev = new OrderConfirmedEvent(
            TenantId,
            OrderId,
            "ORD-1",
            new[] { new OrderLineSnapshot(ProductId, 7m) },
            DateTime.UtcNow);

        await _sut.Handle(ev, default);

        product.StockQuantity.Should().Be(43m);
        _productRepository.Received(1).Update(product);
        await _stockTransactionRepository.Received(1).AddAsync(
            Arg.Is<StockTransaction>(t => t.ProductId == ProductId && t.Quantity == -7m && t.BalanceAfter == 43m && t.OrderId == OrderId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_when_stock_insufficient_at_dispatch_time()
    {
        var product = new Product("SKU-A", "Widget", "pcs", 10m, "USD", initialStock: 3)
        {
            Id = ProductId,
            TenantId = TenantId
        };
        _productRepository.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, Product> { [product.Id] = product });

        var ev = new OrderConfirmedEvent(
            TenantId,
            OrderId,
            "ORD-1",
            new[] { new OrderLineSnapshot(ProductId, 10m) },
            DateTime.UtcNow);

        Func<Task> act = () => _sut.Handle(ev, default);

        await act.Should().ThrowAsync<InsufficientStockException>();
        product.StockQuantity.Should().Be(3m);
    }

    [Fact]
    public async Task Rejects_confirm_when_default_warehouse_atp_insufficient_even_if_global_sufficient()
    {
        // Global rollup is plentiful (100) but split across warehouses; the default
        // warehouse the issue draws from holds only 30. Per-warehouse ATP must reject
        // rather than backorder the default warehouse into negative on-hand.
        var warehouseId = Guid.NewGuid();
        _warehouseRepository.GetDefaultAsync(Arg.Any<CancellationToken>())
            .Returns(new Warehouse("WH-DEF", "Default", isDefault: true) { Id = warehouseId, TenantId = TenantId });

        var product = new Product("SKU-A", "Widget", "pcs", 10m, "USD", initialStock: 100)
        {
            Id = ProductId,
            TenantId = TenantId
        };
        _productRepository.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, Product> { [product.Id] = product });

        var item = new StockItem(ProductId, warehouseId);
        item.SeedOpeningBalance(30m, 10m, DateTime.UtcNow);
        _stockItemRepository.GetOrCreateAsync(ProductId, warehouseId, null, Arg.Any<CancellationToken>()).Returns(item);

        var ev = new OrderConfirmedEvent(
            TenantId, OrderId, "ORD-1",
            new[] { new OrderLineSnapshot(ProductId, 50m) }, DateTime.UtcNow);

        Func<Task> act = () => _sut.Handle(ev, default);

        await act.Should().ThrowAsync<InsufficientStockException>();
        product.StockQuantity.Should().Be(100m);
        item.OnHand.Should().Be(30m);
    }

    [Fact]
    public async Task Rejects_confirm_when_reserved_eats_into_availability()
    {
        // OnHand 60 but 20 reserved by other orders -> ATP 40 < 50. The gate counts
        // AvailableToPromise (OnHand - Reserved), not raw OnHand.
        var warehouseId = Guid.NewGuid();
        _warehouseRepository.GetDefaultAsync(Arg.Any<CancellationToken>())
            .Returns(new Warehouse("WH-DEF", "Default", isDefault: true) { Id = warehouseId, TenantId = TenantId });

        var product = new Product("SKU-A", "Widget", "pcs", 10m, "USD", initialStock: 60)
        {
            Id = ProductId,
            TenantId = TenantId
        };
        _productRepository.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, Product> { [product.Id] = product });

        var item = new StockItem(ProductId, warehouseId);
        item.SeedOpeningBalance(60m, 10m, DateTime.UtcNow);
        item.Reserve(20m, DateTime.UtcNow);
        _stockItemRepository.GetOrCreateAsync(ProductId, warehouseId, null, Arg.Any<CancellationToken>()).Returns(item);

        var ev = new OrderConfirmedEvent(
            TenantId, OrderId, "ORD-1",
            new[] { new OrderLineSnapshot(ProductId, 50m) }, DateTime.UtcNow);

        Func<Task> act = () => _sut.Handle(ev, default);

        await act.Should().ThrowAsync<InsufficientStockException>();
        item.OnHand.Should().Be(60m);
    }

    [Fact]
    public async Task Issues_from_default_warehouse_without_going_negative_when_atp_sufficient()
    {
        var warehouseId = Guid.NewGuid();
        _warehouseRepository.GetDefaultAsync(Arg.Any<CancellationToken>())
            .Returns(new Warehouse("WH-DEF", "Default", isDefault: true) { Id = warehouseId, TenantId = TenantId });

        var product = new Product("SKU-A", "Widget", "pcs", 10m, "USD", initialStock: 100)
        {
            Id = ProductId,
            TenantId = TenantId
        };
        _productRepository.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, Product> { [product.Id] = product });

        var item = new StockItem(ProductId, warehouseId);
        item.SeedOpeningBalance(100m, 10m, DateTime.UtcNow);
        _stockItemRepository.GetOrCreateAsync(ProductId, warehouseId, null, Arg.Any<CancellationToken>()).Returns(item);

        var ev = new OrderConfirmedEvent(
            TenantId, OrderId, "ORD-1",
            new[] { new OrderLineSnapshot(ProductId, 50m) }, DateTime.UtcNow);

        await _sut.Handle(ev, default);

        item.OnHand.Should().Be(50m);
        product.StockQuantity.Should().Be(50m);
        await _stockMovementRepository.Received(1).AddAsync(
            Arg.Is<StockMovement>(m => m.Quantity == 50m && m.Type == StockMovementType.Issue),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Make_to_stock_composite_is_sold_from_its_own_stock_not_exploded_to_components()
    {
        // A manufactured composite (ProcurementType.Make) — e.g. a produced "4+4+4" glass unit —
        // is sold from its OWN finished-goods stock; its BOM was consumed at production time, so
        // the sale must NOT re-explode it to components.
        var componentId = Guid.NewGuid();
        var composite = new Product("SKU-4+4+4", "Cift cam", "pcs", 100m, "USD", initialStock: 10)
        {
            Id = ProductId,
            TenantId = TenantId
        };
        composite.SetProcurementType(ProcurementType.Make);
        var component = new Product("SKU-4MM", "4mm cam", "pcs", 10m, "USD", initialStock: 100)
        {
            Id = componentId,
            TenantId = TenantId
        };
        _componentRepository.GetTreeForProductsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, IReadOnlyList<(Guid, decimal)>>
            {
                [ProductId] = new List<(Guid, decimal)> { (componentId, 2m) }
            });
        _productRepository.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, Product> { [ProductId] = composite, [componentId] = component });

        var ev = new OrderConfirmedEvent(
            TenantId, OrderId, "ORD-1",
            new[] { new OrderLineSnapshot(ProductId, 3m) }, DateTime.UtcNow);

        await _sut.Handle(ev, default);

        composite.StockQuantity.Should().Be(7m);
        component.StockQuantity.Should().Be(100m);
        await _stockTransactionRepository.Received(1).AddAsync(
            Arg.Is<StockTransaction>(t => t.ProductId == ProductId && t.Quantity == -3m),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Phantom_kit_composite_Buy_explodes_to_components_on_sale()
    {
        // A phantom/kit composite (ProcurementType.Buy, the default) explodes to its components on
        // sale: 3 kits × 2 = 6 of the component is issued; the kit itself is not stocked.
        var componentId = Guid.NewGuid();
        var kit = new Product("SKU-KIT", "Kit", "pcs", 100m, "USD", initialStock: 0)
        {
            Id = ProductId,
            TenantId = TenantId
        };
        var component = new Product("SKU-4MM", "4mm cam", "pcs", 10m, "USD", initialStock: 100)
        {
            Id = componentId,
            TenantId = TenantId
        };
        _componentRepository.GetTreeForProductsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, IReadOnlyList<(Guid, decimal)>>
            {
                [ProductId] = new List<(Guid, decimal)> { (componentId, 2m) }
            });
        _productRepository.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, Product> { [ProductId] = kit, [componentId] = component });

        var ev = new OrderConfirmedEvent(
            TenantId, OrderId, "ORD-1",
            new[] { new OrderLineSnapshot(ProductId, 3m) }, DateTime.UtcNow);

        await _sut.Handle(ev, default);

        component.StockQuantity.Should().Be(94m);
        kit.StockQuantity.Should().Be(0m);
    }

    [Fact]
    public async Task Confirm_succeeds_after_bridge_materializes_global_stock_into_fresh_warehouse_item()
    {
        // Product carries global stock but the default-warehouse StockItem is fresh
        // (OnHand 0). The handler must invoke the opening-balance bridge BEFORE the
        // ATP gate, so the seeded balance makes the confirm succeed instead of being
        // wrongly rejected as 0-available.
        var warehouseId = Guid.NewGuid();
        _warehouseRepository.GetDefaultAsync(Arg.Any<CancellationToken>())
            .Returns(new Warehouse("WH-DEF", "Default", isDefault: true) { Id = warehouseId, TenantId = TenantId });

        var product = new Product("SKU-A", "Widget", "pcs", 10m, "USD", initialStock: 100)
        {
            Id = ProductId,
            TenantId = TenantId
        };
        _productRepository.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, Product> { [product.Id] = product });

        var item = new StockItem(ProductId, warehouseId);
        _stockItemRepository.GetOrCreateAsync(ProductId, warehouseId, null, Arg.Any<CancellationToken>()).Returns(item);
        _openingBalanceBridge
            .When(b => b.EnsureMaterializedAsync(item, Arg.Any<CancellationToken>()))
            .Do(_ => item.SeedOpeningBalance(100m, 10m, DateTime.UtcNow));

        var ev = new OrderConfirmedEvent(
            TenantId, OrderId, "ORD-1",
            new[] { new OrderLineSnapshot(ProductId, 40m) }, DateTime.UtcNow);

        await _sut.Handle(ev, default);

        item.OnHand.Should().Be(60m);
        await _openingBalanceBridge.Received(1).EnsureMaterializedAsync(item, Arg.Any<CancellationToken>());
    }
}
