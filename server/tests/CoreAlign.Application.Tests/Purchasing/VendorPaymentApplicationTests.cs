using CoreAlign.Application.Accounting.Services;
using CoreAlign.Application.B2B;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Application.Purchasing;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Purchasing;

public class VendorPaymentDomainTests
{
    [Fact]
    public void RecordApplication_advances_applied_amount_and_blocks_void()
    {
        var p = new VendorPayment(Guid.NewGuid(), "Acme", "VPAY-1", DateTime.UtcNow, 1000m, "TRY");
        p.RecordApplication(400m);
        p.AppliedAmount.Should().Be(400m);
        p.UnappliedAmount.Should().Be(600m);
        p.IsDraft.Should().BeFalse();

        var act = () => p.Void("oops");
        act.Should().Throw<VendorPaymentImmutableException>();
    }

    [Fact]
    public void RecordApplication_blocks_over_application()
    {
        var p = new VendorPayment(Guid.NewGuid(), "Acme", "VPAY-1", DateTime.UtcNow, 100m, "TRY");
        var act = () => p.RecordApplication(150m);
        act.Should().Throw<VendorPaymentOverApplicationException>();
    }

    [Fact]
    public void UpdateDraft_throws_after_application()
    {
        var p = new VendorPayment(Guid.NewGuid(), "Acme", "VPAY-1", DateTime.UtcNow, 1000m, "TRY");
        p.RecordApplication(10m);
        var act = () => p.UpdateDraft(DateTime.UtcNow, 999m, "TRY", 1m, null, null);
        act.Should().Throw<VendorPaymentImmutableException>();
    }

    [Fact]
    public void New_payment_is_not_posted_until_post_is_called()
    {
        var p = new VendorPayment(Guid.NewGuid(), "Acme", "VPAY-1", DateTime.UtcNow, 1000m, "TRY");
        p.IsPosted.Should().BeFalse();

        p.Post();
        p.IsPosted.Should().BeTrue();
    }
}

public class UpdateVendorPaymentHandlerTests
{
    private readonly IVendorPaymentRepository _payments = Substitute.For<IVendorPaymentRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly UpdateVendorPaymentHandler _sut;

    public UpdateVendorPaymentHandlerTests()
    {
        _sut = new UpdateVendorPaymentHandler(_payments, _uow);
    }

    [Fact]
    public async Task Update_blocks_a_posted_payment()
    {
        var payment = new VendorPayment(Guid.NewGuid(), "Acme", "VPAY-1", DateTime.UtcNow, 1000m, "TRY") { Id = Guid.NewGuid() };
        payment.Post();
        _payments.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);

        Func<Task> act = () => _sut.Handle(
            new UpdateVendorPaymentCommand(payment.Id, DateTime.UtcNow, 500m, "TRY"), default);

        await act.Should().ThrowAsync<VendorPaymentImmutableException>();
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_blocks_a_voided_payment()
    {
        var payment = new VendorPayment(Guid.NewGuid(), "Acme", "VPAY-1", DateTime.UtcNow, 1000m, "TRY") { Id = Guid.NewGuid() };
        payment.Void("test");
        _payments.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);

        Func<Task> act = () => _sut.Handle(
            new UpdateVendorPaymentCommand(payment.Id, DateTime.UtcNow, 500m, "TRY"), default);

        await act.Should().ThrowAsync<VendorPaymentImmutableException>();
    }

    [Fact]
    public async Task Update_rejects_non_positive_amount()
    {
        Func<Task> act = () => _sut.Handle(
            new UpdateVendorPaymentCommand(Guid.NewGuid(), DateTime.UtcNow, 0m, "TRY"), default);

        await act.Should().ThrowAsync<StockMovementValidationException>();
    }

    [Fact]
    public async Task Update_mutates_an_unposted_draft_payment()
    {
        var payment = new VendorPayment(Guid.NewGuid(), "Acme", "VPAY-1", DateTime.UtcNow, 1000m, "TRY") { Id = Guid.NewGuid() };
        _payments.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);

        var dto = await _sut.Handle(
            new UpdateVendorPaymentCommand(payment.Id, DateTime.UtcNow, 750m, "TRY"), default);

        dto.Amount.Should().Be(750m);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

