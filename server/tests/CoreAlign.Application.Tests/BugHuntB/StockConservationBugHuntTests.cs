using CoreAlign.Application.B2B;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Application.Inventory.Commands;
using CoreAlign.Application.Inventory.Handlers;
using CoreAlign.Application.Inventory.Services;
using CoreAlign.Application.Inventory.StockCounts;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Services;

namespace CoreAlign.Application.Tests.BugHuntB;

/// <summary>
/// HUNTER B — STOCK CONSERVATION. These tests are RED on current code and prove
/// real stock-ledger correctness defects. They are intentionally adversarial and
/// document concrete failing scenarios. Do NOT "fix" by relaxing the assertions —
/// the assertions encode the conserved-stock invariant (rule 16).
/// </summary>
public class StockConservationBugHuntTests
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

    private static StockItem StockWith(decimal onHand, decimal avgCost = 5m)
    {
        var item = new StockItem(ProductId, WarehouseId) { Id = Guid.NewGuid(), TenantId = TenantId };
        item.SeedOpeningBalance(onHand, avgCost, DateTime.UtcNow);
        return item;
    }

    private static Product ProductWith(decimal stockQuantity)
    {
        var p = new Product("SKU-A", "Widget", "pcs", 10m, "TRY", initialStock: stockQuantity)
        {
            Id = ProductId,
            TenantId = TenantId,
        };
        return p;
    }

    // ------------------------------------------------------------------
    // B-1  HIGH — Cycle-count Post applies the SNAPSHOT-time variance delta
    // to the CURRENT StockItem.OnHand. If stock moves between Plan and Post,
    // the warehouse balance ends at (currentOnHand + (counted - snapshotOnHand))
    // instead of the physically-counted quantity. Money/stock corruption: the
    // physical count is the source of truth and must win.
    // ------------------------------------------------------------------
    [Fact]
    public async Task B1_CycleCountPost_appliesStaleVarianceDelta_warehouseDoesNotEqualCountedQuantity()
    {
        // Plan snapshot: OnHand was 10 when the count sheet was generated.
        const decimal snapshotOnHand = 10m;
        // Physical count found 12 units on the shelf.
        const decimal countedQuantity = 12m;
        // BUT between Plan and Post, an issue of 5 happened (OnHand is now 5).
        const decimal currentOnHand = 5m;

        var line = new StockCountLine(ProductId, "SKU-A", "Widget", snapshotOnHand, snapshotUnitCost: 5m);
        var count = new StockCount("SC-1", WarehouseId, "WH1", "Main", DateTime.UtcNow) { Id = Guid.NewGuid(), TenantId = TenantId };
        count.ReplaceLines(new[] { line });
        count.BeginCounting();
        count.RecordLineCount(line.Id, countedQuantity, null, null); // Variance = 12 - 10 = +2
        count.Reconcile(null);

        var liveItem = StockWith(currentOnHand); // real warehouse balance at Post time

        var counts = Substitute.For<IStockCountRepository>();
        counts.GetWithLinesAsync(count.Id, Arg.Any<CancellationToken>()).Returns(count);
        var reasons = Substitute.For<IStockReasonCodeRepository>();
        reasons.ListAsync(StockReasonCategory.CycleCount, isActive: true, Arg.Any<CancellationToken>())
            .Returns(new List<StockReasonCode> { new("CC", "Cycle Count", StockReasonCategory.CycleCount) { Id = Guid.NewGuid() } });
        var outbox = Substitute.For<IGLPostingOutbox>();
        var user = Substitute.For<ICurrentUserAccessor>();
        user.UserId.Returns(Guid.NewGuid());

        // Real AllocationService applies the delta to the CURRENT live item, and the
        // handler re-reads the live on-hand to reconcile to the counted absolute.
        _stockItems.GetOnHandByProductLotAsync(WarehouseId, Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<(Guid ProductId, Guid? LotId), decimal> { { (ProductId, line.LotId), currentOnHand } });
        _stockItems.GetOrCreateAsync(ProductId, WarehouseId, line.LotId, Arg.Any<CancellationToken>()).Returns(liveItem);

        var sut = new PostStockCountHandler(counts, BuildService(), _stockItems, reasons, outbox, user, _uow);

        await sut.Handle(new PostStockCountCommand(count.Id), default);

        // A physical count is authoritative: after posting, the warehouse on-hand
        // MUST equal what was physically counted (12). On current code it lands at
        // currentOnHand + variance = 5 + 2 = 7, silently corrupting the balance.
        liveItem.OnHand.Should().Be(countedQuantity,
            "a posted cycle count must reconcile warehouse on-hand to the physically-counted quantity, not blindly add a stale snapshot-time delta");
    }

    // ------------------------------------------------------------------
    // B-2  HIGH — Product.StockQuantity vs StockItem.OnHand dual-ledger
    // divergence. Standalone Receive (and Issue/Adjust/Produce/StockCount.Post)
    // go through AllocationService which mutates ONLY StockItem.OnHand and never
    // Product.StockQuantity. Order confirmation later guards availability against
    // the STALE Product.StockQuantity (OrderConfirmedStockHandler line 94) and
    // also DECREMENTS it again — the two ledgers permanently disagree.
    // ------------------------------------------------------------------
    [Fact]
    public async Task B2_StandaloneReceive_updatesStockItem_butLeavesProductStockQuantityStale()
    {
        var product = ProductWith(stockQuantity: 0m);
        var item = new StockItem(ProductId, WarehouseId) { Id = Guid.NewGuid(), TenantId = TenantId };

        _stockItems.GetOrCreateAsync(ProductId, WarehouseId, null, Arg.Any<CancellationToken>()).Returns(item);
        _products.GetByIdAsync(ProductId, Arg.Any<CancellationToken>()).Returns(product);

        var handler = new ReceiveStockHandler(BuildService(), _uow);

        // Receive 100 units into the warehouse.
        await handler.Handle(new ReceiveStockCommand(
            ProductId, WarehouseId, Quantity: 100m, UnitCost: 5m,
            LotId: null, SerialNumber: null, ReasonCodeId: null, Reference: "PO-1", Notes: null), default);

        // StockItem ledger correctly reflects the receipt.
        item.OnHand.Should().Be(100m);

        // The two stock ledgers MUST agree (stock conserved across both representations).
        // On current code Product.StockQuantity stays 0 — divergence that downstream
        // availability checks (OrderConfirmedStockHandler) rely on, causing false
        // "InsufficientStock" rejections OR phantom over-selling.
        product.StockQuantity.Should().Be(100m,
            "a warehouse receipt that increases StockItem.OnHand must also raise Product.StockQuantity, otherwise the order-confirm availability guard reads a stale balance");
    }

    // ------------------------------------------------------------------
    // B-3  MEDIUM — Same divergence, reverse direction. A standalone Issue
    // through the Stock API removes units from StockItem.OnHand but leaves
    // Product.StockQuantity untouched, so the order-confirm guard believes the
    // units are still sellable -> phantom over-sell / negative real stock.
    // ------------------------------------------------------------------
    [Fact]
    public async Task B3_StandaloneIssue_drainsStockItem_butProductStockQuantityStillShowsStock()
    {
        var product = ProductWith(stockQuantity: 50m);
        var item = StockWith(onHand: 50m);

        _stockItems.GetAsync(ProductId, WarehouseId, null, Arg.Any<CancellationToken>()).Returns(item);
        _products.GetByIdAsync(ProductId, Arg.Any<CancellationToken>()).Returns(product);

        var handler = new IssueStockHandler(BuildService(), _uow);

        // Issue all 50 units out (e.g. scrap / manual consumption).
        await handler.Handle(new IssueStockCommand(
            ProductId, WarehouseId, Quantity: 50m,
            LotId: null, SerialNumber: null, ReasonCodeId: null, Reference: "ISS-1", Notes: null), default);

        item.OnHand.Should().Be(0m);

        // Ledgers must stay in lockstep: Product.StockQuantity must also drop to 0,
        // else OrderConfirmedStockHandler will happily confirm an order for 50 units
        // that no longer physically exist.
        product.StockQuantity.Should().Be(0m,
            "issuing stock out of the warehouse must also decrement Product.StockQuantity so availability checks cannot over-sell");
    }
}
