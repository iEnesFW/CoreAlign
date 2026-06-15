using CoreAlign.Application.B2B;
using CoreAlign.Application.Purchasing;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Idempotency;

[Collection(IdempotencyTestCollection.Name)]
public class ApplyVendorPaymentIdempotencyTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid VendorId = Guid.NewGuid();
    private static readonly Guid ActorId = Guid.NewGuid();

    private readonly IVendorPaymentRepository _payments = Substitute.For<IVendorPaymentRepository>();
    private readonly IVendorBillRepository _bills = Substitute.For<IVendorBillRepository>();
    private readonly IVendorPaymentApplicationRepository _applications = Substitute.For<IVendorPaymentApplicationRepository>();
    private readonly ICurrentUserAccessor _currentUser = Substitute.For<ICurrentUserAccessor>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly List<VendorPaymentApplication> _recorded = new();
    private readonly ApplyVendorPaymentHandler _sut;

    public ApplyVendorPaymentIdempotencyTests()
    {
        _currentUser.UserId.Returns(ActorId);
        _applications
            .When(a => a.AddAsync(Arg.Any<VendorPaymentApplication>(), Arg.Any<CancellationToken>()))
            .Do(ci => _recorded.Add(ci.Arg<VendorPaymentApplication>()));
        _applications
            .GetByPaymentAndBillAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ci => _recorded.FirstOrDefault(a =>
                a.VendorPaymentId == ci.ArgAt<Guid>(0) && a.VendorBillId == ci.ArgAt<Guid>(1)));
        _sut = new ApplyVendorPaymentHandler(_payments, _bills, _applications, _currentUser, _uow);
    }

    [Fact]
    public async Task SinglePaymentApplication_PostsTheAppliedAmountOnce()
    {
        var payment = BuildPayment(amount: 100m);
        var bill = BuildPostedBill(total: 200m);
        _payments.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);
        _bills.GetByIdAsync(bill.Id, Arg.Any<CancellationToken>()).Returns(bill);

        var command = new ApplyVendorPaymentCommand(payment.Id, bill.Id, 50m);
        var result = await _sut.Handle(command, default);

        result.AppliedAmount.Should().Be(50m);
        payment.AppliedAmount.Should().Be(50m);
        bill.AmountPaid.Should().Be(50m);
        await _applications.Received(1).AddAsync(Arg.Any<VendorPaymentApplication>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RetryWithSameVendorPaymentAndBill_IsIdempotent_DoesNotDoubleApply()
    {
        var payment = BuildPayment(amount: 100m);
        var bill = BuildPostedBill(total: 200m);
        _payments.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);
        _bills.GetByIdAsync(bill.Id, Arg.Any<CancellationToken>()).Returns(bill);

        var command = new ApplyVendorPaymentCommand(payment.Id, bill.Id, 50m);
        var first = await _sut.Handle(command, default);
        var retry = await _sut.Handle(command, default);

        await _applications.Received(1).AddAsync(Arg.Any<VendorPaymentApplication>(), Arg.Any<CancellationToken>());
        payment.AppliedAmount.Should().Be(50m, "duplicate application MUST be ignored on retry of the same (payment, bill)");
        bill.AmountPaid.Should().Be(50m);
        retry.Id.Should().Be(first.Id, "retry returns the existing application, not a new one");
        retry.AppliedAmount.Should().Be(50m);
    }

    [Fact]
    public async Task SamePaymentAppliedToDifferentBill_StillRecordsASecondApplication()
    {
        var payment = BuildPayment(amount: 200m);
        var billA = BuildPostedBill(total: 200m);
        var billB = BuildPostedBill(total: 200m);
        _payments.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);
        _bills.GetByIdAsync(billA.Id, Arg.Any<CancellationToken>()).Returns(billA);
        _bills.GetByIdAsync(billB.Id, Arg.Any<CancellationToken>()).Returns(billB);

        await _sut.Handle(new ApplyVendorPaymentCommand(payment.Id, billA.Id, 50m), default);
        var second = await _sut.Handle(new ApplyVendorPaymentCommand(payment.Id, billB.Id, 50m), default);

        await _applications.Received(2).AddAsync(Arg.Any<VendorPaymentApplication>(), Arg.Any<CancellationToken>());
        payment.AppliedAmount.Should().Be(100m, "a payment may apply to multiple distinct bills");
        billA.AmountPaid.Should().Be(50m);
        billB.AmountPaid.Should().Be(50m);
        second.AppliedAmount.Should().Be(50m);
    }

    private static VendorPayment BuildPayment(decimal amount) => new(
        VendorId,
        "Acme Vendor",
        $"VP-{Guid.NewGuid():N}",
        DateTime.UtcNow.Date,
        amount,
        "TRY")
    {
        Id = Guid.NewGuid(),
        TenantId = TenantId,
    };

    private static VendorBill BuildPostedBill(decimal total)
    {
        var bill = new VendorBill(
            VendorId,
            "Acme Vendor",
            $"VB-{Guid.NewGuid():N}",
            DateTime.UtcNow.Date,
            "TRY",
            subtotal: total,
            taxAmount: 0m)
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
        };
        bill.Post();
        return bill;
    }
}
