using CoreAlign.Application.B2B;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Application.Inventory.StockCounts;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Inventory;

/// <summary>
/// Rule 16 idempotency-on-retry: re-running the same StockCount.Post must NOT
/// double-adjust stock. The Post is guarded by Status == Reconciliation; the first
/// call moves the count to Posted, so a retry throws and emits no second
/// adjustment. These tests prove the guard holds and stock is adjusted exactly once.
/// </summary>
public class StockCountPostIdempotencyTests
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

    public StockCountPostIdempotencyTests()
    {
        _user.UserId.Returns(Guid.NewGuid());
        _reasons.ListAsync(StockReasonCategory.CycleCount, isActive: true, Arg.Any<CancellationToken>())
            .Returns(new List<StockReasonCode>
            {
                new("CC", "Cycle Count", StockReasonCategory.CycleCount) { Id = Guid.NewGuid() },
            });
        _allocation.AdjustAsync(Arg.Any<StockAdjustmentRequest>(), Arg.Any<CancellationToken>())
            .Returns(ci => new StockMovement(
                Guid.NewGuid(), WarehouseId, StockMovementType.AdjustmentNegative,
                3m, 5m, 7m, 5m, DateTime.UtcNow, StockSourceDocumentType.CycleCount));
        _sut = new PostStockCountHandler(_counts, _allocation, _stockItems, _reasons, _outbox, _user, _uow);
    }

    private StockCount BuildReconciled(decimal expected, decimal counted, decimal unitCost)
    {
        var c = new StockCount("SC-1", WarehouseId, "WH1", "Main", DateTime.UtcNow)
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
        };
        var line = new StockCountLine(Guid.NewGuid(), "SKU", "Widget", expected, unitCost);
        c.ReplaceLines(new[] { line });
        c.BeginCounting();
        c.RecordLineCount(line.Id, counted, null, null);
        c.Reconcile(null);

        // Live on-hand still equals the snapshot (no movement window), so the
        // reconcile-to-counted variance is (counted − expected) = −3.
        var liveItem = new StockItem(line.ProductId, WarehouseId) { TenantId = TenantId };
        liveItem.SeedOpeningBalance(expected, unitCost, DateTime.UtcNow);
        _stockItems.GetAsync(line.ProductId, WarehouseId, line.LotId, Arg.Any<CancellationToken>())
            .Returns(liveItem);
        return c;
    }

    [Fact]
    public async Task Second_post_of_same_count_throws_and_does_not_double_adjust()
    {
        var c = BuildReconciled(10m, 7m, 5m);
        _counts.GetWithLinesAsync(c.Id, Arg.Any<CancellationToken>()).Returns(c);

        await _sut.Handle(new PostStockCountCommand(c.Id), default);
        c.Status.Should().Be(StockCountStatus.Posted);

        Func<Task> retry = () => _sut.Handle(new PostStockCountCommand(c.Id), default);
        await retry.Should().ThrowAsync<InvalidStockCountStateException>();

        // Exactly one adjustment across both calls — the retry never reached AdjustAsync.
        await _allocation.Received(1).AdjustAsync(Arg.Any<StockAdjustmentRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Second_post_enqueues_no_additional_gl_adjustment()
    {
        var c = BuildReconciled(10m, 7m, 5m);
        _counts.GetWithLinesAsync(c.Id, Arg.Any<CancellationToken>()).Returns(c);

        await _sut.Handle(new PostStockCountCommand(c.Id), default);

        Func<Task> retry = () => _sut.Handle(new PostStockCountCommand(c.Id), default);
        await retry.Should().ThrowAsync<InvalidStockCountStateException>();

        await _outbox.Received(1).EnqueueAsync(
            Arg.Any<CoreAlign.Application.Accounting.Services.GLPostingRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Concurrent_double_dispatch_on_same_loaded_entity_adjusts_once()
    {
        // Both calls observe the SAME reconciled aggregate (single loaded instance,
        // as a network retry racing the original would). The state machine collapses
        // the second into a no-op exception so stock moves exactly once.
        var c = BuildReconciled(10m, 7m, 5m);
        _counts.GetWithLinesAsync(c.Id, Arg.Any<CancellationToken>()).Returns(c);

        await _sut.Handle(new PostStockCountCommand(c.Id), default);
        var second = await Record.ExceptionAsync(() => _sut.Handle(new PostStockCountCommand(c.Id), default));

        second.Should().BeOfType<InvalidStockCountStateException>();
        await _allocation.Received(1).AdjustAsync(Arg.Any<StockAdjustmentRequest>(), Arg.Any<CancellationToken>());
        c.PostedAtUtc.Should().NotBeNull();
    }
}
