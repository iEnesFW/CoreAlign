using CoreAlign.Application.Accounting.Services;
using CoreAlign.Application.B2B;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Application.Purchasing;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Purchasing;

// Drives the PO-linked, line-aware vendor-bill slice end to end: the write path
// (Create with Lines snapshots PoUnitCost), the three-way-match gate, the
// receipt-cost clearing + price-variance split, and the cancel reversal — with
// the captured GL legs replayed through the REAL GLPostingService to prove every
// journal balances and that 322 clears to zero against the goods receipt.
public class VendorBillLineAwarePostingTests
{
    private static readonly Guid VendorId = Guid.NewGuid();
    private static readonly Guid ProductId = Guid.NewGuid();
    private static readonly Guid PoId = Guid.NewGuid();
    private static readonly Guid PoLineId = Guid.NewGuid();

    private readonly IVendorBillRepository _bills = Substitute.For<IVendorBillRepository>();
    private readonly IVendorRepository _vendors = Substitute.For<IVendorRepository>();
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly IPurchaseOrderRepository _orders = Substitute.For<IPurchaseOrderRepository>();
    private readonly IVendorLedgerRepository _ledger = Substitute.For<IVendorLedgerRepository>();
    private readonly IGLPostingOutbox _outbox = Substitute.For<IGLPostingOutbox>();
    private readonly ITolerancePolicyProvider _tolerance = Substitute.For<ITolerancePolicyProvider>();
    private readonly ICurrentUserAccessor _currentUser = Substitute.For<ICurrentUserAccessor>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    public VendorBillLineAwarePostingTests()
    {
        _tolerance.GetAsync(Arg.Any<CancellationToken>()).Returns(ThreeWayMatchTolerance.EnabledDefault);
        _currentUser.UserIdOrThrow().Returns(Guid.NewGuid());
        _products.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, Product>
            {
                [ProductId] = new Product("SKU-1", "Widget", "pcs", 0m) { Id = ProductId },
            });
    }

    // Approved PO with one received line at the given unit cost.
    private static PurchaseOrder Po(decimal poUnitCost, decimal qtyReceived, decimal qtyOrdered)
    {
        var po = new PurchaseOrder("PO-1", VendorId, "Acme", DateTime.UtcNow, "TRY") { Id = PoId };
        var line = new PurchaseOrderLine(ProductId, "SKU-1", "Widget", qtyOrdered, poUnitCost) { Id = PoLineId };
        po.ReplaceLines(new[] { line });
        po.Submit();
        po.Approve(Guid.NewGuid());
        po.RecordLineReceipt(PoLineId, qtyReceived);
        return po;
    }

    private CreateVendorBillHandler CreateSut() =>
        new(_bills, _vendors, _products, _orders, _uow);

    private PostVendorBillHandler PostSut() =>
        new(_bills, _ledger, _vendors, _outbox, _orders, _tolerance, _uow);

    private ApproveVendorBillHandler ApproveSut() =>
        new(_bills, _ledger, _vendors, _outbox, _orders, _currentUser, _uow);

    private CancelVendorBillHandler CancelSut() =>
        new(_bills, _ledger, _vendors, _outbox, _orders, _uow);

    private GLPostingRequest CaptureGl(Action action)
    {
        GLPostingRequest? captured = null;
        _outbox.EnqueueAsync(Arg.Do<GLPostingRequest>(r => captured = r), Arg.Any<CancellationToken>());
        action();
        captured.Should().NotBeNull();
        return captured!;
    }

    private static GLPostingLine Leg(GLPostingRequest r, GLPostingKey key) =>
        r.Lines.Single(l => l.Key == key);

    // ---- write path: Create with Lines snapshots PoUnitCost and derives header totals. ----
    [Fact]
    public async Task Create_with_lines_snapshots_po_unit_cost_and_derives_header_totals()
    {
        var po = Po(poUnitCost: 10m, qtyReceived: 5m, qtyOrdered: 5m);
        _orders.GetByIdAsync(PoId, Arg.Any<CancellationToken>()).Returns(po);
        _vendors.GetByIdAsync(VendorId, Arg.Any<CancellationToken>()).Returns(new Vendor("Acme") { Id = VendorId });

        VendorBill? saved = null;
        await _bills.AddAsync(Arg.Do<VendorBill>(b => saved = b), Arg.Any<CancellationToken>());

        var cmd = new CreateVendorBillCommand(
            VendorId, "INV-1", DateTime.UtcNow, "TRY", 0m, 0m,
            PurchaseOrderId: PoId,
            Lines: new[] { new VendorBillLineInput(ProductId, 5m, 10.40m, 18m, PoLineId) });
        await CreateSut().Handle(cmd, default);

        saved.Should().NotBeNull();
        saved!.Lines.Should().HaveCount(1);
        var line = saved.Lines.Single();
        line.PoUnitCost.Should().Be(10m); // snapshotted from the PO line, NOT the billed 10.40
        line.UnitPrice.Should().Be(10.40m);
        line.ProductSku.Should().Be("SKU-1");
        saved.Subtotal.Should().Be(52m);   // 5 * 10.40 derived from the line
        saved.TaxAmount.Should().Be(9.36m); // 52 * 18%
        saved.Total.Should().Be(61.36m);
        saved.Lines.Any(l => l.PurchaseOrderLineId is not null).Should().BeTrue();
    }

    // ---- write path: PO-less line forces PoUnitCost == UnitPrice so variance is zero. ----
    [Fact]
    public async Task Create_poless_line_forces_pounitcost_equal_unitprice_zero_variance()
    {
        _vendors.GetByIdAsync(VendorId, Arg.Any<CancellationToken>()).Returns(new Vendor("Acme") { Id = VendorId });

        VendorBill? saved = null;
        await _bills.AddAsync(Arg.Do<VendorBill>(b => saved = b), Arg.Any<CancellationToken>());

        var cmd = new CreateVendorBillCommand(
            VendorId, "INV-1", DateTime.UtcNow, "TRY", 0m, 0m,
            Lines: new[] { new VendorBillLineInput(ProductId, 3m, 7m) }); // no PoLineId
        await CreateSut().Handle(cmd, default);

        var line = saved!.Lines.Single();
        line.PoUnitCost.Should().Be(7m);
        line.PriceVariance.Should().Be(0m);
        saved.Lines.Any(l => l.PurchaseOrderLineId is not null).Should().BeFalse();
    }

    // ---- backward compat: Create with NO lines keeps the header-only path working. ----
    [Fact]
    public async Task Create_without_lines_keeps_header_only_path()
    {
        _vendors.GetByIdAsync(VendorId, Arg.Any<CancellationToken>()).Returns(new Vendor("Acme") { Id = VendorId });

        VendorBill? saved = null;
        await _bills.AddAsync(Arg.Do<VendorBill>(b => saved = b), Arg.Any<CancellationToken>());

        var cmd = new CreateVendorBillCommand(VendorId, "INV-1", DateTime.UtcNow, "TRY", 1000m, 180m);
        await CreateSut().Handle(cmd, default);

        saved!.Lines.Should().BeEmpty();
        saved.Subtotal.Should().Be(1000m);
        saved.Total.Should().Be(1180m);
    }

    // ---- (a) in-tolerance PO-linked bill posts: 322 at receipt cost, PPV books the diff,
    //          balanced, and 322 nets to zero against the prior receipt credit. ----
    [Fact]
    public async Task InTolerance_lineBill_posts_322_at_receipt_cost_PPV_books_diff_and_322_nets_to_zero()
    {
        var po = Po(poUnitCost: 10m, qtyReceived: 5m, qtyOrdered: 5m);
        _orders.GetByIdAsync(PoId, Arg.Any<CancellationToken>()).Returns(po);
        var vendor = new Vendor("Acme") { Id = VendorId };
        _vendors.GetByIdAsync(VendorId, Arg.Any<CancellationToken>()).Returns(vendor);

        // Billed at 10.40 (4% over PO 10 -> inside the 5% default). qty 5.
        var bill = new VendorBill(VendorId, "Acme", "INV-1", DateTime.UtcNow, "TRY", 0m, 0m, purchaseOrderId: PoId)
        { Id = Guid.NewGuid() };
        bill.ReplaceLines(new[] { new VendorBillLine(ProductId, "SKU-1", "Widget", 5m, 10.40m, 10m, PoLineId, 0m) });
        _bills.GetByIdAsync(bill.Id, Arg.Any<CancellationToken>()).Returns(bill);
        _ledger.GetLastRunningBalanceAsync(VendorId, Arg.Any<CancellationToken>()).Returns(0m);

        var gl = CaptureGl(() => PostSut().Handle(new PostVendorBillCommand(bill.Id), default).GetAwaiter().GetResult());

        bill.Status.Should().Be(VendorBillStatus.Posted);
        Leg(gl, GLPostingKey.GoodsReceiptClearing).Debit.Should().Be(50m); // 5 * 10 receipt cost
        Leg(gl, GLPostingKey.PurchasePriceVariance).Debit.Should().Be(2m); // 52 subtotal - 50 clearing
        Leg(gl, GLPostingKey.AccountsPayable).Credit.Should().Be(52m);
        gl.Lines.Sum(l => l.Debit).Should().Be(gl.Lines.Sum(l => l.Credit));

        // Replay through the real GLPostingService; 322 debit (50) clears exactly
        // against a prior receipt credit of 50.
        var entry = await PostThroughRealServiceAsync(gl);
        entry.TotalDebit.Should().Be(entry.TotalCredit);
        var clearing322 = entry.Lines.Single(l => l.AccountCode == "322");
        var priorReceiptCredit = 50m;
        (priorReceiptCredit - clearing322.Debit).Should().Be(0m);
    }

    // ---- (b) rounding counterexample posts a BALANCED journal. ----
    [Fact]
    public async Task RoundingCounterexample_posts_balanced_journal()
    {
        // Isolate the rounding/balance proof from the hold gate (this large price
        // gap would otherwise be held); tolerance off so the bill posts straight.
        _tolerance.GetAsync(Arg.Any<CancellationToken>()).Returns(ThreeWayMatchTolerance.Disabled);
        var po = Po(poUnitCost: 1.11115m, qtyReceived: 1.5m, qtyOrdered: 1.5m);
        _orders.GetByIdAsync(PoId, Arg.Any<CancellationToken>()).Returns(po);
        _vendors.GetByIdAsync(VendorId, Arg.Any<CancellationToken>()).Returns(new Vendor("Acme") { Id = VendorId });

        var bill = new VendorBill(VendorId, "Acme", "INV-1", DateTime.UtcNow, "TRY", 0m, 0m, purchaseOrderId: PoId)
        { Id = Guid.NewGuid() };
        bill.ReplaceLines(new[] { new VendorBillLine(ProductId, "SKU-1", "Widget", 1.5m, 2.22225m, 1.11115m, PoLineId, 0m) });
        _bills.GetByIdAsync(bill.Id, Arg.Any<CancellationToken>()).Returns(bill);
        _ledger.GetLastRunningBalanceAsync(VendorId, Arg.Any<CancellationToken>()).Returns(0m);

        var gl = CaptureGl(() => PostSut().Handle(new PostVendorBillCommand(bill.Id), default).GetAwaiter().GetResult());

        // Subtotal = round(1.5*2.22225)=3.3334; clearing=round(1.5*1.11115)=1.6667;
        // variance = 3.3334-1.6667 = 1.6667 -> clearing+variance == Subtotal exactly.
        Leg(gl, GLPostingKey.GoodsReceiptClearing).Debit.Should().Be(1.6667m);
        Leg(gl, GLPostingKey.PurchasePriceVariance).Debit.Should().Be(1.6667m);
        Leg(gl, GLPostingKey.AccountsPayable).Credit.Should().Be(3.3334m);
        gl.Lines.Sum(l => l.Debit).Should().Be(gl.Lines.Sum(l => l.Credit));

        var entry = await PostThroughRealServiceAsync(gl);
        entry.TotalDebit.Should().Be(entry.TotalCredit); // real service did not throw
    }

    // ---- (c) price breach beyond tolerance -> PendingApproval, NO GL, no QuantityBilled bump. ----
    [Fact]
    public async Task PriceBreach_beyond_tolerance_holds_for_approval_and_does_not_post()
    {
        var po = Po(poUnitCost: 10m, qtyReceived: 5m, qtyOrdered: 5m);
        _orders.GetByIdAsync(PoId, Arg.Any<CancellationToken>()).Returns(po);
        _vendors.GetByIdAsync(VendorId, Arg.Any<CancellationToken>()).Returns(new Vendor("Acme") { Id = VendorId });

        // Billed at 12 (20% over PO 10) -> breaches 5% default.
        var bill = new VendorBill(VendorId, "Acme", "INV-1", DateTime.UtcNow, "TRY", 0m, 0m, purchaseOrderId: PoId)
        { Id = Guid.NewGuid() };
        bill.ReplaceLines(new[] { new VendorBillLine(ProductId, "SKU-1", "Widget", 5m, 12m, 10m, PoLineId, 0m) });
        _bills.GetByIdAsync(bill.Id, Arg.Any<CancellationToken>()).Returns(bill);

        await PostSut().Handle(new PostVendorBillCommand(bill.Id), default);

        bill.Status.Should().Be(VendorBillStatus.PendingApproval);
        await _outbox.DidNotReceiveWithAnyArgs().EnqueueAsync(default!, default);
        po.Lines.Single().QuantityBilled.Should().Be(0m);
    }

    // ---- (d) ApproveAndPost -> GL posts once, QuantityBilled bumped once. ----
    [Fact]
    public async Task ApproveAndPost_posts_gl_once_and_bumps_quantitybilled_once()
    {
        var po = Po(poUnitCost: 10m, qtyReceived: 5m, qtyOrdered: 5m);
        _orders.GetByIdAsync(PoId, Arg.Any<CancellationToken>()).Returns(po);
        _vendors.GetByIdAsync(VendorId, Arg.Any<CancellationToken>()).Returns(new Vendor("Acme") { Id = VendorId });
        _ledger.GetLastRunningBalanceAsync(VendorId, Arg.Any<CancellationToken>()).Returns(0m);

        var bill = new VendorBill(VendorId, "Acme", "INV-1", DateTime.UtcNow, "TRY", 0m, 0m, purchaseOrderId: PoId)
        { Id = Guid.NewGuid() };
        bill.ReplaceLines(new[] { new VendorBillLine(ProductId, "SKU-1", "Widget", 5m, 12m, 10m, PoLineId, 0m) });
        _bills.GetByIdAsync(bill.Id, Arg.Any<CancellationToken>()).Returns(bill);

        await PostSut().Handle(new PostVendorBillCommand(bill.Id), default);
        bill.Status.Should().Be(VendorBillStatus.PendingApproval);

        await ApproveSut().Handle(new ApproveVendorBillCommand(bill.Id), default);

        bill.Status.Should().Be(VendorBillStatus.Posted);
        await _outbox.Received(1).EnqueueAsync(Arg.Any<GLPostingRequest>(), Arg.Any<CancellationToken>());
        po.Lines.Single().QuantityBilled.Should().Be(5m); // bumped exactly once
    }

    // ---- (e) full cancel of a line-posted bill -> 322 and PPV net to zero. ----
    [Fact]
    public async Task FullCancel_of_line_posted_bill_nets_322_and_PPV_to_zero()
    {
        var po = Po(poUnitCost: 10m, qtyReceived: 5m, qtyOrdered: 5m);
        _orders.GetByIdAsync(PoId, Arg.Any<CancellationToken>()).Returns(po);
        _vendors.GetByIdAsync(VendorId, Arg.Any<CancellationToken>()).Returns(new Vendor("Acme") { Id = VendorId });
        _ledger.GetLastRunningBalanceAsync(VendorId, Arg.Any<CancellationToken>()).Returns(0m);

        var bill = new VendorBill(VendorId, "Acme", "INV-1", DateTime.UtcNow, "TRY", 0m, 0m, purchaseOrderId: PoId)
        { Id = Guid.NewGuid() };
        bill.ReplaceLines(new[] { new VendorBillLine(ProductId, "SKU-1", "Widget", 5m, 10.40m, 10m, PoLineId, 0m) });
        _bills.GetByIdAsync(bill.Id, Arg.Any<CancellationToken>()).Returns(bill);

        var postGl = CaptureGl(() => PostSut().Handle(new PostVendorBillCommand(bill.Id), default).GetAwaiter().GetResult());
        var cancelGl = CaptureGl(() => CancelSut().Handle(new CancelVendorBillCommand(bill.Id), default).GetAwaiter().GetResult());

        // Net of the two postings: 322 nets to zero, PPV nets to zero.
        decimal Net(GLPostingRequest r, GLPostingKey k) =>
            r.Lines.Where(l => l.Key == k).Sum(l => l.Debit - l.Credit);

        (Net(postGl, GLPostingKey.GoodsReceiptClearing) + Net(cancelGl, GLPostingKey.GoodsReceiptClearing))
            .Should().Be(0m);
        (Net(postGl, GLPostingKey.PurchasePriceVariance) + Net(cancelGl, GLPostingKey.PurchasePriceVariance))
            .Should().Be(0m);
        (Net(postGl, GLPostingKey.AccountsPayable) + Net(cancelGl, GLPostingKey.AccountsPayable))
            .Should().Be(0m);

        cancelGl.Lines.Sum(l => l.Debit).Should().Be(cancelGl.Lines.Sum(l => l.Credit));
        po.Lines.Single().QuantityBilled.Should().Be(0m); // bill then cancel -> back to zero
    }

    // ---- (e2) partial cancel of a partially-paid line bill reverses only the OPEN
    //          portion, prorated across the line-aware legs, and balances. ----
    [Fact]
    public async Task PartialCancel_of_partially_paid_line_bill_reverses_open_portion_balanced()
    {
        var po = Po(poUnitCost: 10m, qtyReceived: 5m, qtyOrdered: 5m);
        _orders.GetByIdAsync(PoId, Arg.Any<CancellationToken>()).Returns(po);
        _vendors.GetByIdAsync(VendorId, Arg.Any<CancellationToken>()).Returns(new Vendor("Acme") { Id = VendorId });
        _ledger.GetLastRunningBalanceAsync(VendorId, Arg.Any<CancellationToken>()).Returns(0m);

        // 5 @ 10.40, no tax -> Total 52 (clearing 50, variance 2).
        var bill = new VendorBill(VendorId, "Acme", "INV-1", DateTime.UtcNow, "TRY", 0m, 0m, purchaseOrderId: PoId)
        { Id = Guid.NewGuid() };
        bill.ReplaceLines(new[] { new VendorBillLine(ProductId, "SKU-1", "Widget", 5m, 10.40m, 10m, PoLineId, 0m) });
        _bills.GetByIdAsync(bill.Id, Arg.Any<CancellationToken>()).Returns(bill);

        await PostSut().Handle(new PostVendorBillCommand(bill.Id), default);
        bill.RecordPayment(26m); // pay half -> PartiallyPaid, AmountDue 26
        bill.Status.Should().Be(VendorBillStatus.PartiallyPaid);

        var cancelGl = CaptureGl(() => CancelSut().Handle(new CancelVendorBillCommand(bill.Id), default).GetAwaiter().GetResult());

        bill.Status.Should().Be(VendorBillStatus.Cancelled);
        // Reversal touches only the open 26, prorated (factor 0.5): CR 322 25 + CR PPV 1 == DR 320 26.
        Leg(cancelGl, GLPostingKey.AccountsPayable).Debit.Should().Be(26m);
        cancelGl.Lines.Sum(l => l.Debit).Should().Be(cancelGl.Lines.Sum(l => l.Credit));
        var entry = await PostThroughRealServiceAsync(cancelGl);
        entry.TotalDebit.Should().Be(entry.TotalCredit);
    }

    // ---- (f) PO-less / header-only bill still posts the OLD single-322 path. ----
    [Fact]
    public async Task PoLess_headerOnly_bill_posts_old_single_322_path()
    {
        _vendors.GetByIdAsync(VendorId, Arg.Any<CancellationToken>()).Returns(new Vendor("Acme") { Id = VendorId });
        _ledger.GetLastRunningBalanceAsync(VendorId, Arg.Any<CancellationToken>()).Returns(0m);

        // Header-only bill linked to a PO id but with NO lines -> inventory single-322 path.
        var bill = new VendorBill(VendorId, "Acme", "INV-1", DateTime.UtcNow, "TRY", 1000m, 180m, purchaseOrderId: PoId)
        { Id = Guid.NewGuid() };
        _bills.GetByIdAsync(bill.Id, Arg.Any<CancellationToken>()).Returns(bill);

        var gl = CaptureGl(() => PostSut().Handle(new PostVendorBillCommand(bill.Id), default).GetAwaiter().GetResult());

        // Single 322 leg at full subtotal, no PPV leg.
        Leg(gl, GLPostingKey.GoodsReceiptClearing).Debit.Should().Be(1000m);
        gl.Lines.Any(l => l.Key == GLPostingKey.PurchasePriceVariance).Should().BeFalse();
        Leg(gl, GLPostingKey.InputVat).Debit.Should().Be(180m);
        Leg(gl, GLPostingKey.AccountsPayable).Credit.Should().Be(1180m);
        gl.Lines.Sum(l => l.Debit).Should().Be(gl.Lines.Sum(l => l.Credit));
    }

    // ---- (g) FX (rate != 1) line bill balances in base TRY. ----
    [Fact]
    public async Task Fx_rate_not_one_line_bill_balances_in_base_try()
    {
        _tolerance.GetAsync(Arg.Any<CancellationToken>()).Returns(ThreeWayMatchTolerance.Disabled);
        var po = Po(poUnitCost: 1.11115m, qtyReceived: 1.5m, qtyOrdered: 1.5m);
        _orders.GetByIdAsync(PoId, Arg.Any<CancellationToken>()).Returns(po);
        _vendors.GetByIdAsync(VendorId, Arg.Any<CancellationToken>()).Returns(new Vendor("Acme") { Id = VendorId });
        _ledger.GetLastRunningBalanceAsync(VendorId, Arg.Any<CancellationToken>()).Returns(0m);

        var bill = new VendorBill(VendorId, "Acme", "INV-1", DateTime.UtcNow, "EUR", 0m, 0m,
            exchangeRate: 38.7531m, purchaseOrderId: PoId) { Id = Guid.NewGuid() };
        bill.ReplaceLines(new[] { new VendorBillLine(ProductId, "SKU-1", "Widget", 1.5m, 2.22225m, 1.11115m, PoLineId, 18m) });
        _bills.GetByIdAsync(bill.Id, Arg.Any<CancellationToken>()).Returns(bill);

        var gl = CaptureGl(() => PostSut().Handle(new PostVendorBillCommand(bill.Id), default).GetAwaiter().GetResult());

        // Document-currency legs balance.
        gl.Lines.Sum(l => l.Debit).Should().Be(gl.Lines.Sum(l => l.Credit));
        gl.ExchangeRate.Should().Be(38.7531m);

        // The real service translates to base TRY at the rate and the entry balances.
        var entry = await PostThroughRealServiceAsync(gl);
        entry.TotalDebit.Should().Be(entry.TotalCredit);
        entry.TotalDebit.Should().BeGreaterThan(0m);
    }

    // Replays captured GL legs through the real GLPostingService against an
    // in-memory chart of accounts (322/191/320/631), returning the posted entry.
    private static async Task<JournalEntry> PostThroughRealServiceAsync(GLPostingRequest gl)
    {
        var journals = Substitute.For<IJournalEntryRepository>();
        var accounts = Substitute.For<IGLAccountRepository>();
        var mappings = Substitute.For<IGLPostingMappingRepository>();
        var sequences = Substitute.For<IDocumentSequenceRepository>();
        var periods = Substitute.For<IAccountingPeriodRepository>();

        var chart = new List<GLAccount>
        {
            new("322", "Satıcılardan Alınan Avanslar / GR-IR", AccountType.Liability, isPostable: true),
            new("191", "İndirilecek KDV", AccountType.Asset, isPostable: true),
            new("320", "Satıcılar", AccountType.Liability, isPostable: true),
            new("631", "Pazarlama Satış Dağıtım Giderleri / PPV", AccountType.Expense, isPostable: true),
            new("153", "Ticari Mallar", AccountType.Asset, isPostable: true),
            new("632", "Genel Yönetim Giderleri", AccountType.Expense, isPostable: true),
        };
        accounts.GetAllAsync(Arg.Any<CancellationToken>()).Returns(chart);
        mappings.ListAsync(Arg.Any<CancellationToken>()).Returns(new List<GLPostingMapping>());
        sequences.GetAsync(DocumentSequenceType.JournalNumber, Arg.Any<CancellationToken>())
            .Returns(new DocumentSequence(DocumentSequenceType.JournalNumber, "YEV", 2026, 1, 5));

        JournalEntry? posted = null;
        await journals.AddAsync(Arg.Do<JournalEntry>(e => posted = e), Arg.Any<CancellationToken>());

        var sut = new GLPostingService(journals, accounts, mappings, sequences, periods);
        var result = await sut.PostAsync(gl, default);
        result.Should().Be(GLPostingResult.Posted);
        posted.Should().NotBeNull();
        return posted!;
    }
}
