using CoreAlign.Application.B2B;
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
