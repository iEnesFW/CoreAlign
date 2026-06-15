using CoreAlign.Application.B2B;
using CoreAlign.Application.Inventory.Services;
using CoreAlign.Application.Mrp;
using CoreAlign.Application.Mrp.Planning;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.Manufacturing;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Mrp;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoreAlign.Application.Tests.Mrp;

public class CompletePlannedProductionOrderTests
{
    private readonly IPlannedProductionOrderRepository _productionOrders =
        Substitute.For<IPlannedProductionOrderRepository>();
    private readonly IWarehouseRepository _warehouses = Substitute.For<IWarehouseRepository>();
    private readonly IProductionExecutionService _execution = Substitute.For<IProductionExecutionService>();

    private static readonly Guid ProductId = Guid.NewGuid();
    private static readonly Guid DefaultWarehouseId = Guid.NewGuid();

    private MrpPlanningService Build() => new(
        Substitute.For<IMrpPlanningDataLoader>(),
        Substitute.For<IMrpPlanningEngine>(),
        Substitute.For<IMrpPlanRunRepository>(),
        _productionOrders,
        Substitute.For<IPurchaseRequisitionRepository>(),
        Substitute.For<IProductRepository>(),
        Substitute.For<IDocumentSequenceRepository>(),
        _warehouses,
        _execution,
        Substitute.For<ICurrentUserAccessor>(),
        Substitute.For<IUnitOfWork>(),
        NullLogger<MrpPlanningService>.Instance);

    private static PlannedProductionOrder NewOrder(decimal qty = 20m)
    {
        var order = new PlannedProductionOrder(
            sourcePlanRunId: Guid.NewGuid(),
            productId: ProductId,
            lowLevelCode: 1,
            quantity: qty,
            dueDateUtc: new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc),
            releaseDateUtc: new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc),
            estimatedUnitCost: 4m,
            sourcePolicy: LotSizingPolicy.LotForLot,
            peggingParentProductId: null,
            peggingSourceOrderLineId: null)
        {
            TenantId = Guid.NewGuid()
        };
        return order;
    }

    private static Warehouse DefaultWarehouse()
    {
        var wh = new Warehouse("MAIN", "Main", WarehouseType.Main, isDefault: true)
        {
            Id = DefaultWarehouseId,
            TenantId = Guid.NewGuid()
        };
        return wh;
    }

    [Fact]
    public async Task Completing_released_order_executes_stock_and_closes_order()
    {
        var order = NewOrder(qty: 20m);
        order.Release();
        _productionOrders.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        _warehouses.GetDefaultAsync(Arg.Any<CancellationToken>()).Returns(DefaultWarehouse());
        _execution.ExecuteAsync(ProductId, DefaultWarehouseId, 20m, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new ProductionExecutionResult(ProductId, DefaultWarehouseId, 20m, 2, 12m, 240m));

        var svc = Build();
        var result = await svc.CompleteProductionOrderAsync(order.Id, Guid.NewGuid(), warehouseId: null, default);

        result.AlreadyCompleted.Should().BeFalse();
        result.WarehouseId.Should().Be(DefaultWarehouseId);
        result.ComponentsIssued.Should().Be(2);
        result.ProducedQuantity.Should().Be(20m);
        result.UnitCost.Should().Be(12m);
        order.Status.Should().Be(PlannedProductionOrderStatus.Closed);
        order.ProducedWarehouseId.Should().Be(DefaultWarehouseId);
        await _execution.Received(1).ExecuteAsync(
            ProductId, DefaultWarehouseId, 20m, Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Completing_already_closed_order_is_idempotent_noop()
    {
        var order = NewOrder();
        order.Release();
        order.Complete(DefaultWarehouseId);
        _productionOrders.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var svc = Build();
        var result = await svc.CompleteProductionOrderAsync(order.Id, Guid.NewGuid(), warehouseId: null, default);

        result.AlreadyCompleted.Should().BeTrue();
        order.Status.Should().Be(PlannedProductionOrderStatus.Closed);
        await _execution.DidNotReceiveWithAnyArgs().ExecuteAsync(
            default, default, default, default, default);
    }

    [Fact]
    public async Task Completing_non_released_order_is_rejected_and_no_stock_moved()
    {
        var order = NewOrder(); // Planned
        _productionOrders.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        _warehouses.GetDefaultAsync(Arg.Any<CancellationToken>()).Returns(DefaultWarehouse());

        var svc = Build();
        var act = () => svc.CompleteProductionOrderAsync(order.Id, Guid.NewGuid(), warehouseId: null, default);

        await act.Should().ThrowAsync<InvalidPlannedProductionOrderTransitionException>();
        order.Status.Should().Be(PlannedProductionOrderStatus.Planned);
    }

    [Fact]
    public async Task Completing_missing_order_throws_not_found()
    {
        _productionOrders.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((PlannedProductionOrder?)null);

        var svc = Build();
        var act = () => svc.CompleteProductionOrderAsync(Guid.NewGuid(), Guid.NewGuid(), warehouseId: null, default);

        await act.Should().ThrowAsync<PlannedProductionOrderNotFoundException>();
    }
}
