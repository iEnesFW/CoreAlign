using CoreAlign.Application.Orders.Commands;
using CoreAlign.Application.Orders.Handlers;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Events;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Orders;

public class UpdateOrderCommandHandlerTests
{
    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>();
    private readonly ICustomerRepository _customerRepository = Substitute.For<ICustomerRepository>();
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly IProductComponentRepository _componentRepository = Substitute.For<IProductComponentRepository>();
    private readonly IAllocationService _allocationService = Substitute.For<IAllocationService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly UpdateOrderCommandHandler _sut;

    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid OrderId = Guid.NewGuid();
    private static readonly Guid ProductAId = Guid.NewGuid();

    public UpdateOrderCommandHandlerTests()
    {
        _componentRepository.GetTreeForProductsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, IReadOnlyList<(Guid, decimal)>>());
        _sut = new UpdateOrderCommandHandler(_orderRepository, _customerRepository, _productRepository, _componentRepository, _allocationService, _unitOfWork);
    }

    [Fact]
    public async Task Confirming_draft_order_with_sufficient_stock_raises_confirmed_event()
    {
        var product = BuildProduct(stock: 100);
        var order = BuildOrder(OrderStatus.Draft, line: BuildLine(product, quantity: 10));
        SetupRepositories(order, product);

        var command = BuildUpdateCommand(order, status: OrderStatus.Confirmed);

        var result = await _sut.Handle(command, default);

        result.Should().NotBeNull();
        order.Status.Should().Be(OrderStatus.Confirmed);
        order.DomainEvents.OfType<OrderConfirmedEvent>().Should().ContainSingle()
            .Which.Lines.Should().ContainSingle(l => l.ProductId == product.Id && l.Quantity == 10);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Confirming_draft_order_with_insufficient_stock_throws_and_does_not_mutate()
    {
        var product = BuildProduct(stock: 5);
        var order = BuildOrder(OrderStatus.Draft, line: BuildLine(product, quantity: 10));
        SetupRepositories(order, product);

        var command = BuildUpdateCommand(order, status: OrderStatus.Confirmed);

        Func<Task> act = () => _sut.Handle(command, default);

        await act.Should().ThrowAsync<InsufficientStockException>();
        order.Status.Should().Be(OrderStatus.Draft);
        order.DomainEvents.Should().BeEmpty();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cancelling_confirmed_order_raises_cancelled_event()
    {
        var product = BuildProduct(stock: 90);
        var order = BuildOrder(OrderStatus.Confirmed, line: BuildLine(product, quantity: 10));
        order.ClearDomainEvents();
        SetupRepositories(order, product);

        var command = BuildUpdateCommand(order, status: OrderStatus.Cancelled);

        var result = await _sut.Handle(command, default);

        result.Should().NotBeNull();
        order.Status.Should().Be(OrderStatus.Cancelled);
        order.DomainEvents.OfType<OrderCancelledFromActiveEvent>().Should().ContainSingle()
            .Which.Lines.Should().ContainSingle(l => l.ProductId == product.Id && l.Quantity == 10);
    }

    [Fact]
    public async Task Editing_header_on_non_draft_order_throws_immutable_exception()
    {
        var product = BuildProduct(stock: 100);
        var order = BuildOrder(OrderStatus.Confirmed, line: BuildLine(product, quantity: 10));
        SetupRepositories(order, product);

        var command = BuildUpdateCommand(order, status: OrderStatus.Confirmed) with
        {
            OrderNumber = "ORD-CHANGED"
        };

        Func<Task> act = () => _sut.Handle(command, default);

        await act.Should().ThrowAsync<OrderImmutableException>();
    }

    [Fact]
    public async Task Invalid_status_transition_throws()
    {
        var product = BuildProduct(stock: 100);
        var order = BuildOrder(OrderStatus.Draft, line: BuildLine(product, quantity: 1));
        SetupRepositories(order, product);

        var command = BuildUpdateCommand(order, status: OrderStatus.Shipped);

        Func<Task> act = () => _sut.Handle(command, default);

        await act.Should().ThrowAsync<InvalidOrderStatusTransitionException>();
    }

    [Fact]
    public async Task Updating_draft_order_lines_does_not_raise_stock_event()
    {
        var product = BuildProduct(stock: 50);
        var order = BuildOrder(OrderStatus.Draft, line: BuildLine(product, quantity: 5));
        SetupRepositories(order, product);

        var command = BuildUpdateCommand(order, status: OrderStatus.Draft) with
        {
            Lines = new List<OrderLineInput> { new(product.Id, Quantity: 8, UnitPrice: product.Price) }
        };

        var result = await _sut.Handle(command, default);

        result.Should().NotBeNull();
        order.Lines.Should().ContainSingle();
        order.Lines.First().Quantity.Should().Be(8);
        order.DomainEvents.OfType<OrderConfirmedEvent>().Should().BeEmpty();
    }

    [Fact]
    public async Task Confirming_with_unknown_order_throws_not_found()
    {
        _orderRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Order?)null);

        var command = new UpdateOrderCommand(
            Guid.NewGuid(),
            "ORD-1",
            CustomerId,
            DateTime.UtcNow,
            OrderStatus.Confirmed,
            "USD",
            null,
            new List<OrderLineInput>());

        Func<Task> act = () => _sut.Handle(command, default);

        await act.Should().ThrowAsync<OrderNotFoundException>();
    }

    private void SetupRepositories(Order order, params Product[] products)
    {
        _orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        _orderRepository.OrderNumberExistsAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(false);
        _customerRepository.GetByIdAsync(order.CustomerId, Arg.Any<CancellationToken>()).Returns(BuildCustomer());
        _productRepository.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(products.ToDictionary(p => p.Id));
    }

    private static Customer BuildCustomer() => new("Acme") { Id = CustomerId, TenantId = TenantId };

    private static Product BuildProduct(decimal stock)
    {
        var product = new Product("SKU-A", "Widget", "pcs", 10m, "USD", stock)
        {
            Id = ProductAId,
            TenantId = TenantId
        };
        return product;
    }

    private static OrderLine BuildLine(Product product, decimal quantity) =>
        new(product.Id, product.Sku, product.Name, quantity, product.Price)
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId
        };

    private static Order BuildOrder(OrderStatus status, OrderLine line)
    {
        var order = new Order("ORD-1", CustomerId, DateTime.UtcNow, "USD")
        {
            Id = OrderId,
            TenantId = TenantId
        };
        order.ReplaceLines(new[] { line });
        AdvanceTo(order, status);
        return order;
    }

    private static void AdvanceTo(Order order, OrderStatus target)
    {
        if (order.Status == target) return;
        var path = target switch
        {
            OrderStatus.Confirmed => new[] { OrderStatus.Confirmed },
            OrderStatus.Shipped => new[] { OrderStatus.Confirmed, OrderStatus.Shipped },
            OrderStatus.Closed => new[] { OrderStatus.Confirmed, OrderStatus.Shipped, OrderStatus.Closed },
            OrderStatus.Cancelled => new[] { OrderStatus.Cancelled },
            _ => Array.Empty<OrderStatus>()
        };
        foreach (var s in path)
        {
            order.ChangeStatus(s);
        }
    }

    private static UpdateOrderCommand BuildUpdateCommand(Order order, OrderStatus status) =>
        new(
            order.Id,
            order.OrderNumber,
            order.CustomerId,
            order.OrderDate,
            status,
            order.Currency,
            order.Notes,
            order.Lines.Select(l => new OrderLineInput(l.ProductId, l.Quantity, l.UnitPrice)).ToList());
}