public class ApplyVendorPaymentHandlerTests
{
    private readonly IVendorPaymentRepository _payments = Substitute.For<IVendorPaymentRepository>();
    private readonly IVendorBillRepository _bills = Substitute.For<IVendorBillRepository>();
    private readonly IVendorPaymentApplicationRepository _apps = Substitute.For<IVendorPaymentApplicationRepository>();
    private readonly ICurrentUserAccessor _user = Substitute.For<ICurrentUserAccessor>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly ApplyVendorPaymentHandler _sut;

    public ApplyVendorPaymentHandlerTests()
    {
        _user.UserId.Returns(Guid.NewGuid());
        _sut = new ApplyVendorPaymentHandler(_payments, _bills, _apps, _user, _uow);
    }

    [Fact]
    public async Task Apply_drops_bill_outstanding_amount()
    {
        var payment = new VendorPayment(Guid.NewGuid(), "Acme", "VPAY-1", DateTime.UtcNow, 500m, "TRY") { Id = Guid.NewGuid() };
        var bill = new VendorBill(payment.VendorId, "Acme", "INV-1", DateTime.UtcNow, "TRY", 1000m, 0m) { Id = Guid.NewGuid() };
        bill.Post();
        _payments.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);
        _bills.GetByIdAsync(bill.Id, Arg.Any<CancellationToken>()).Returns(bill);

        var dto = await _sut.Handle(new ApplyVendorPaymentCommand(payment.Id, bill.Id, 400m), default);

        dto.AppliedAmount.Should().Be(400m);
        payment.AppliedAmount.Should().Be(400m);
        bill.AmountPaid.Should().Be(400m);
        bill.AmountDue.Should().Be(600m);
        bill.Status.Should().Be(VendorBillStatus.PartiallyPaid);
        await _apps.Received(1).AddAsync(Arg.Any<VendorPaymentApplication>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Apply_full_marks_bill_paid()
    {
        var payment = new VendorPayment(Guid.NewGuid(), "Acme", "VPAY-1", DateTime.UtcNow, 1000m, "TRY") { Id = Guid.NewGuid() };
        var bill = new VendorBill(payment.VendorId, "Acme", "INV-1", DateTime.UtcNow, "TRY", 1000m, 0m) { Id = Guid.NewGuid() };
        bill.Post();
        _payments.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);
        _bills.GetByIdAsync(bill.Id, Arg.Any<CancellationToken>()).Returns(bill);

        await _sut.Handle(new ApplyVendorPaymentCommand(payment.Id, bill.Id, 1000m), default);

        bill.Status.Should().Be(VendorBillStatus.Paid);
        bill.AmountDue.Should().Be(0m);
        payment.UnappliedAmount.Should().Be(0m);
    }

    [Fact]
    public async Task Apply_blocks_when_payment_unapplied_is_insufficient()
    {
        var payment = new VendorPayment(Guid.NewGuid(), "Acme", "VPAY-1", DateTime.UtcNow, 100m, "TRY") { Id = Guid.NewGuid() };
        var bill = new VendorBill(payment.VendorId, "Acme", "INV-1", DateTime.UtcNow, "TRY", 1000m, 0m) { Id = Guid.NewGuid() };
        bill.Post();
        _payments.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);
        _bills.GetByIdAsync(bill.Id, Arg.Any<CancellationToken>()).Returns(bill);

