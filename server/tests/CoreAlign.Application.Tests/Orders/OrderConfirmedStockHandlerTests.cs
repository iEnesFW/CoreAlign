using CoreAlign.Application.Orders.EventHandlers;
using CoreAlign.Domain.Entities;
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
            _stockMovementRepository);
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
}
