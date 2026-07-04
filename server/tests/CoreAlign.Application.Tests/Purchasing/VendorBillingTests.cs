using CoreAlign.Application.Accounting.Services;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Application.Purchasing;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Purchasing;

public class VendorBillDomainTests
{
    private static VendorBill Bill() =>
        new(Guid.NewGuid(), "Acme", "INV-1", DateTime.UtcNow, "TRY", 1000m, 180m);

    [Fact]
    public void Total_is_subtotal_plus_tax()
    {
        Bill().Total.Should().Be(1180m);
    }

    [Fact]
    public void Payment_partial_then_full_advances_status()
    {
        var b = Bill();
        b.Post();
        b.RecordPayment(180m);
        b.Status.Should().Be(VendorBillStatus.PartiallyPaid);
        b.AmountDue.Should().Be(1000m);
        b.RecordPayment(1000m);
        b.Status.Should().Be(VendorBillStatus.Paid);
        b.AmountDue.Should().Be(0m);
    }

    [Fact]
    public void Payment_exceeding_due_throws()
    {
        var b = Bill();
        b.Post();
        var act = () => b.RecordPayment(2000m);
        act.Should().Throw<StockMovementValidationException>();
    }
}

public class PostVendorBillHandlerTests
{
    private readonly IVendorBillRepository _bills = Substitute.For<IVendorBillRepository>();
    private readonly IVendorLedgerRepository _ledger = Substitute.For<IVendorLedgerRepository>();
    private readonly IVendorRepository _vendors = Substitute.For<IVendorRepository>();
    private readonly IGLPostingOutbox _outbox = Substitute.For<IGLPostingOutbox>();
    private readonly IPurchaseOrderRepository _orders = Substitute.For<IPurchaseOrderRepository>();
    private readonly ITolerancePolicyProvider _tolerance = Substitute.For<ITolerancePolicyProvider>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly PostVendorBillHandler _sut;

    public PostVendorBillHandlerTests()
    {
        _tolerance.GetAsync(Arg.Any<CancellationToken>()).Returns(ThreeWayMatchTolerance.Disabled);
        _sut = new PostVendorBillHandler(_bills, _ledger, _vendors, _outbox, _orders, _tolerance, _uow);
    }