        Func<Task> act = () => _sut.Handle(new ApplyVendorPaymentCommand(payment.Id, bill.Id, 500m), default);
        await act.Should().ThrowAsync<VendorPaymentOverApplicationException>();
    }

    [Fact]
    public async Task Apply_blocks_when_amount_exceeds_bill_due()
    {
        var payment = new VendorPayment(Guid.NewGuid(), "Acme", "VPAY-1", DateTime.UtcNow, 5000m, "TRY") { Id = Guid.NewGuid() };
        var bill = new VendorBill(payment.VendorId, "Acme", "INV-1", DateTime.UtcNow, "TRY", 200m, 0m) { Id = Guid.NewGuid() };
        bill.Post();
        _payments.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);
        _bills.GetByIdAsync(bill.Id, Arg.Any<CancellationToken>()).Returns(bill);

        Func<Task> act = () => _sut.Handle(new ApplyVendorPaymentCommand(payment.Id, bill.Id, 300m), default);
        await act.Should().ThrowAsync<VendorPaymentOverApplicationException>();
    }

    [Fact]
    public async Task Apply_rejects_cross_vendor_attempt()
    {
        var payment = new VendorPayment(Guid.NewGuid(), "Acme", "VPAY-1", DateTime.UtcNow, 500m, "TRY") { Id = Guid.NewGuid() };
        var bill = new VendorBill(Guid.NewGuid(), "OtherVendor", "INV-1", DateTime.UtcNow, "TRY", 1000m, 0m) { Id = Guid.NewGuid() };
        bill.Post();
        _payments.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);
        _bills.GetByIdAsync(bill.Id, Arg.Any<CancellationToken>()).Returns(bill);

        Func<Task> act = () => _sut.Handle(new ApplyVendorPaymentCommand(payment.Id, bill.Id, 100m), default);
        await act.Should().ThrowAsync<VendorPaymentBillMismatchException>();
    }

    [Fact]
    public async Task Apply_rejects_currency_mismatch()
    {
        var payment = new VendorPayment(Guid.NewGuid(), "Acme", "VPAY-1", DateTime.UtcNow, 500m, "USD") { Id = Guid.NewGuid() };
        var bill = new VendorBill(payment.VendorId, "Acme", "INV-1", DateTime.UtcNow, "TRY", 1000m, 0m) { Id = Guid.NewGuid() };
        bill.Post();
        _payments.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);
        _bills.GetByIdAsync(bill.Id, Arg.Any<CancellationToken>()).Returns(bill);

        Func<Task> act = () => _sut.Handle(new ApplyVendorPaymentCommand(payment.Id, bill.Id, 100m), default);
        await act.Should().ThrowAsync<VendorPaymentBillMismatchException>();
    }

    [Fact]
    public async Task Apply_rejects_draft_or_cancelled_bill()
    {
        var payment = new VendorPayment(Guid.NewGuid(), "Acme", "VPAY-1", DateTime.UtcNow, 500m, "TRY") { Id = Guid.NewGuid() };
        var draftBill = new VendorBill(payment.VendorId, "Acme", "INV-1", DateTime.UtcNow, "TRY", 1000m, 0m) { Id = Guid.NewGuid() };
        _payments.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);
        _bills.GetByIdAsync(draftBill.Id, Arg.Any<CancellationToken>()).Returns(draftBill);

        Func<Task> act = () => _sut.Handle(new ApplyVendorPaymentCommand(payment.Id, draftBill.Id, 100m), default);
        await act.Should().ThrowAsync<VendorPaymentBillMismatchException>();
    }

    [Fact]
    public async Task Apply_rejects_voided_payment()
    {
        var payment = new VendorPayment(Guid.NewGuid(), "Acme", "VPAY-1", DateTime.UtcNow, 500m, "TRY") { Id = Guid.NewGuid() };
        payment.Void("test");
        var bill = new VendorBill(payment.VendorId, "Acme", "INV-1", DateTime.UtcNow, "TRY", 1000m, 0m) { Id = Guid.NewGuid() };
        bill.Post();
        _payments.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);
        _bills.GetByIdAsync(bill.Id, Arg.Any<CancellationToken>()).Returns(bill);

        Func<Task> act = () => _sut.Handle(new ApplyVendorPaymentCommand(payment.Id, bill.Id, 100m), default);
        await act.Should().ThrowAsync<VendorPaymentAlreadyVoidedException>();
    }
}

