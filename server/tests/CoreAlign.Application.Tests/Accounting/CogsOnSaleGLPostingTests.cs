using CoreAlign.Application.Accounting.Services;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Application.Inventory.Services;
using CoreAlign.Application.Orders.EventHandlers;
using CoreAlign.Application.Returns.EventHandlers;
using CoreAlign.Application.Shipments.Commands;
using CoreAlign.Application.Shipments.Handlers;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Events;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Accounting;

/// <summary>
/// COGS-on-sale GL recognition. A sale issue relieves inventory at issue cost
/// (DR CostOfGoodsSold(621) / CR Inventory(153)); a return/cancel that receives
/// stock back reverses it. Accounts resolve through the standard mapping (621 /
/// 153) and the journal must balance. Idempotency is keyed off
/// (CostOfGoodsSold[Reversal], documentId) so replays cannot double-post.
/// </summary>
public class CogsOnSaleGLPostingTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid OrderId = Guid.NewGuid();
    private static readonly Guid ProductId = Guid.NewGuid();
    private static readonly Guid WarehouseId = Guid.NewGuid();

    private const decimal AvgCost = 7m;
    private const decimal Qty = 4m;
    private const decimal ExpectedCogs = Qty * AvgCost; // 28

    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly IProductComponentRepository _components = Substitute.For<IProductComponentRepository>();
    private readonly IStockTransactionRepository _stockTxns = Substitute.For<IStockTransactionRepository>();
    private readonly IWarehouseRepository _warehouses = Substitute.For<IWarehouseRepository>();
    private readonly IStockItemRepository _stockItems = Substitute.For<IStockItemRepository>();
    private readonly IStockMovementRepository _movements = Substitute.For<IStockMovementRepository>();
    private readonly IGLPostingOutbox _outbox = Substitute.For<IGLPostingOutbox>();
    private readonly IStockOpeningBalanceBridge _openingBalance = Substitute.For<IStockOpeningBalanceBridge>();

    public CogsOnSaleGLPostingTests()
    {
        _components.GetTreeForProductsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, IReadOnlyList<(Guid, decimal)>>());
        _warehouses.GetDefaultAsync(Arg.Any<CancellationToken>())
            .Returns(new Warehouse("WH-DEF", "Default", isDefault: true) { Id = WarehouseId, TenantId = TenantId });
        // Stock item carries a known AvgCost so TotalCost = qty * AvgCost.
        _stockItems.GetOrCreateAsync(ProductId, WarehouseId, Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                var item = new StockItem(ProductId, WarehouseId) { Id = Guid.NewGuid(), TenantId = TenantId };
                item.SeedOpeningBalance(100m, AvgCost, DateTime.UtcNow);
                return item;
            });
    }

    private OrderConfirmedStockHandler ConfirmHandler() => new(
        _products, _components, _stockTxns, _warehouses, _stockItems, _movements, _outbox, _openingBalance);

    private OrderCancelledStockHandler CancelHandler() => new(
        _products, _components, _stockTxns, _warehouses, _stockItems, _movements, _outbox);

    private static Product TrackedProduct() =>
        new("SKU-A", "Widget", "pcs", 10m, "TRY", initialStock: 100m) { Id = ProductId, TenantId = TenantId };

    private static GLPostingLine Line(GLPostingRequest r, GLPostingKey key) =>
        r.Lines.Single(l => l.Key == key);

    [Fact]
    public async Task Confirming_a_sale_posts_DR_cogs_CR_inventory_for_issue_cost_balanced()
    {
        _products.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, Product> { [ProductId] = TrackedProduct() });

        GLPostingRequest? captured = null;
        await _outbox.EnqueueAsync(Arg.Do<GLPostingRequest>(r => captured = r), Arg.Any<CancellationToken>());

        await ConfirmHandler().Handle(
            new OrderConfirmedEvent(TenantId, OrderId, "ORD-1",
                new[] { new OrderLineSnapshot(ProductId, Qty) }, DateTime.UtcNow),
            default);

        captured.Should().NotBeNull();
        captured!.SourceType.Should().Be(JournalSourceType.CostOfGoodsSold);
        captured.SourceDocumentId.Should().Be(OrderId);
        // DR 621 (CostOfGoodsSold), CR 153 (Inventory).
        Line(captured, GLPostingKey.CostOfGoodsSold).Debit.Should().Be(ExpectedCogs);
        Line(captured, GLPostingKey.CostOfGoodsSold).Credit.Should().Be(0m);
        Line(captured, GLPostingKey.Inventory).Credit.Should().Be(ExpectedCogs);
        Line(captured, GLPostingKey.Inventory).Debit.Should().Be(0m);
        // Balanced.
        captured.Lines.Sum(l => l.Debit).Should().Be(captured.Lines.Sum(l => l.Credit));
    }

    [Fact]
    public async Task Cancelling_a_sale_reverses_DR_inventory_CR_cogs()
    {
        _products.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, Product> { [ProductId] = TrackedProduct() });

        GLPostingRequest? captured = null;
        await _outbox.EnqueueAsync(Arg.Do<GLPostingRequest>(r => captured = r), Arg.Any<CancellationToken>());

        await CancelHandler().Handle(
            new OrderCancelledFromActiveEvent(TenantId, OrderId, "ORD-1",
                new[] { new OrderLineSnapshot(ProductId, Qty) }, DateTime.UtcNow),
            default);

        captured.Should().NotBeNull();
        captured!.SourceType.Should().Be(JournalSourceType.CostOfGoodsSoldReversal);
        captured.SourceDocumentId.Should().Be(OrderId);
        // Reverse: DR 153 (Inventory), CR 621 (CostOfGoodsSold).
        Line(captured, GLPostingKey.Inventory).Debit.Should().Be(ExpectedCogs);
        Line(captured, GLPostingKey.CostOfGoodsSold).Credit.Should().Be(ExpectedCogs);
        captured.Lines.Sum(l => l.Debit).Should().Be(captured.Lines.Sum(l => l.Credit));
    }

    [Fact]
    public async Task Service_only_sale_posts_no_cogs()
    {
        _products.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, Product>());

        // A service-only confirm emits an event with no stock lines.
        await ConfirmHandler().Handle(
            new OrderConfirmedEvent(TenantId, OrderId, "ORD-1",
                Array.Empty<OrderLineSnapshot>(), DateTime.UtcNow),
            default);

        await _outbox.DidNotReceiveWithAnyArgs().EnqueueAsync(default!, default);
    }

    [Fact]
    public async Task Return_receipt_posts_reverse_cogs_DR_inventory_CR_cogs()
    {
        var returnId = Guid.NewGuid();
        _products.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, Product> { [ProductId] = TrackedProduct() });
        _stockItems.GetOrCreateAsync(ProductId, WarehouseId, Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(new StockItem(ProductId, WarehouseId) { Id = Guid.NewGuid(), TenantId = TenantId });

        GLPostingRequest? captured = null;
        await _outbox.EnqueueAsync(Arg.Do<GLPostingRequest>(r => captured = r), Arg.Any<CancellationToken>());

        var sut = new ReturnRequestReceivedStockHandler(_products, _stockItems, _movements, _stockTxns, _outbox);
        await sut.Handle(
            new ReturnRequestReceivedEvent(TenantId, returnId, "RMA-1", OrderId, Guid.NewGuid(), WarehouseId,
                new[] { new ReturnRequestLineSnapshot(Guid.NewGuid(), ProductId, Qty, 10m, AvgCost) },
                DateTime.UtcNow),
            default);

        captured.Should().NotBeNull();
        captured!.SourceType.Should().Be(JournalSourceType.CostOfGoodsSoldReversal);
        captured.SourceDocumentId.Should().Be(returnId);
        Line(captured, GLPostingKey.Inventory).Debit.Should().Be(ExpectedCogs);
        Line(captured, GLPostingKey.CostOfGoodsSold).Credit.Should().Be(ExpectedCogs);
        captured.Lines.Sum(l => l.Debit).Should().Be(captured.Lines.Sum(l => l.Credit));
    }
}

