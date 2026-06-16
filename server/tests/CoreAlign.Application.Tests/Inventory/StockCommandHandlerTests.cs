using CoreAlign.Application.Inventory.Commands;
using CoreAlign.Application.Inventory.Handlers;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Inventory;

public class StockCommandHandlerTests
{
    private readonly IAllocationService _allocation = Substitute.For<IAllocationService>();
    private readonly IStockReasonCodeRepository _reasons = Substitute.For<IStockReasonCodeRepository>();
    private readonly CoreAlign.Application.Common.Outbox.IGLPostingOutbox _outbox = Substitute.For<CoreAlign.Application.Common.Outbox.IGLPostingOutbox>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private static readonly Guid ProductId = Guid.NewGuid();
    private static readonly Guid WarehouseId = Guid.NewGuid();

    private static StockMovement Stub(StockMovementType type, decimal qty) =>
        new(ProductId, WarehouseId, type, qty, 5m, 100m, 5m, DateTime.UtcNow, StockSourceDocumentType.Adjustment);

    [Fact]
    public async Task AdjustStock_calls_allocator_with_request_payload_and_saves()
    {
        _allocation.AdjustAsync(Arg.Any<StockAdjustmentRequest>(), Arg.Any<CancellationToken>())
            .Returns(Stub(StockMovementType.AdjustmentPositive, 5m));
        var sut = new AdjustStockHandler(_allocation, _reasons, _outbox, _uow);

        await sut.Handle(new AdjustStockCommand(ProductId, WarehouseId, 5m, 10m, null, null, "test"), default);

        await _allocation.Received(1).AdjustAsync(
            Arg.Is<StockAdjustmentRequest>(r =>
                r.ProductId == ProductId &&
                r.WarehouseId == WarehouseId &&
                r.Delta == 5m &&
                r.UnitCost == 10m &&
                r.SourceDocumentType == StockSourceDocumentType.Adjustment),
            Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReceiveStock_calls_allocator_with_positive_quantity_and_unit_cost()
    {
        _allocation.ApplyReceiptAsync(Arg.Any<StockReceiptRequest>(), Arg.Any<CancellationToken>())
            .Returns(Stub(StockMovementType.Receipt, 10m));
        var sut = new ReceiveStockHandler(_allocation, _uow);

        await sut.Handle(new ReceiveStockCommand(
            ProductId, WarehouseId, 10m, 4.5m, null, null, null, "PO-1", "note"), default);

        await _allocation.Received(1).ApplyReceiptAsync(
            Arg.Is<StockReceiptRequest>(r =>
                r.ProductId == ProductId &&
                r.Quantity == 10m &&
                r.UnitCost == 4.5m &&
                r.SourceReference == "PO-1"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IssueStock_calls_allocator_and_persists_movement()
    {
        _allocation.ApplyIssueAsync(Arg.Any<StockIssueRequest>(), Arg.Any<CancellationToken>())
            .Returns(Stub(StockMovementType.Issue, 7m));
        var sut = new IssueStockHandler(_allocation, _uow);

        var dto = await sut.Handle(new IssueStockCommand(
            ProductId, WarehouseId, 7m, null, null, null, "SO-1", null), default);

        dto.Quantity.Should().Be(7m);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateLot_inserts_new_lot_with_metadata_and_saves()
    {
        var lots = Substitute.For<ILotRepository>();
        var sut = new CreateLotHandler(lots, _uow);

        await sut.Handle(new CreateLotCommand(
            ProductId, "LOT-123", new DateTime(2026, 1, 1), new DateTime(2027, 1, 1), "REF", "TR", "n"), default);

        await lots.Received(1).AddAsync(
            Arg.Is<Lot>(l => l.LotNumber == "LOT-123" && l.ProductId == ProductId),
            Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateLot_blocks_or_unblocks_based_on_flag()
    {
        var lots = Substitute.For<ILotRepository>();
        var lot = new Lot(ProductId, "L1", null, null, null) { Id = Guid.NewGuid() };
        lots.GetByIdAsync(lot.Id, Arg.Any<CancellationToken>()).Returns(lot);
        var sut = new UpdateLotHandler(lots, _uow);

        await sut.Handle(new UpdateLotCommand(
            lot.Id, "L1", null, null, null, null, null, IsBlocked: true, BlockReason: "QC fail"), default);

        lot.IsBlocked.Should().BeTrue();

        await sut.Handle(new UpdateLotCommand(
            lot.Id, "L1", null, null, null, null, null, IsBlocked: false, BlockReason: null), default);

        lot.IsBlocked.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateLot_throws_when_lot_missing()
    {
        var lots = Substitute.For<ILotRepository>();
        lots.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Lot?)null);
        var sut = new UpdateLotHandler(lots, _uow);

        Func<Task> act = () => sut.Handle(new UpdateLotCommand(
            Guid.NewGuid(), "L", null, null, null, null, null, false, null), default);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task CreateStockReasonCode_inserts_entity()
    {
        var repo = Substitute.For<IStockReasonCodeRepository>();
        var sut = new CreateStockReasonCodeHandler(repo, _uow);

        await sut.Handle(new CreateStockReasonCodeCommand(
            "DMG", "Damaged", StockReasonCategory.Adjustment, true, null), default);

        await repo.Received(1).AddAsync(
            Arg.Is<StockReasonCode>(r => r.Code == "DMG" && r.Category == StockReasonCategory.Adjustment),
            Arg.Any<CancellationToken>());
    }
}