public class VendorBillReverseRecordedPaymentTests
{
    [Fact]
    public void Reversing_full_payment_returns_bill_to_posted()
    {
        var b = new VendorBill(Guid.NewGuid(), "Acme", "INV-1", DateTime.UtcNow, "TRY", 1000m, 0m);
        b.Post();
        b.RecordPayment(1000m);
        b.Status.Should().Be(VendorBillStatus.Paid);

        b.ReverseRecordedPayment(1000m);

        b.AmountPaid.Should().Be(0m);
        b.AmountDue.Should().Be(1000m);
        b.Status.Should().Be(VendorBillStatus.Posted);
    }

    [Fact]
    public void Reversing_partial_payment_keeps_partially_paid_when_residual_remains()
    {
        var b = new VendorBill(Guid.NewGuid(), "Acme", "INV-1", DateTime.UtcNow, "TRY", 1000m, 0m);
        b.Post();
        b.RecordPayment(800m);
        b.ReverseRecordedPayment(300m);
        b.AmountPaid.Should().Be(500m);
        b.Status.Should().Be(VendorBillStatus.PartiallyPaid);
    }
}

public class VoidVendorPaymentHandlerTests
{
    private readonly IVendorPaymentRepository _payments = Substitute.For<IVendorPaymentRepository>();
    private readonly IVendorBillRepository _bills = Substitute.For<IVendorBillRepository>();
    private readonly IVendorPaymentApplicationRepository _apps = Substitute.For<IVendorPaymentApplicationRepository>();
    private readonly IVendorLedgerRepository _ledger = Substitute.For<IVendorLedgerRepository>();
    private readonly IVendorRepository _vendors = Substitute.For<IVendorRepository>();
    private readonly IGLPostingOutbox _outbox = Substitute.For<IGLPostingOutbox>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly List<GLPostingRequest> _captured = new();
    private readonly VoidVendorPaymentHandler _sut;

