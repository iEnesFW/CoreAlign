using CoreAlign.Application.B2B;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Application.Inventory.StockCounts;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Inventory;

public class PostStockCountHandlerTests
{
    private readonly IStockCountRepository _counts = Substitute.For<IStockCountRepository>();
    private readonly IAllocationService _allocation = Substitute.For<IAllocationService>();
    private readonly IStockItemRepository _stockItems = Substitute.For<IStockItemRepository>();
    private readonly IStockReasonCodeRepository _reasons = Substitute.For<IStockReasonCodeRepository>();
    private readonly IGLPostingOutbox _outbox = Substitute.For<IGLPostingOutbox>();
    private readonly ICurrentUserAccessor _user = Substitute.For<ICurrentUserAccessor>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly PostStockCountHandler _sut;

    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid WarehouseId = Guid.NewGuid();

    public PostStockCountHandlerTests()
    {
        _user.UserId.Returns(Guid.NewGuid());
        _reasons.ListAsync(StockReasonCategory.CycleCount, isActive: true, Arg.Any<CancellationToken>())
            .Returns(new List<StockReasonCode> { new("CC", "Cycle Count", StockReasonCategory.CycleCount) { Id = Guid.NewGuid() } });
        _sut = new PostStockCountHandler(_counts, _allocation, _stockItems, _reasons, _outbox, _user, _uow);
    }

    private StockCount BuildReconciled(decimal expected, decimal counted, decimal unitCost)
    {
        var c = new StockCount("SC-1", WarehouseId, "WH1", "Main", DateTime.UtcNow) { TenantId = TenantId };
        var line = new StockCountLine(Guid.NewGuid(), "SKU", "Widget", expected, unitCost);
        c.ReplaceLines(new[] { line });
        c.BeginCounting();
        c.RecordLineCount(line.Id, counted, null, null);
        c.Reconcile(null);

        // No movement inside the count window: the live warehouse on-hand still
        // equals the snapshot expected, so reconcile-to-counted reproduces the
        // classic (counted − expected) variance the assertions encode. The handler
        // batch-loads live on-hand via GetOnHandByProductLotAsync (keyed by product+lot).
        _stockItems.GetOnHandByProductLotAsync(WarehouseId, Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<(Guid ProductId, Guid? LotId), decimal>
            {
                { (line.ProductId, line.LotId), expected },
            });
        return c;
    }

    [Fact]
    public async Task Post_fails_when_not_in_reconciliation()
    {
        var c = new StockCount("SC-1", WarehouseId, "WH1", "Main", DateTime.UtcNow) { Id = Guid.NewGuid() };
        _counts.GetWithLinesAsync(c.Id, Arg.Any<CancellationToken>()).Returns(c);

        Func<Task> act = () => _sut.Handle(new PostStockCountCommand(c.Id), default);
        await act.Should().ThrowAsync<InvalidStockCountStateException>();
    }

    [Fact]
    public async Task Post_creates_adjustment_per_variance_line()
    {
        var c = BuildReconciled(10m, 7m, 5m);
        c.Id = Guid.NewGuid();
        _counts.GetWithLinesAsync(c.Id, Arg.Any<CancellationToken>()).Returns(c);
        _allocation.AdjustAsync(Arg.Any<StockAdjustmentRequest>(), Arg.Any<CancellationToken>())
            .Returns(ci => new StockMovement(
                c.Lines.First().ProductId, WarehouseId,
                StockMovementType.AdjustmentNegative, 3m, 5m, 7m, 5m,
                DateTime.UtcNow, StockSourceDocumentType.CycleCount));

        await _sut.Handle(new PostStockCountCommand(c.Id), default);

        await _allocation.Received(1).AdjustAsync(
            Arg.Is<StockAdjustmentRequest>(r => r.Delta == -3m && r.SourceDocumentType == StockSourceDocumentType.CycleCount),
            Arg.Any<CancellationToken>());
        c.Status.Should().Be(StockCountStatus.Posted);
        c.PostedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task Post_skips_lines_with_zero_variance()
    {
        var c = BuildReconciled(10m, 10m, 5m);
        c.Id = Guid.NewGuid();
        _counts.GetWithLinesAsync(c.Id, Arg.Any<CancellationToken>()).Returns(c);

        await _sut.Handle(new PostStockCountCommand(c.Id), default);

        await _allocation.DidNotReceiveWithAnyArgs().AdjustAsync(default!, default);
        await _outbox.DidNotReceiveWithAnyArgs().EnqueueAsync(default!, default);
    }

    [Fact]
    public async Task Post_enqueues_cogs_adjustment_for_negative_net_variance()
    {
        var c = BuildReconciled(10m, 7m, 5m);
        c.Id = Guid.NewGuid();
        _counts.GetWithLinesAsync(c.Id, Arg.Any<CancellationToken>()).Returns(c);
        _allocation.AdjustAsync(Arg.Any<StockAdjustmentRequest>(), Arg.Any<CancellationToken>())
            .Returns(ci => new StockMovement(
                c.Lines.First().ProductId, WarehouseId,
                StockMovementType.AdjustmentNegative, 3m, 5m, 7m, 5m,
                DateTime.UtcNow, StockSourceDocumentType.CycleCount));

        await _sut.Handle(new PostStockCountCommand(c.Id), default);

        await _outbox.Received(1).EnqueueAsync(
            Arg.Is<CoreAlign.Application.Accounting.Services.GLPostingRequest>(r =>
                r.SourceType == JournalSourceType.InventoryScrap),
            Arg.Any<CancellationToken>());
    }
}
