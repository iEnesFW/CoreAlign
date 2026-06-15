using CoreAlign.Application.Inventory.Services;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Inventory;

public class ProductionExecutionServiceTests
{
    private readonly IAllocationService _allocation = Substitute.For<IAllocationService>();
    private readonly IProductComponentRepository _components = Substitute.For<IProductComponentRepository>();
    private readonly ProductionExecutionService _sut;

    private static readonly Guid ParentId = Guid.NewGuid();
    private static readonly Guid CompAId = Guid.NewGuid();
    private static readonly Guid CompBId = Guid.NewGuid();
    private static readonly Guid WarehouseId = Guid.NewGuid();

    public ProductionExecutionServiceTests()
    {
        _sut = new ProductionExecutionService(_allocation, _components);
    }

    private static StockMovement Movement(Guid productId, decimal qty, decimal unitCost) =>
        new(productId, WarehouseId, StockMovementType.Issue, qty, unitCost, 0m, unitCost,
            DateTime.UtcNow, StockSourceDocumentType.Production);

    [Fact]
    public async Task Executing_consumes_components_times_quantity_and_receipts_parent_at_rolled_up_cost()
    {
        _components.GetByParentAsync(ParentId, Arg.Any<CancellationToken>()).Returns(new List<ProductComponent>
        {
            new(ParentId, CompAId, 2m),
            new(ParentId, CompBId, 3m),
        });
        _allocation.ApplyIssueAsync(Arg.Any<StockIssueRequest>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var r = ci.Arg<StockIssueRequest>();
                return Movement(r.ProductId, r.Quantity, 10m);
            });
        _allocation.ApplyReceiptAsync(Arg.Any<StockReceiptRequest>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var r = ci.Arg<StockReceiptRequest>();
                return Movement(r.ProductId, r.Quantity, r.UnitCost);
            });

        var result = await _sut.ExecuteAsync(ParentId, WarehouseId, 5m, null, default);

        await _allocation.Received(1).ApplyIssueAsync(
            Arg.Is<StockIssueRequest>(r => r.ProductId == CompAId && r.Quantity == 10m), Arg.Any<CancellationToken>());
        await _allocation.Received(1).ApplyIssueAsync(
            Arg.Is<StockIssueRequest>(r => r.ProductId == CompBId && r.Quantity == 15m), Arg.Any<CancellationToken>());
        // Rolled-up unit cost: (2*10) + (3*10) = 50; receipt of parent at 50, qty 5.
        await _allocation.Received(1).ApplyReceiptAsync(
            Arg.Is<StockReceiptRequest>(r => r.ProductId == ParentId && r.Quantity == 5m && r.UnitCost == 50m),
            Arg.Any<CancellationToken>());

        result.ComponentsIssued.Should().Be(2);
        result.ProducedQuantity.Should().Be(5m);
        result.UnitCost.Should().Be(50m);
        result.TotalCost.Should().Be(250m);
    }

    [Fact]
    public async Task Executing_without_a_formula_throws()
    {
        _components.GetByParentAsync(ParentId, Arg.Any<CancellationToken>())
            .Returns(new List<ProductComponent>());

        var act = () => _sut.ExecuteAsync(ParentId, WarehouseId, 5m, null, default);

        await act.Should().ThrowAsync<StockMovementValidationException>();
        await _allocation.DidNotReceive().ApplyReceiptAsync(Arg.Any<StockReceiptRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Executing_with_non_positive_quantity_throws()
    {
        var act = () => _sut.ExecuteAsync(ParentId, WarehouseId, 0m, null, default);

        await act.Should().ThrowAsync<StockMovementValidationException>();
    }
}