/// <summary>
/// Proves the COGS journal is idempotent end-to-end through the real
/// <see cref="GLPostingService"/>: a replay of the same (CostOfGoodsSold,
/// documentId) source posts the journal exactly once.
/// </summary>
public class CogsGLPostingIdempotencyTests
{
    private readonly IJournalEntryRepository _journals = Substitute.For<IJournalEntryRepository>();
    private readonly IGLAccountRepository _accounts = Substitute.For<IGLAccountRepository>();
    private readonly IGLPostingMappingRepository _mappings = Substitute.For<IGLPostingMappingRepository>();
    private readonly IDocumentSequenceRepository _sequences = Substitute.For<IDocumentSequenceRepository>();
    private readonly IAccountingPeriodRepository _periods = Substitute.For<IAccountingPeriodRepository>();
    private readonly List<GLAccount> _chart = new();
    private readonly GLPostingService _sut;

    private static readonly Guid OrderId = Guid.NewGuid();

    public CogsGLPostingIdempotencyTests()
    {
        _sequences.GetAsync(DocumentSequenceType.JournalNumber, Arg.Any<CancellationToken>())
            .Returns(new DocumentSequence(DocumentSequenceType.JournalNumber, "YEV", 2026, 1, 5));
        _accounts.GetAllAsync(Arg.Any<CancellationToken>()).Returns(_chart);
        _mappings.ListAsync(Arg.Any<CancellationToken>()).Returns(new List<GLPostingMapping>());
        _chart.Add(new GLAccount("621", "Satılan Malın Maliyeti", AccountType.Expense, isPostable: true));
        _chart.Add(new GLAccount("153", "Ticari Mallar", AccountType.Asset, isPostable: true));
        _sut = new GLPostingService(_journals, _accounts, _mappings, _sequences, _periods);
    }

