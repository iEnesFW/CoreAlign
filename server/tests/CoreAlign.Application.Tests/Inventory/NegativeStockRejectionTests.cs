using CoreAlign.Application.Inventory.Commands;
using CoreAlign.Application.Inventory.Handlers;
using CoreAlign.Application.Inventory.Services;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.Catalog;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Services;

namespace CoreAlign.Application.Tests.Inventory;

/// <summary>
/// Rule 16: stock-mutating commands must reject taking on-hand below zero unless
/// backorder/negative stock is explicitly allowed. Each test issues/adjusts more
/// than is on hand and asserts a domain exception with the balance left untouched.
/// </summary>
public class NegativeStockRejectionTests
{
    private static readonly Guid ProductId = Guid.NewGuid();
    private static readonly Guid WarehouseId = Guid.NewGuid();
    private static readonly Guid TenantId = Guid.NewGuid();

    private readonly IStockItemRepository _stockItems = Substitute.For<IStockItemRepository>();
    private readonly IStockMovementRepository _movements = Substitute.For<IStockMovementRepository>();
    private readonly IStockAllocationRepository _allocations = Substitute.For<IStockAllocationRepository>();
    private readonly IWarehouseRepository _warehouses = Substitute.For<IWarehouseRepository>();
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private AllocationService BuildService() =>
        new(_stockItems, _movements, _allocations, _warehouses, _products,
            new StockOpeningBalanceBridge(_stockItems, _products, _movements));

    private static IssueStockCommand IssueCommand(decimal quantity) =>
        new(ProductId, WarehouseId, quantity, null, null, null, null, null);

    private static AdjustStockCommand AdjustCommand(decimal delta) =>
        new(ProductId, WarehouseId, delta, null, null, null, null);

    private static StockItem StockWith(decimal onHand, decimal reserved = 0m)
    {
        var item = new StockItem(ProductId, WarehouseId) { Id = Guid.NewGuid(), TenantId = TenantId };
        item.SeedOpeningBalance(onHand, 5m, DateTime.UtcNow);
        if (reserved > 0m) item.Reserve(reserved, DateTime.UtcNow);
        return item;
    }

    [Fact]
    public async Task IssueStock_rejects_when_quantity_exceeds_on_hand()
    {
        var item = StockWith(onHand: 4m);
        _stockItems.GetAsync(ProductId, WarehouseId, null, Arg.Any<CancellationToken>()).Returns(item);
        var handler = new IssueStockHandler(BuildService(), _uow);

        Func<Task> act = () => handler.Handle(IssueCommand(10m), default);

        await act.Should().ThrowAsync<StockMovementValidationException>();
        item.OnHand.Should().Be(4m);
        await _movements.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await _uow.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task IssueStock_allows_issuing_exactly_on_hand_down_to_zero()
    {
        var item = StockWith(onHand: 10m);
        _stockItems.GetAsync(ProductId, WarehouseId, null, Arg.Any<CancellationToken>()).Returns(item);
        var handler = new IssueStockHandler(BuildService(), _uow);

        await handler.Handle(IssueCommand(10m), default);

        item.OnHand.Should().Be(0m);
        await _movements.Received(1).AddAsync(Arg.Any<StockMovement>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IssueStock_rejects_when_no_stock_item_exists_at_warehouse()
    {
        _stockItems.GetAsync(ProductId, WarehouseId, null, Arg.Any<CancellationToken>())
            .Returns((StockItem?)null);
        var handler = new IssueStockHandler(BuildService(), _uow);

        Func<Task> act = () => handler.Handle(IssueCommand(1m), default);

        await act.Should().ThrowAsync<StockMovementValidationException>();
    }

    [Fact]
    public async Task AdjustStock_rejects_negative_delta_that_drives_on_hand_below_zero()
    {
        var item = StockWith(onHand: 3m);
        _stockItems.GetOrCreateAsync(ProductId, WarehouseId, null, Arg.Any<CancellationToken>()).Returns(item);
        var handler = new AdjustStockHandler(BuildService(), _uow);

        Func<Task> act = () => handler.Handle(AdjustCommand(-5m), default);

        await act.Should().ThrowAsync<StockMovementValidationException>();
        item.OnHand.Should().Be(3m);
        await _movements.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task AdjustStock_allows_negative_delta_that_lands_exactly_on_zero()
    {
        var item = StockWith(onHand: 5m);
        _stockItems.GetOrCreateAsync(ProductId, WarehouseId, null, Arg.Any<CancellationToken>()).Returns(item);
        var handler = new AdjustStockHandler(BuildService(), _uow);

        await handler.Handle(AdjustCommand(-5m), default);

        item.OnHand.Should().Be(0m);
        await _movements.Received(1).AddAsync(Arg.Any<StockMovement>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AllocateStock_rejects_reserving_more_than_available_to_promise()
    {
        var item = StockWith(onHand: 6m, reserved: 4m); // ATP = 2
        item.Product = new Product("SKU-A", "Widget", "pcs", 10m, "TRY") { Id = ProductId, TenantId = TenantId };
        item.Warehouse = new Warehouse("WH1", "Main") { Id = WarehouseId, TenantId = TenantId };
        _stockItems.GetOrCreateAsync(ProductId, WarehouseId, null, Arg.Any<CancellationToken>()).Returns(item);
        var service = BuildService();

        Func<Task> act = () => service.ReserveAsync(new AllocationRequest(
            Guid.NewGuid(), Guid.NewGuid(), ProductId, WarehouseId, 5m));

        await act.Should().ThrowAsync<InsufficientAvailableStockException>();
        item.Reserved.Should().Be(4m);
        item.AvailableToPromise.Should().Be(2m);
        await _allocations.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public void StockItem_domain_guard_rejects_issue_below_zero_directly()
    {
        var item = StockWith(onHand: 2m);

        var act = () => item.ApplyIssue(3m, DateTime.UtcNow);

        act.Should().Throw<StockMovementValidationException>();
        item.OnHand.Should().Be(2m);
    }

    [Fact]
    public void StockItem_honours_explicit_backorder_flag_and_goes_negative()
    {
        var item = StockWith(onHand: 2m);

        item.ApplyIssue(5m, DateTime.UtcNow, allowNegative: true);

        item.OnHand.Should().Be(-3m);
    }

    [Fact]
    public void ProductVariant_AdjustStock_rejects_decrement_below_zero()
    {
        var variant = new ProductVariant(Guid.NewGuid(), "SKU-V", "{}", stockQuantity: 4m)
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
        };

        var act = () => variant.AdjustStock(-10m);

        act.Should().Throw<InsufficientStockException>();
        variant.StockQuantity.Should().Be(4m);
    }
}
