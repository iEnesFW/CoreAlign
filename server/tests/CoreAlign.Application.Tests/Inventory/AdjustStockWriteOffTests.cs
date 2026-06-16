using CoreAlign.Application.Accounting.Services;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Application.Inventory.Commands;
using CoreAlign.Application.Inventory.Handlers;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Inventory;

public class AdjustStockWriteOffTests
{
    private readonly IAllocationService _allocation = Substitute.For<IAllocationService>();
    private readonly IStockReasonCodeRepository _reasons = Substitute.For<IStockReasonCodeRepository>();
    private readonly IGLPostingOutbox _outbox = Substitute.For<IGLPostingOutbox>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly AdjustStockHandler _sut;

    private static readonly Guid ProductId = Guid.NewGuid();
    private static readonly Guid WarehouseId = Guid.NewGuid();

    public AdjustStockWriteOffTests()
    {
        _sut = new AdjustStockHandler(_allocation, _reasons, _outbox, _uow);
    }

    private StockMovement ArrangeAdjust(decimal delta, decimal unitCost)
    {
        var movement = new StockMovement(
            ProductId, WarehouseId,
            delta < 0m ? StockMovementType.AdjustmentNegative : StockMovementType.AdjustmentPositive,
            Math.Abs(delta), unitCost, 0m, unitCost, DateTime.UtcNow, StockSourceDocumentType.Adjustment);
        _allocation.AdjustAsync(Arg.Any<StockAdjustmentRequest>(), Arg.Any<CancellationToken>()).Returns(movement);
        return movement;
    }

    private void ArrangeReason(Guid id, StockReasonCategory category, bool affectsCost = true) =>
        _reasons.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(new StockReasonCode("WO", "Write-off", category, affectsCost) { Id = id });

    [Theory]
    [InlineData(StockReasonCategory.DamageWriteOff)]
    [InlineData(StockReasonCategory.Expired)]
    [InlineData(StockReasonCategory.Loss)]
    public async Task Negative_writeoff_reason_enqueues_689_against_153(StockReasonCategory category)
    {
        var reasonId = Guid.NewGuid();
        ArrangeReason(reasonId, category);
        var movement = ArrangeAdjust(delta: -4m, unitCost: 25m);

        await _sut.Handle(new AdjustStockCommand(ProductId, WarehouseId, -4m, 25m, reasonId, null, "imha"), default);

        await _outbox.Received(1).EnqueueAsync(
            Arg.Is<GLPostingRequest>(r =>
                r.SourceType == JournalSourceType.InventoryWriteOff &&
                r.SourceDocumentId == movement.Id &&
                r.Lines.Any(l => l.Key == GLPostingKey.InventoryWriteOff && l.Debit == 100m) &&
                r.Lines.Any(l => l.Key == GLPostingKey.Inventory && l.Credit == 100m)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Positive_writeoff_reason_flips_legs_to_dr153_cr689()
    {
        var reasonId = Guid.NewGuid();
        ArrangeReason(reasonId, StockReasonCategory.DamageWriteOff);
        ArrangeAdjust(delta: 2m, unitCost: 10m);

        await _sut.Handle(new AdjustStockCommand(ProductId, WarehouseId, 2m, 10m, reasonId, null, null), default);

        await _outbox.Received(1).EnqueueAsync(
            Arg.Is<GLPostingRequest>(r =>
                r.Lines.Any(l => l.Key == GLPostingKey.Inventory && l.Debit == 20m) &&
                r.Lines.Any(l => l.Key == GLPostingKey.InventoryWriteOff && l.Credit == 20m)),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(StockReasonCategory.Adjustment)]
    [InlineData(StockReasonCategory.Found)]
    [InlineData(StockReasonCategory.CycleCount)]
    [InlineData(StockReasonCategory.Receipt)]
    public async Task Routine_reason_does_not_enqueue_any_gl(StockReasonCategory category)
    {
        var reasonId = Guid.NewGuid();
        ArrangeReason(reasonId, category);
        ArrangeAdjust(delta: -4m, unitCost: 25m);

        await _sut.Handle(new AdjustStockCommand(ProductId, WarehouseId, -4m, 25m, reasonId, null, null), default);

        await _outbox.DidNotReceiveWithAnyArgs().EnqueueAsync(default!, default);
    }

    [Fact]
    public async Task Writeoff_reason_that_does_not_affect_cost_skips_gl()
    {
        var reasonId = Guid.NewGuid();
        ArrangeReason(reasonId, StockReasonCategory.DamageWriteOff, affectsCost: false);
        ArrangeAdjust(delta: -4m, unitCost: 25m);

        await _sut.Handle(new AdjustStockCommand(ProductId, WarehouseId, -4m, 25m, reasonId, null, null), default);

        await _outbox.DidNotReceiveWithAnyArgs().EnqueueAsync(default!, default);
    }

    [Fact]
    public async Task No_reason_skips_gl()
    {
        ArrangeAdjust(delta: -4m, unitCost: 25m);

        await _sut.Handle(new AdjustStockCommand(ProductId, WarehouseId, -4m, 25m, null, null, null), default);

        await _outbox.DidNotReceiveWithAnyArgs().EnqueueAsync(default!, default);
    }
}