    public VoidVendorPaymentHandlerTests()
    {
        _apps.GetByVendorPaymentAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<VendorPaymentApplication>());
        _outbox.WhenForAnyArgs(o => o.EnqueueAsync(default!, default))
            .Do(ci => _captured.Add(ci.Arg<GLPostingRequest>()));
        _sut = new VoidVendorPaymentHandler(_payments, _bills, _apps, _ledger, _vendors, _outbox, _uow);
    }

    [Fact]
    public async Task Voiding_a_regular_payment_reverses_to_accounts_payable()
    {
        var payment = new VendorPayment(Guid.NewGuid(), "Acme", "VPAY-1", DateTime.UtcNow, 1000m, "TRY") { Id = Guid.NewGuid() };
        payment.Post();
        _payments.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);

        await _sut.Handle(new VoidVendorPaymentCommand(payment.Id, "test"), default);

        var req = _captured.Should().ContainSingle().Subject;
        req.SourceType.Should().Be(JournalSourceType.VendorPaymentReversal);
        req.Lines.Single(l => l.Key == GLPostingKey.AccountsPayable).Credit.Should().Be(1000m);
        req.Lines.Should().NotContain(l => l.Key == GLPostingKey.VendorAdvancePaid);
        AssertBalanced(req);
    }

    [Fact]
    public async Task Voiding_an_unoffset_advance_reverses_to_vendor_advance_not_ap()
    {
        var payment = new VendorPayment(Guid.NewGuid(), "Acme", "VADV-1", DateTime.UtcNow, 1000m, "TRY", isAdvance: true)
        {
            Id = Guid.NewGuid(),
        };
        payment.Post();
        _payments.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);

        await _sut.Handle(new VoidVendorPaymentCommand(payment.Id, "test"), default);

        var req = _captured.Should().ContainSingle().Subject;
        req.SourceType.Should().Be(JournalSourceType.VendorAdvancePaidReversal);
        req.Lines.Single(l => l.Key == GLPostingKey.VendorAdvancePaid).Credit.Should().Be(1000m);
        req.Lines.Should().NotContain(l => l.Key == GLPostingKey.AccountsPayable);
        AssertBalanced(req);
    }

    [Fact]
    public async Task Voiding_an_offset_advance_reverses_both_the_offset_and_the_advance()
    {
        var payment = new VendorPayment(Guid.NewGuid(), "Acme", "VADV-2", DateTime.UtcNow, 1000m, "TRY", isAdvance: true)
        {
            Id = Guid.NewGuid(),
        };
        payment.Post();
        payment.RecordApplication(600m);

        var bill = new VendorBill(payment.VendorId, "Acme", "INV-1", DateTime.UtcNow, "TRY", 1000m, 0m) { Id = Guid.NewGuid() };
        bill.Post();
        bill.RecordPayment(600m);

        var app = new VendorPaymentApplication(payment.Id, bill.Id, 600m) { Id = Guid.NewGuid() };

        _payments.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);
        _apps.GetByVendorPaymentAsync(payment.Id, Arg.Any<CancellationToken>())
            .Returns(new List<VendorPaymentApplication> { app });
        _bills.GetByIdAsync(bill.Id, Arg.Any<CancellationToken>()).Returns(bill);

        await _sut.Handle(new VoidVendorPaymentCommand(payment.Id, "test"), default);

        _captured.Should().HaveCount(2);
        _captured.Should().OnlyContain(r => IsBalanced(r));

        var offsetReversal = _captured.Single(r => r.SourceType == JournalSourceType.VendorAdvanceAppliedReversal);
        offsetReversal.SourceDocumentId.Should().Be(app.Id);
        offsetReversal.Lines.Single(l => l.Key == GLPostingKey.VendorAdvancePaid).Debit.Should().Be(600m);
        offsetReversal.Lines.Single(l => l.Key == GLPostingKey.AccountsPayable).Credit.Should().Be(600m);

        var advanceReversal = _captured.Single(r => r.SourceType == JournalSourceType.VendorAdvancePaidReversal);
        advanceReversal.Lines.Single(l => l.Key == GLPostingKey.VendorAdvancePaid).Credit.Should().Be(1000m);
        advanceReversal.Lines.Should().NotContain(l => l.Key == GLPostingKey.AccountsPayable);

        payment.IsVoided.Should().BeTrue();
    }

    private static bool IsBalanced(GLPostingRequest r) =>
        r.Lines.Sum(l => l.Debit) == r.Lines.Sum(l => l.Credit);

    private static void AssertBalanced(GLPostingRequest r) =>
        r.Lines.Sum(l => l.Debit).Should().Be(r.Lines.Sum(l => l.Credit));
}

public class CreateVendorPaymentHandlerTests
{
    private readonly IVendorPaymentRepository _payments = Substitute.For<IVendorPaymentRepository>();
    private readonly IVendorBillRepository _bills = Substitute.For<IVendorBillRepository>();
    private readonly IVendorRepository _vendors = Substitute.For<IVendorRepository>();
    private readonly IVendorLedgerRepository _ledger = Substitute.For<IVendorLedgerRepository>();
    private readonly IDocumentSequenceRepository _sequences = Substitute.For<IDocumentSequenceRepository>();
    private readonly IVendorPaymentApplicationRepository _apps = Substitute.For<IVendorPaymentApplicationRepository>();
    private readonly ICurrentUserAccessor _user = Substitute.For<ICurrentUserAccessor>();
    private readonly IGLPostingOutbox _outbox = Substitute.For<IGLPostingOutbox>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly CreateVendorPaymentHandler _sut;

    public CreateVendorPaymentHandlerTests()
    {
        _sut = new CreateVendorPaymentHandler(_payments, _bills, _vendors, _ledger, _sequences, _apps, _user, _outbox, _uow);
    }

