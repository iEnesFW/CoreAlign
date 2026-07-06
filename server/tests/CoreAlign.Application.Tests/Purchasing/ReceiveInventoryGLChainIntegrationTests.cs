using CoreAlign.Application.Accounting.Services;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Application.Inventory.Services;
using CoreAlign.Application.Purchasing;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Services;

namespace CoreAlign.Application.Tests.Purchasing;

/// <summary>
/// P2P-9 — exercises the REAL goods-receipt chain end to end instead of mocking
/// IAllocationService: ReceivePurchaseOrderHandler -> the real AllocationService
/// (StockItem weighted-average AvgCost, StockMovement, Product.StockQuantity sync)
/// -> the GL request enqueued on the outbox -> the real GLPostingService that
/// resolves Inventory(153)/GoodsReceiptClearing(322) and translates foreign
/// currency at the document rate. PostVendorBill is covered with the same harness
/// to lock the FX direction on the 322/191/320 legs (regression guard for P2P-1).
/// </summary>
public class ReceiveInventoryGLChainIntegrationTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid VendorId = Guid.NewGuid();
    private static readonly Guid ProductId = Guid.NewGuid();
    private static readonly Guid WarehouseId = Guid.NewGuid();

    private readonly IStockItemRepository _stockItems = Substitute.For<IStockItemRepository>();
    private readonly IStockMovementRepository _movements = Substitute.For<IStockMovementRepository>();
    private readonly IStockAllocationRepository _allocations = Substitute.For<IStockAllocationRepository>();
    private readonly IWarehouseRepository _warehouses = Substitute.For<IWarehouseRepository>();
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly IPurchaseOrderRepository _orders = Substitute.For<IPurchaseOrderRepository>();
    private readonly IGoodsReceiptRepository _grns = Substitute.For<IGoodsReceiptRepository>();
    private readonly IDocumentSequenceRepository _sequences = Substitute.For<IDocumentSequenceRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private readonly List<StockMovement> _writtenMovements = new();
    private readonly List<GoodsReceipt> _writtenGrns = new();
    private readonly RecordingGLPostingService _gl = new();
    private readonly CapturingGLOutbox _outbox;
    private readonly AllocationService _allocation;
    private readonly StockItem _stockItem;
    private readonly Product _product;
    private int _grnCounter;

    public ReceiveInventoryGLChainIntegrationTests()
    {
        _stockItem = new StockItem(ProductId, WarehouseId) { Id = Guid.NewGuid(), TenantId = TenantId };
        _stockItems.GetOrCreateAsync(ProductId, WarehouseId, null, Arg.Any<CancellationToken>()).Returns(_stockItem);
        _stockItems.GetByProductAsync(ProductId, Arg.Any<CancellationToken>())
            .Returns(new List<StockItem> { _stockItem });

        _product = new Product("SKU-A", "Widget", "pcs", 10m, "TRY", initialStock: 0m)
        {
            Id = ProductId,
            TenantId = TenantId,
        };
        _products.GetByIdAsync(ProductId, Arg.Any<CancellationToken>()).Returns(_product);
        _products.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, Product> { [ProductId] = _product });

        _movements.AddAsync(Arg.Any<StockMovement>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(ci => _writtenMovements.Add(ci.Arg<StockMovement>()));

        _allocation = new AllocationService(
            _stockItems, _movements, _allocations, _warehouses, _products,
            new StockOpeningBalanceBridge(_stockItems, _products, _movements),
            new InventoryCostingService(Substitute.For<CoreAlign.Domain.Interfaces.IStockCostLayerRepository>()));

        _grns.GetByIdempotencyKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((GoodsReceipt?)null);
        _grns.AddAsync(Arg.Any<GoodsReceipt>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(ci => _writtenGrns.Add(ci.Arg<GoodsReceipt>()));
        _sequences.GetAsync(DocumentSequenceType.GoodsReceiptNumber, Arg.Any<CancellationToken>())
            .Returns(_ => new DocumentSequence(DocumentSequenceType.GoodsReceiptNumber, "GRN", 2026, 1, 5));
        _sequences.ConsumeAsync(DocumentSequenceType.GoodsReceiptNumber, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(_ => $"GRN-2026-{(++_grnCounter):00000}");

        _outbox = new CapturingGLOutbox(_gl);
    }

    private ReceivePurchaseOrderHandler ReceiveHandler() =>
        new(_orders, _grns, _allocation, _warehouses, _sequences, _outbox, _products, _uow);

    private static string NewKey() => Guid.NewGuid().ToString("N");

    private PurchaseOrder ApprovedPo(string currency, decimal exchangeRate, Guid lineId, decimal qty, decimal unitCost)
    {
        var po = new PurchaseOrder("PO-1", VendorId, "Acme", DateTime.UtcNow, currency)
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
        };
        po.UpdateHeader(VendorId, "Acme", DateTime.UtcNow, null, currency, exchangeRate, WarehouseId, null);
        var line = new PurchaseOrderLine(ProductId, "SKU-A", "Widget", qty, unitCost) { Id = lineId, TenantId = TenantId };
        po.ReplaceLines(new[] { line });
        po.Submit();
        po.Approve(Guid.NewGuid());
        _orders.GetByIdAsync(po.Id, Arg.Any<CancellationToken>()).Returns(po);
        return po;
    }

    [Fact]
    public async Task Goods_receipt_runs_real_chain_avgcost_movement_product_sync_and_balanced_gl()
    {
        _gl.SeedChart("153", "322");
        var lineId = Guid.NewGuid();
        var po = ApprovedPo("TRY", 1m, lineId, qty: 10m, unitCost: 4m);

        // First receipt: 10 @ 4.
        await ReceiveHandler().Handle(
            new ReceivePurchaseOrderCommand(po.Id, new List<ReceiptLineInput> { new(lineId, 10m) }, NewKey(), WarehouseId),
            default);

        _stockItem.OnHand.Should().Be(10m);
        _stockItem.AvgCost.Should().Be(4m, "first receipt sets AvgCost to the receipt unit cost");
        _product.StockQuantity.Should().Be(10m, "Product.StockQuantity mirrors the warehouse ledger");

        // Second receipt at a different unit cost blends the weighted average.
        // (10 * 4 + 10 * 6) / 20 = 100 / 20 = 5.
        var po2 = ApprovedPo("TRY", 1m, lineId, qty: 10m, unitCost: 6m);
        await ReceiveHandler().Handle(
            new ReceivePurchaseOrderCommand(po2.Id, new List<ReceiptLineInput> { new(lineId, 10m) }, NewKey(), WarehouseId),
            default);

        _stockItem.OnHand.Should().Be(20m);
        _stockItem.AvgCost.Should().Be(5m, "second receipt blends 10@4 with 10@6 to a weighted average of 5");
        _product.StockQuantity.Should().Be(20m);

        var receipts = _writtenMovements.Where(m => m.Type == StockMovementType.Receipt).ToList();
        receipts.Should().HaveCount(2);
        receipts[0].Quantity.Should().Be(10m);
        receipts[0].UnitCost.Should().Be(4m);
        receipts[0].TotalCost.Should().Be(40m);
        receipts[0].SourceDocumentType.Should().Be(StockSourceDocumentType.Purchase);

        // One GL entry per GRN (P2P-4 fix), each balanced DR 153 / CR 322 keyed by
        // the GRN id rather than the movement id. Two receipts -> two GRNs -> two entries.
        _gl.PostedEntries.Should().HaveCount(2);
        var firstGrnId = _writtenGrns[0].Id;
        var entry = _gl.PostedEntries.Single(e => e.SourceDocumentId == firstGrnId);
        entry.SourceType.Should().Be(JournalSourceType.GoodsReceipt);
        entry.TotalDebit.Should().Be(entry.TotalCredit, "the GL entry must balance");
        entry.TotalDebit.Should().Be(40m);
        entry.Lines.Single(l => l.AccountCode == "153").Debit.Should().Be(40m);
        entry.Lines.Single(l => l.AccountCode == "322").Credit.Should().Be(40m);
    }

    [Fact]
    public async Task Foreign_currency_receipt_posts_gl_in_base_currency_at_rate_not_one()
    {
        // USD PO at 30.00. Movement.TotalCost is the document-currency footing
        // (10 @ 50 = 500 USD); the verified GLPostingService direction translates
        // it to base TRY at the rate (500 * 30 = 15 000), NOT at rate=1 (500).
        _gl.SeedChart("153", "322");
        var lineId = Guid.NewGuid();
        var po = ApprovedPo("USD", 30m, lineId, qty: 10m, unitCost: 50m);

        await ReceiveHandler().Handle(
            new ReceivePurchaseOrderCommand(po.Id, new List<ReceiptLineInput> { new(lineId, 10m) }, NewKey(), WarehouseId),
            default);

        var entry = _gl.PostedEntries.Should().ContainSingle().Subject;
        entry.SourceType.Should().Be(JournalSourceType.GoodsReceipt);
        entry.SourceDocumentId.Should().Be(_writtenGrns.Single().Id, "the GRN id is the GL idempotency key");
        entry.TotalDebit.Should().Be(15000m, "the foreign receipt is booked in base TRY at the PO rate, not rate=1");
        entry.TotalCredit.Should().Be(15000m);
        entry.Lines.Single(l => l.AccountCode == "153").Debit.Should().Be(15000m);
        entry.Lines.Single(l => l.AccountCode == "322").Credit.Should().Be(15000m);
        entry.Lines.Should().OnlyContain(l => l.Currency == "TRY" && l.ExchangeRate == 30m);
    }

    [Fact]
    public async Task Partial_receipt_marks_partially_received_and_accumulates_quantities()
    {
        _gl.SeedChart("153", "322");
        var lineId = Guid.NewGuid();
        var po = ApprovedPo("TRY", 1m, lineId, qty: 10m, unitCost: 4m);

        await ReceiveHandler().Handle(
            new ReceivePurchaseOrderCommand(po.Id, new List<ReceiptLineInput> { new(lineId, 4m) }, NewKey(), WarehouseId),
            default);

        po.Status.Should().Be(PurchaseOrderStatus.PartiallyReceived);
        po.Lines.Single().QuantityReceived.Should().Be(4m);
        _stockItem.OnHand.Should().Be(4m);
        _product.StockQuantity.Should().Be(4m);

        // Receive the remaining 6 -> fully received, cumulative quantity 10.
        await ReceiveHandler().Handle(
            new ReceivePurchaseOrderCommand(po.Id, new List<ReceiptLineInput> { new(lineId, 6m) }, NewKey(), WarehouseId),
            default);

        po.Status.Should().Be(PurchaseOrderStatus.Received);
        po.Lines.Single().QuantityReceived.Should().Be(10m);
        _stockItem.OnHand.Should().Be(10m);
        _product.StockQuantity.Should().Be(10m);
        _writtenMovements.Count(m => m.Type == StockMovementType.Receipt).Should().Be(2);
    }

    [Fact]
    public async Task Foreign_vendor_bill_posts_gl_in_base_currency_at_rate_not_one()
    {
        // USD bill 1000 + 180 tax at 30.00. The verified GLPostingService direction
        // translates each document-currency leg to base TRY at the bill rate, NOT 1.
        // PurchaseOrderId set -> inventory purchase, so the debit leg is GR/IR (322).
        _gl.SeedChart("322", "191", "320");
        var bills = Substitute.For<IVendorBillRepository>();
        var ledger = Substitute.For<IVendorLedgerRepository>();
        var vendors = Substitute.For<IVendorRepository>();
        ledger.GetLastRunningBalanceAsync(VendorId, Arg.Any<CancellationToken>()).Returns(0m);

        var bill = new VendorBill(VendorId, "Acme", "INV-1", DateTime.UtcNow, "USD",
            1000m, 180m, dueDate: null, exchangeRate: 30m, purchaseOrderId: Guid.NewGuid())
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
        };
        bills.GetByIdAsync(bill.Id, Arg.Any<CancellationToken>()).Returns(bill);

        var tolerance = Substitute.For<ITolerancePolicyProvider>();
        tolerance.GetAsync(Arg.Any<CancellationToken>()).Returns(ThreeWayMatchTolerance.Disabled);
        var sut = new PostVendorBillHandler(bills, ledger, vendors, _outbox, _orders, tolerance, _uow);
        await sut.Handle(new PostVendorBillCommand(bill.Id), default);

        var entry = _gl.PostedEntries.Should().ContainSingle().Subject;
        entry.SourceType.Should().Be(JournalSourceType.VendorBill);
        entry.SourceDocumentId.Should().Be(bill.Id);
        entry.TotalDebit.Should().Be(35400m, "1180 USD * 30 = 35 400 base TRY, not 1180 at rate=1");
        entry.TotalCredit.Should().Be(35400m);
        entry.Lines.Single(l => l.AccountCode == "322").Debit.Should().Be(30000m); // 1000 * 30
        entry.Lines.Single(l => l.AccountCode == "191").Debit.Should().Be(5400m);  // 180 * 30
        entry.Lines.Single(l => l.AccountCode == "320").Credit.Should().Be(35400m);
        entry.Lines.Should().OnlyContain(l => l.Currency == "TRY" && l.ExchangeRate == 30m);
    }

    /// <summary>
    /// Stands in for the transactional outbox + its async drain: it forwards each
    /// enqueued GLPostingRequest straight to a real GLPostingService so the test
    /// exercises the genuine account-resolution + FX-translation path that the
    /// outbox processor would run in production.
    /// </summary>
    private sealed class CapturingGLOutbox : IGLPostingOutbox
    {
        private readonly RecordingGLPostingService _gl;
        public CapturingGLOutbox(RecordingGLPostingService gl) => _gl = gl;

        public Task EnqueueAsync(GLPostingRequest request, CancellationToken cancellationToken = default) =>
            _gl.PostAsync(request, cancellationToken);
    }

    /// <summary>
    /// A real GLPostingService wired to in-memory NSubstitute repositories with a
    /// configurable chart of accounts, capturing every JournalEntry it posts.
    /// </summary>
    private sealed class RecordingGLPostingService
    {
        private readonly IJournalEntryRepository _journals = Substitute.For<IJournalEntryRepository>();
        private readonly IGLAccountRepository _accounts = Substitute.For<IGLAccountRepository>();
        private readonly IGLPostingMappingRepository _mappings = Substitute.For<IGLPostingMappingRepository>();
        private readonly IDocumentSequenceRepository _sequences = Substitute.For<IDocumentSequenceRepository>();
        private readonly IAccountingPeriodRepository _periods = Substitute.For<IAccountingPeriodRepository>();
        private readonly List<GLAccount> _chart = new();
        private readonly GLPostingService _service;

        public List<JournalEntry> PostedEntries { get; } = new();

        public RecordingGLPostingService()
        {
            _sequences.GetAsync(DocumentSequenceType.JournalNumber, Arg.Any<CancellationToken>())
                .Returns(_ => new DocumentSequence(DocumentSequenceType.JournalNumber, "YEV", 2026, 1, 5));
            _accounts.GetAllAsync(Arg.Any<CancellationToken>()).Returns(_chart);
            _mappings.ListAsync(Arg.Any<CancellationToken>()).Returns(new List<GLPostingMapping>());
            _journals.AddAsync(Arg.Any<JournalEntry>(), Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask)
                .AndDoes(ci => PostedEntries.Add(ci.Arg<JournalEntry>()));
            _service = new GLPostingService(_journals, _accounts, _mappings, _sequences, _periods);
        }

        public void SeedChart(params string[] codes)
        {
            foreach (var code in codes)
            {
                _chart.Add(new GLAccount(code, $"Account {code}", AccountType.Asset, isPostable: true));
            }
        }

        public Task<GLPostingResult> PostAsync(GLPostingRequest request, CancellationToken cancellationToken) =>
            _service.PostAsync(request, cancellationToken);
    }
}
