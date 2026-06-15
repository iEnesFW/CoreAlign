using CoreAlign.Application.Common.Outbox;
using CoreAlign.Application.Inventory.Services;
using CoreAlign.Application.Orders.EventHandlers;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Events;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Orders;

public class OrderConfirmedStockHandlerServiceLineTests
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

    public OrderConfirmedStockHandlerServiceLineTests()
    {
        _componentRepository.GetTreeForProductsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, IReadOnlyList<(Guid, decimal)>>());

        var warehouse = new Warehouse("WH-DEF", "Default", isDefault: true) { Id = Guid.NewGuid(), TenantId = TenantId };
        _warehouseRepository.GetDefaultAsync(Arg.Any<CancellationToken>()).Returns(warehouse);

        _stockItemRepository
            .GetOrCreateAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var productId = call.ArgAt<Guid>(0);
                var warehouseId = call.ArgAt<Guid>(1);
                var item = new StockItem(productId, warehouseId, null) { Id = Guid.NewGuid(), TenantId = TenantId };
                // Confirm now enforces per-warehouse availability; seed ample on-hand
                // so this exclusion test exercises movement filtering, not the gate.
                item.SeedOpeningBalance(1000m, 10m, DateTime.UtcNow);
                return item;
            });

        _sut = new OrderConfirmedStockHandler(
            _productRepository,
            _componentRepository,
            _stockTransactionRepository,
            _warehouseRepository,
            _stockItemRepository,
            _stockMovementRepository,
            _glOutbox,
            _openingBalanceBridge);
    }

    [Fact]
    public async Task Service_line_is_excluded_from_stock_movements_in_emitted_event()
    {
        var stockProduct = BuildProduct("STK-1", "Hinge");
        _productRepository.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, Product> { [stockProduct.Id] = stockProduct });

        var order = BuildDraftOrder();
        order.ReplaceLines(new[]
        {
            BuildOrderLine(stockProduct.Id, quantity: 4m, isService: false),
            BuildServiceLine("Installation labor", quantity: 1m)
        });

        var confirmedEvent = TransitionToConfirmedAndCaptureEvent(order);
        confirmedEvent.Lines.Should().HaveCount(1);
        confirmedEvent.Lines.Single().ProductId.Should().Be(stockProduct.Id);

        await _sut.Handle(confirmedEvent, default);

        await _stockMovementRepository.Received(1).AddAsync(
            Arg.Is<StockMovement>(m => m.ProductId == stockProduct.Id && m.Quantity == 4m),
            Arg.Any<CancellationToken>());
        await _stockMovementRepository.ReceivedWithAnyArgs(1).AddAsync(default!, default);
    }

    [Fact]
    public async Task Service_only_order_produces_zero_stock_movements()
    {
        _productRepository.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, Product>());

        var order = BuildDraftOrder();
        order.ReplaceLines(new[]
        {
            BuildServiceLine("Installation labor", quantity: 1m),
            BuildServiceLine("Transport", quantity: 2m)
        });

        var confirmedEvent = TransitionToConfirmedAndCaptureEvent(order);
        confirmedEvent.Lines.Should().BeEmpty();

        await _sut.Handle(confirmedEvent, default);

        await _stockMovementRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await _stockTransactionRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    private static OrderConfirmedEvent TransitionToConfirmedAndCaptureEvent(Order order)
    {
        order.ChangeStatus(OrderStatus.Confirmed);
        var ev = order.DomainEvents.OfType<OrderConfirmedEvent>().Single();
        order.ClearDomainEvents();
        return ev;
    }

    private static Order BuildDraftOrder()
    {
        var order = new Order("ORD-1", Guid.NewGuid(), DateTime.UtcNow, "TRY", "test")
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId
        };
        return order;
    }

    private static OrderLine BuildOrderLine(Guid productId, decimal quantity, bool isService)
    {
        return new OrderLine(
            productId,
            productSku: "SKU",
            productName: "Stock Item",
            quantity: quantity,
            unitPrice: 10m,
            sourceBomLineId: Guid.NewGuid(),
            sourceProjectId: Guid.NewGuid(),
            isService: isService);
    }

    private static OrderLine BuildServiceLine(string description, decimal quantity)
    {
        return new OrderLine(
            Guid.Empty,
            productSku: "SERVICE",
            productName: description,
            quantity: quantity,
            unitPrice: 100m,
            sourceBomLineId: Guid.NewGuid(),
            sourceProjectId: Guid.NewGuid(),
            isService: true);
    }

    private static Product BuildProduct(string sku, string name)
    {
        return new Product(sku, name, "pcs", 10m, "TRY", initialStock: 100m)
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId
        };
    }
}