    [Fact]
    public async Task Duplicate_create_with_same_operation_id_replays_the_original_payment()
    {
        var operationId = Guid.NewGuid();
        var existing = new VendorPayment(Guid.NewGuid(), "Acme", "VPAY-1", DateTime.UtcNow, 1000m, "TRY", operationId: operationId)
        {
            Id = Guid.NewGuid(),
        };
        _payments.GetByOperationIdAsync(operationId, Arg.Any<CancellationToken>()).Returns(existing);

        var dto = await _sut.Handle(
            new CreateVendorPaymentCommand(existing.VendorId, 1000m, DateTime.UtcNow, "TRY", OperationId: operationId), default);

        dto.PaymentNumber.Should().Be("VPAY-1");
        await _payments.DidNotReceive().AddAsync(Arg.Any<VendorPayment>(), Arg.Any<CancellationToken>());
        await _sequences.DidNotReceive().ConsumeAsync(Arg.Any<DocumentSequenceType>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_without_operation_id_skips_the_replay_guard()
    {
        _vendors.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Vendor?)null);

        Func<Task> act = () => _sut.Handle(
            new CreateVendorPaymentCommand(Guid.NewGuid(), 1000m, DateTime.UtcNow, "TRY"), default);

        await act.Should().ThrowAsync<VendorNotFoundForPurchaseException>();
        await _payments.DidNotReceive().GetByOperationIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}

public class UpdateVendorBillHandlerTests
{
    private readonly IVendorBillRepository _bills = Substitute.For<IVendorBillRepository>();
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly IPurchaseOrderRepository _orders = Substitute.For<IPurchaseOrderRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly UpdateVendorBillHandler _sut;

    public UpdateVendorBillHandlerTests()
    {
        _sut = new UpdateVendorBillHandler(_bills, _products, _orders, _uow);
    }

    [Fact]
    public async Task Update_mutates_draft_bill()
    {
        var bill = new VendorBill(Guid.NewGuid(), "Acme", "INV-1", DateTime.UtcNow, "TRY", 100m, 18m) { Id = Guid.NewGuid() };
        _bills.GetByIdAsync(bill.Id, Arg.Any<CancellationToken>()).Returns(bill);

        var dto = await _sut.Handle(new UpdateVendorBillCommand(
            bill.Id, "INV-2", DateTime.UtcNow, "TRY", 200m, 36m), default);

        dto.BillNumber.Should().Be("INV-2");
        dto.Subtotal.Should().Be(200m);
        dto.Total.Should().Be(236m);
    }

    [Fact]
    public async Task Update_rejects_posted_bill()
    {
        var bill = new VendorBill(Guid.NewGuid(), "Acme", "INV-1", DateTime.UtcNow, "TRY", 100m, 18m) { Id = Guid.NewGuid() };
        bill.Post();
        _bills.GetByIdAsync(bill.Id, Arg.Any<CancellationToken>()).Returns(bill);

        Func<Task> act = () => _sut.Handle(new UpdateVendorBillCommand(
            bill.Id, "INV-2", DateTime.UtcNow, "TRY", 200m, 36m), default);
        await act.Should().ThrowAsync<InvalidOrderStatusTransitionException>();
    }

    [Fact]
    public async Task Update_blocks_duplicate_bill_number_for_same_vendor()
    {
        var bill = new VendorBill(Guid.NewGuid(), "Acme", "INV-1", DateTime.UtcNow, "TRY", 100m, 18m) { Id = Guid.NewGuid() };
        _bills.GetByIdAsync(bill.Id, Arg.Any<CancellationToken>()).Returns(bill);
        _bills.BillNumberExistsAsync(bill.VendorId, "INV-9", bill.Id, Arg.Any<CancellationToken>()).Returns(true);

        Func<Task> act = () => _sut.Handle(new UpdateVendorBillCommand(
            bill.Id, "INV-9", DateTime.UtcNow, "TRY", 200m, 36m), default);
        await act.Should().ThrowAsync<DuplicateVendorBillNumberException>();
    }
}
