using CoreAlign.Application.Accounting.Services;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Application.Inventory.Services;
using CoreAlign.Application.Purchasing;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Services;

namespace CoreAlign.Application.Tests.Purchasing;

public class GoodsReceiptReversalAndIdempotencyTests
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

    public GoodsReceiptReversalAndIdempotencyTests()
    {
        _stockItem = new StockItem(ProductId, WarehouseId) { Id = Guid.NewGuid(), TenantId = TenantId };
        _stockItems.GetOrCreateAsync(ProductId, WarehouseId, null, Arg.Any<CancellationToken>()).Returns(_stockItem);
        _stockItems.GetAsync(ProductId, WarehouseId, null, Arg.Any<CancellationToken>()).Returns(_stockItem);
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
            new StockOpeningBalanceBridge(_stockItems, _products, _movements));

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

    private ReverseGoodsReceiptHandler ReverseHandler() =>
        new(_grns, _orders, _allocation, _outbox, _uow);

    private ApproveGoodsReceiptQcHandler ApproveQcHandler() =>
        new(_grns, _orders, _allocation, _outbox, _uow);

    private RejectGoodsReceiptQcHandler RejectQcHandler() =>
        new(_grns, _orders, _uow);

    private PurchaseOrder ApprovedPo(Guid lineId, decimal qty, decimal unitCost)
    {
        var po = new PurchaseOrder("PO-1", VendorId, "Acme", DateTime.UtcNow, "TRY")
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
        };
        po.UpdateHeader(VendorId, "Acme", DateTime.UtcNow, null, "TRY", 1m, WarehouseId, null);
        var line = new PurchaseOrderLine(ProductId, "SKU-A", "Widget", qty, unitCost) { Id = lineId, TenantId = TenantId };
        po.ReplaceLines(new[] { line });
        po.Submit();
        po.Approve(Guid.NewGuid());
        _orders.GetByIdAsync(po.Id, Arg.Any<CancellationToken>()).Returns(po);
        return po;
    }

    [Fact]
    public async Task Qc_required_receive_holds_stock_and_gl_until_approved()
    {
        _gl.SeedChart("153", "322");
        _product.SetRequiresInspection(true);
        var lineId = Guid.NewGuid();
        var po = ApprovedPo(lineId, qty: 10m, unitCost: 4m);

        await ReceiveHandler().Handle(
            new ReceivePurchaseOrderCommand(po.Id, new List<ReceiptLineInput> { new(lineId, 10m) },
                Guid.NewGuid().ToString("N"), WarehouseId),
            default);

        var grn = _writtenGrns.Single();
        grn.QcStatus.Should().Be(GoodsReceiptQcStatus.PendingInspection);
        _writtenMovements.Should().NotContain(m => m.Type == StockMovementType.Receipt, "QC hold defers the stock receipt");
        _gl.PostedEntries.Should().BeEmpty("no inventory GL is recognized while awaiting QC");
        _stockItem.OnHand.Should().Be(0m, "held goods are not available stock");
        po.Lines.Single().QuantityReceived.Should().Be(10m, "the PO still progresses on receive");
    }

    [Fact]
    public async Task Approving_qc_applies_stock_and_gl_exactly_once()
    {
        _gl.SeedChart("153", "322");
        _product.SetRequiresInspection(true);
        var lineId = Guid.NewGuid();
        var po = ApprovedPo(lineId, qty: 10m, unitCost: 4m);

        await ReceiveHandler().Handle(
            new ReceivePurchaseOrderCommand(po.Id, new List<ReceiptLineInput> { new(lineId, 10m) },
                Guid.NewGuid().ToString("N"), WarehouseId),
            default);
        var grn = _writtenGrns.Single();
        _grns.GetByIdAsync(grn.Id, Arg.Any<CancellationToken>()).Returns(grn);

        await ApproveQcHandler().Handle(new ApproveGoodsReceiptQcCommand(grn.Id, Guid.NewGuid()), default);

        grn.QcStatus.Should().Be(GoodsReceiptQcStatus.Approved);
        _stockItem.OnHand.Should().Be(10m, "approval applies the deferred receipt");
        _gl.PostedEntries.Count(e => e.SourceType == JournalSourceType.GoodsReceipt).Should().Be(1);
        _writtenMovements.Count(m => m.Type == StockMovementType.Receipt).Should().Be(1);

        var second = () => ApproveQcHandler().Handle(new ApproveGoodsReceiptQcCommand(grn.Id, Guid.NewGuid()), default);
        await second.Should().ThrowAsync<InvalidGoodsReceiptQcTransitionException>();
        _stockItem.OnHand.Should().Be(10m, "a second approve must not double-apply stock");
        _writtenMovements.Count(m => m.Type == StockMovementType.Receipt).Should().Be(1);
    }

    [Fact]
    public async Task Rejecting_qc_adds_no_stock_and_unrecords_the_po_line()
    {
        _gl.SeedChart("153", "322");
        _product.SetRequiresInspection(true);
        var lineId = Guid.NewGuid();
        var po = ApprovedPo(lineId, qty: 10m, unitCost: 4m);

        await ReceiveHandler().Handle(
            new ReceivePurchaseOrderCommand(po.Id, new List<ReceiptLineInput> { new(lineId, 10m) },
                Guid.NewGuid().ToString("N"), WarehouseId),
            default);
        var grn = _writtenGrns.Single();
        _grns.GetByIdAsync(grn.Id, Arg.Any<CancellationToken>()).Returns(grn);

        await RejectQcHandler().Handle(new RejectGoodsReceiptQcCommand(grn.Id, "Kırık geldi", Guid.NewGuid()), default);

        grn.QcStatus.Should().Be(GoodsReceiptQcStatus.Rejected);
        grn.QcRejectionReason.Should().Be("Kırık geldi");
        _stockItem.OnHand.Should().Be(0m, "rejected goods never enter stock");
        _writtenMovements.Should().NotContain(m => m.Type == StockMovementType.Receipt);
        _gl.PostedEntries.Should().BeEmpty("no GL on a rejected QC hold");
        po.Lines.Single().QuantityReceived.Should().Be(0m, "the rejected qty is un-recorded on the PO");
    }

    [Fact]
    public async Task Resending_receive_with_same_idempotency_key_is_a_no_op()
    {
        _gl.SeedChart("153", "322");
        var lineId = Guid.NewGuid();
        var po = ApprovedPo(lineId, qty: 10m, unitCost: 4m);
        var key = Guid.NewGuid().ToString("N");

        await ReceiveHandler().Handle(
            new ReceivePurchaseOrderCommand(po.Id, new List<ReceiptLineInput> { new(lineId, 10m) }, key, WarehouseId),
            default);

        _writtenMovements.Count(m => m.Type == StockMovementType.Receipt).Should().Be(1);
        _gl.PostedEntries.Should().HaveCount(1);
        _stockItem.OnHand.Should().Be(10m);

        // Simulate the persisted GRN being found on the retry with the same key.
        _grns.GetByIdempotencyKeyAsync(key, Arg.Any<CancellationToken>()).Returns(_writtenGrns[0]);

        await ReceiveHandler().Handle(
            new ReceivePurchaseOrderCommand(po.Id, new List<ReceiptLineInput> { new(lineId, 10m) }, key, WarehouseId),
            default);

        _writtenMovements.Count(m => m.Type == StockMovementType.Receipt).Should().Be(1, "no second stock movement");
        _gl.PostedEntries.Should().HaveCount(1, "no second GL entry");
        _stockItem.OnHand.Should().Be(10m, "stock is not double-applied");
        po.Lines.Single().QuantityReceived.Should().Be(10m);
    }

    [Fact]
    public async Task Reversing_a_receipt_issues_stock_back_and_posts_swapped_gl_legs_keyed_by_grn()
    {
        _gl.SeedChart("153", "322");
        var lineId = Guid.NewGuid();
        var po = ApprovedPo(lineId, qty: 10m, unitCost: 4m);

        await ReceiveHandler().Handle(
            new ReceivePurchaseOrderCommand(po.Id, new List<ReceiptLineInput> { new(lineId, 10m) },
                Guid.NewGuid().ToString("N"), WarehouseId),
            default);

        var grn = _writtenGrns.Single();
        _grns.GetByIdAsync(grn.Id, Arg.Any<CancellationToken>()).Returns(grn);

        await ReverseHandler().Handle(new ReverseGoodsReceiptCommand(grn.Id, "Hatalı kabul", Guid.NewGuid()), default);

        grn.Status.Should().Be(GoodsReceiptStatus.Reversed);
        _stockItem.OnHand.Should().Be(0m, "the reversal issues the received quantity back out");
        _product.StockQuantity.Should().Be(0m, "the dual ledger drops in lockstep");
        po.Lines.Single().QuantityReceived.Should().Be(0m);
        po.Status.Should().Be(PurchaseOrderStatus.Approved, "all received qty reversed -> back to Approved");

        var reversal = _gl.PostedEntries.Single(e => e.SourceType == JournalSourceType.GoodsReceiptReversal);
        reversal.SourceDocumentId.Should().Be(grn.Id);
        reversal.TotalDebit.Should().Be(reversal.TotalCredit, "reversal entry must balance");
        reversal.TotalDebit.Should().Be(40m);
        reversal.Lines.Single(l => l.AccountCode == "322").Debit.Should().Be(40m, "DR 322 reverses the original credit");
        reversal.Lines.Single(l => l.AccountCode == "153").Credit.Should().Be(40m, "CR 153 reverses the original debit");
    }

    [Fact]
    public async Task Reversing_a_billed_receipt_is_blocked()
    {
        _gl.SeedChart("153", "322");
        var lineId = Guid.NewGuid();
        var po = ApprovedPo(lineId, qty: 10m, unitCost: 4m);

        await ReceiveHandler().Handle(
            new ReceivePurchaseOrderCommand(po.Id, new List<ReceiptLineInput> { new(lineId, 10m) },
                Guid.NewGuid().ToString("N"), WarehouseId),
            default);

        // The vendor bill later debited GR/IR against the received qty.
        po.RecordLineBill(lineId, 10m);

        var grn = _writtenGrns.Single();
        _grns.GetByIdAsync(grn.Id, Arg.Any<CancellationToken>()).Returns(grn);

        var act = async () => await ReverseHandler().Handle(
            new ReverseGoodsReceiptCommand(grn.Id, null, Guid.NewGuid()), default);

        await act.Should().ThrowAsync<GoodsReceiptAlreadyBilledException>();
        grn.Status.Should().Be(GoodsReceiptStatus.Posted, "a blocked reversal must not mutate the GRN");
        _stockItem.OnHand.Should().Be(10m, "a blocked reversal must not touch stock");
    }

    [Fact]
    public async Task Double_reverse_is_idempotent()
    {
        _gl.SeedChart("153", "322");
        var lineId = Guid.NewGuid();
        var po = ApprovedPo(lineId, qty: 10m, unitCost: 4m);

        await ReceiveHandler().Handle(
            new ReceivePurchaseOrderCommand(po.Id, new List<ReceiptLineInput> { new(lineId, 10m) },
                Guid.NewGuid().ToString("N"), WarehouseId),
            default);

        var grn = _writtenGrns.Single();
        _grns.GetByIdAsync(grn.Id, Arg.Any<CancellationToken>()).Returns(grn);

        await ReverseHandler().Handle(new ReverseGoodsReceiptCommand(grn.Id, null, Guid.NewGuid()), default);
        var reversalEntriesAfterFirst = _gl.PostedEntries.Count(e => e.SourceType == JournalSourceType.GoodsReceiptReversal);

        await ReverseHandler().Handle(new ReverseGoodsReceiptCommand(grn.Id, null, Guid.NewGuid()), default);

        _gl.PostedEntries.Count(e => e.SourceType == JournalSourceType.GoodsReceiptReversal)
            .Should().Be(reversalEntriesAfterFirst, "re-reversing a reversed GRN is a no-op");
        _stockItem.OnHand.Should().Be(0m, "stock is not issued back twice");
    }

    private sealed class CapturingGLOutbox : IGLPostingOutbox
    {
        private readonly RecordingGLPostingService _gl;
        public CapturingGLOutbox(RecordingGLPostingService gl) => _gl = gl;

        public Task EnqueueAsync(GLPostingRequest request, CancellationToken cancellationToken = default) =>
            _gl.PostAsync(request, cancellationToken);
    }

    private sealed class RecordingGLPostingService
    {
        private readonly IJournalEntryRepository _journals = Substitute.For<IJournalEntryRepository>();
        private readonly IGLAccountRepository _accounts = Substitute.For<IGLAccountRepository>();
        private readonly IGLPostingMappingRepository _mappings = Substitute.For<IGLPostingMappingRepository>();
        private readonly IDocumentSequenceRepository _sequences = Substitute.For<IDocumentSequenceRepository>();
        private readonly IAccountingPeriodRepository _periods = Substitute.For<IAccountingPeriodRepository>();
        private readonly List<GLAccount> _chart = new();
        private readonly HashSet<(JournalSourceType, Guid)> _posted = new();
        private readonly GLPostingService _service;

        public List<JournalEntry> PostedEntries { get; } = new();

        public RecordingGLPostingService()
        {
            _sequences.GetAsync(DocumentSequenceType.JournalNumber, Arg.Any<CancellationToken>())
                .Returns(_ => new DocumentSequence(DocumentSequenceType.JournalNumber, "YEV", 2026, 1, 5));
            _accounts.GetAllAsync(Arg.Any<CancellationToken>()).Returns(_chart);
            _mappings.ListAsync(Arg.Any<CancellationToken>()).Returns(new List<GLPostingMapping>());
            _journals.ExistsForSourceAsync(Arg.Any<JournalSourceType>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns(ci => _posted.Contains((ci.Arg<JournalSourceType>(), ci.Arg<Guid>())));
            _journals.AddAsync(Arg.Any<JournalEntry>(), Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask)
                .AndDoes(ci =>
                {
                    var e = ci.Arg<JournalEntry>();
                    PostedEntries.Add(e);
                    _posted.Add((e.SourceType!.Value, e.SourceDocumentId!.Value));
                });
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