    [Fact]
    public async Task Posting_a_bill_writes_a_credit_ledger_entry()
    {
        var bill = new VendorBill(Guid.NewGuid(), "Acme", "INV-1", DateTime.UtcNow, "TRY", 1000m, 180m)
        {
            Id = Guid.NewGuid(),
        };
        _bills.GetByIdAsync(bill.Id, Arg.Any<CancellationToken>()).Returns(bill);
        _ledger.GetLastRunningBalanceAsync(bill.VendorId, Arg.Any<CancellationToken>()).Returns(0m);

        await _sut.Handle(new PostVendorBillCommand(bill.Id), default);

        bill.Status.Should().Be(VendorBillStatus.Posted);
        await _ledger.Received(1).AddAsync(
            Arg.Is<VendorLedgerEntry>(e =>
                e.EntryType == LedgerEntryType.Credit &&
                e.Amount == 1180m &&
                e.SourceType == LedgerSourceType.Invoice &&
                e.RunningBalanceAfter == 1180m),
            Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Posting_header_only_bill_against_a_po_holds_for_approval_and_posts_no_gl()
    {
        _tolerance.GetAsync(Arg.Any<CancellationToken>()).Returns(ThreeWayMatchTolerance.EnabledDefault);
        var poId = Guid.NewGuid();
        var bill = new VendorBill(Guid.NewGuid(), "Acme", "INV-1", DateTime.UtcNow, "TRY", 1000m, 180m, purchaseOrderId: poId)
        {
            Id = Guid.NewGuid(),
        };
        _bills.GetByIdAsync(bill.Id, Arg.Any<CancellationToken>()).Returns(bill);
        _orders.GetByIdAsync(poId, Arg.Any<CancellationToken>())
            .Returns(new PurchaseOrder("PO-1", bill.VendorId, "Acme", DateTime.UtcNow, "TRY") { Id = poId });

        await _sut.Handle(new PostVendorBillCommand(bill.Id), default);

        bill.Status.Should().Be(VendorBillStatus.PendingApproval);
        await _outbox.DidNotReceiveWithAnyArgs().EnqueueAsync(default!, default);
    }

    [Fact]
    public async Task Posting_po_bill_with_an_unlinked_line_holds_for_approval()
    {
        _tolerance.GetAsync(Arg.Any<CancellationToken>()).Returns(ThreeWayMatchTolerance.EnabledDefault);
        var poId = Guid.NewGuid();
        var bill = new VendorBill(Guid.NewGuid(), "Acme", "INV-1", DateTime.UtcNow, "TRY", 100m, 18m, purchaseOrderId: poId)
        {
            Id = Guid.NewGuid(),
        };
        bill.ReplaceLines(new[]
        {
            new VendorBillLine(Guid.NewGuid(), "SKU", "Item", 1m, 100m, poUnitCost: 100m, purchaseOrderLineId: null, taxRatePercent: 18m),
        });
        _bills.GetByIdAsync(bill.Id, Arg.Any<CancellationToken>()).Returns(bill);
        _orders.GetByIdAsync(poId, Arg.Any<CancellationToken>())
            .Returns(new PurchaseOrder("PO-1", bill.VendorId, "Acme", DateTime.UtcNow, "TRY") { Id = poId });

        await _sut.Handle(new PostVendorBillCommand(bill.Id), default);

        bill.Status.Should().Be(VendorBillStatus.PendingApproval);
        await _outbox.DidNotReceiveWithAnyArgs().EnqueueAsync(default!, default);
    }

    [Fact]
    public async Task Posting_po_bill_whose_po_does_not_resolve_holds_for_approval()
    {
        _tolerance.GetAsync(Arg.Any<CancellationToken>()).Returns(ThreeWayMatchTolerance.EnabledDefault);
        var bill = new VendorBill(Guid.NewGuid(), "Acme", "INV-1", DateTime.UtcNow, "TRY", 1000m, 180m, purchaseOrderId: Guid.NewGuid())
        {
            Id = Guid.NewGuid(),
        };
        _bills.GetByIdAsync(bill.Id, Arg.Any<CancellationToken>()).Returns(bill);
        // _orders.GetByIdAsync(poId) is deliberately NOT stubbed → returns null (deleted / cross-tenant / stale PO id).

        await _sut.Handle(new PostVendorBillCommand(bill.Id), default);

        bill.Status.Should().Be(VendorBillStatus.PendingApproval);
        await _outbox.DidNotReceiveWithAnyArgs().EnqueueAsync(default!, default);
    }
}

public class CancelVendorBillHandlerTests
{
    private readonly IVendorBillRepository _bills = Substitute.For<IVendorBillRepository>();
    private readonly IVendorLedgerRepository _ledger = Substitute.For<IVendorLedgerRepository>();
    private readonly IVendorRepository _vendors = Substitute.For<IVendorRepository>();
    private readonly IGLPostingOutbox _outbox = Substitute.For<IGLPostingOutbox>();
    private readonly IPurchaseOrderRepository _orders = Substitute.For<IPurchaseOrderRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly CancelVendorBillHandler _sut;

    public CancelVendorBillHandlerTests()
    {
        _sut = new CancelVendorBillHandler(_bills, _ledger, _vendors, _outbox, _orders, _uow);
    }

    [Fact]
    public async Task Cancelling_partially_paid_bill_reverses_only_the_unpaid_amount()
    {
        // Bill 1180 (1000 + 180 tax), 180 already paid → 1000 still due.
        var bill = new VendorBill(Guid.NewGuid(), "Acme", "INV-1", DateTime.UtcNow, "TRY", 1000m, 180m)
        {
            Id = Guid.NewGuid(),
        };
        bill.Post();
        bill.RecordPayment(180m);
        _bills.GetByIdAsync(bill.Id, Arg.Any<CancellationToken>()).Returns(bill);

        GLPostingRequest? captured = null;
        _outbox.WhenForAnyArgs(o => o.EnqueueAsync(default!, default))
            .Do(ci => captured = ci.Arg<GLPostingRequest>());

        await _sut.Handle(new CancelVendorBillCommand(bill.Id), default);

        captured.Should().NotBeNull();
        var ap = captured!.Lines.Single(l => l.Key == GLPostingKey.AccountsPayable);
        ap.Debit.Should().Be(1000m); // only the unpaid portion, not the full 1180
        captured.Lines.Where(l => l.Key != GLPostingKey.AccountsPayable).Sum(l => l.Credit)
            .Should().Be(1000m); // balances exactly to AmountDue
    }
}