    private static GLPostingRequest CogsRequest() => new(
        JournalSourceType.CostOfGoodsSold,
        OrderId,
        "ORD-1",
        DateTime.UtcNow.Date,
        JournalEntryType.Mahsup,
        "Satış maliyeti (ORD-1)",
        new[]
        {
            new GLPostingLine(GLPostingKey.CostOfGoodsSold, 28m, 0m),
            new GLPostingLine(GLPostingKey.Inventory, 0m, 28m),
        });

    [Fact]
    public async Task First_post_writes_a_balanced_cogs_entry_to_621_and_153()
    {
        await _sut.PostAsync(CogsRequest(), default);

        await _journals.Received(1).AddAsync(
            Arg.Is<JournalEntry>(j =>
                j.Status == JournalEntryStatus.Posted &&
                j.TotalDebit == 28m &&
                j.TotalCredit == 28m &&
                j.SourceType == JournalSourceType.CostOfGoodsSold &&
                j.SourceDocumentId == OrderId &&
                j.Lines.Count == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Replay_of_the_same_source_does_not_duplicate_the_cogs_entry()
    {
        _journals.ExistsForSourceAsync(JournalSourceType.CostOfGoodsSold, OrderId, Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _sut.PostAsync(CogsRequest(), default);

        result.Should().Be(GLPostingResult.SkippedDuplicate);
        await _journals.DidNotReceive().AddAsync(Arg.Any<JournalEntry>(), Arg.Any<CancellationToken>());
    }
}

/// <summary>
/// FIN-P2-010 (REFUTED): the confirm-path and the WMS reserve→ship path are
/// FSM-disjoint, so COGS is recognized exactly once. A Draft→Confirmed order
/// books COGS at confirm via <see cref="OrderConfirmedStockHandler"/>. That
/// confirmed order holds no stock allocations, so when its shipment is later
/// dispatched <c>ConsumeForOrderLineAsync</c> issues nothing and returns cost 0;
/// the dispatch handler only enqueues a COGS posting when cogsCost &gt; 0, so no
/// second COGS is booked.
/// </summary>
public class ConfirmThenShipDoesNotDoubleCountCogsTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid ProductId = Guid.NewGuid();
    private static readonly Guid WarehouseId = Guid.NewGuid();

    private const decimal AvgCost = 7m;
    private const decimal Qty = 4m;
    private const decimal ExpectedCogs = Qty * AvgCost; // 28

    [Fact]
    public async Task Confirm_books_cogs_once_and_subsequent_dispatch_of_allocationless_order_books_no_more()
    {
        var orderId = Guid.NewGuid();

        var products = Substitute.For<IProductRepository>();
        var components = Substitute.For<IProductComponentRepository>();
        var stockTxns = Substitute.For<IStockTransactionRepository>();
        var warehouses = Substitute.For<IWarehouseRepository>();
        var stockItems = Substitute.For<IStockItemRepository>();
        var movements = Substitute.For<IStockMovementRepository>();
        var openingBalance = Substitute.For<IStockOpeningBalanceBridge>();
        var confirmOutbox = Substitute.For<IGLPostingOutbox>();

        components.GetTreeForProductsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, IReadOnlyList<(Guid, decimal)>>());
        warehouses.GetDefaultAsync(Arg.Any<CancellationToken>())
            .Returns(new Warehouse("WH-DEF", "Default", isDefault: true) { Id = WarehouseId, TenantId = TenantId });
        products.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, Product>
            {
                [ProductId] = new("SKU-A", "Widget", "pcs", 10m, "TRY", initialStock: 100m) { Id = ProductId, TenantId = TenantId },
            });
        stockItems.GetOrCreateAsync(ProductId, WarehouseId, Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                var item = new StockItem(ProductId, WarehouseId) { Id = Guid.NewGuid(), TenantId = TenantId };
                item.SeedOpeningBalance(100m, AvgCost, DateTime.UtcNow);
                return item;
            });

        var confirmHandler = new OrderConfirmedStockHandler(
            products, components, stockTxns, warehouses, stockItems, movements, confirmOutbox, openingBalance);

        GLPostingRequest? confirmPosting = null;
        await confirmOutbox.EnqueueAsync(Arg.Do<GLPostingRequest>(r => confirmPosting = r), Arg.Any<CancellationToken>());

        await confirmHandler.Handle(
            new OrderConfirmedEvent(TenantId, orderId, "ORD-1",
                new[] { new OrderLineSnapshot(ProductId, Qty) }, DateTime.UtcNow),
            default);

        // Confirm posts COGS exactly once.
        await confirmOutbox.Received(1).EnqueueAsync(Arg.Any<GLPostingRequest>(), Arg.Any<CancellationToken>());
        confirmPosting!.SourceType.Should().Be(JournalSourceType.CostOfGoodsSold);
        confirmPosting.Lines.Single(l => l.Key == GLPostingKey.CostOfGoodsSold).Debit.Should().Be(ExpectedCogs);

        // The confirmed order holds no allocations, so dispatch issues nothing.
        var shipments = Substitute.For<IShipmentRepository>();
        var orders = Substitute.For<IOrderRepository>();
        var allocator = Substitute.For<IAllocationService>();
        var dispatchOutbox = Substitute.For<IGLPostingOutbox>();
        var uow = Substitute.For<IUnitOfWork>();

        var order = new Order("ORD-1", Guid.NewGuid(), DateTime.UtcNow, "TRY") { Id = orderId, TenantId = TenantId };
        var orderLine = new OrderLine(ProductId, "SKU-A", "Widget", Qty, 10m);
        order.ReplaceLines(new[] { orderLine });
        order.ChangeStatus(OrderStatus.Confirmed);

        var shipment = new Shipment("SHP-1", orderId, order.CustomerId, WarehouseId, shippingAddressSnapshot: null)
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
        };
        shipment.AddLine(new ShipmentLine(orderLine.Id, ProductId, "SKU-A", "Widget", Qty, AvgCost));
        shipment.MarkPicked(null);
        shipment.MarkPacked();

        shipments.GetWithLinesAsync(shipment.Id, Arg.Any<CancellationToken>()).Returns(shipment);
        orders.GetWithLinesAndShipmentsAsync(orderId, Arg.Any<CancellationToken>()).Returns(order);
        // Allocation-less order: nothing to consume, zero issue cost.
        allocator.ConsumeForOrderLineAsync(orderId, orderLine.Id, Qty, Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(new OrderLineConsumption(0m, 0m));

        var dispatchHandler = new DispatchShipmentHandler(shipments, orders, allocator, dispatchOutbox, uow);
        await dispatchHandler.Handle(new DispatchShipmentCommand(shipment.Id, "Carrier", "TRK", null, null), default);

        // No second COGS posting from the dispatch (cogsCost == 0 path).
        await dispatchOutbox.DidNotReceiveWithAnyArgs().EnqueueAsync(default!, default);
    }
}
