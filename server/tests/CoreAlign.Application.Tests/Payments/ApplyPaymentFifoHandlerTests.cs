using CoreAlign.Application.Payments.Commands;
using CoreAlign.Application.Payments.Handlers;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Payments;

public class ApplyPaymentFifoHandlerTests
{
    private static readonly Guid CustomerId = Guid.NewGuid();

    private readonly IPaymentRepository _payments = Substitute.For<IPaymentRepository>();
    private readonly IInvoiceRepository _invoices = Substitute.For<IInvoiceRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private ApplyPaymentFifoHandler CreateHandler() => new(_payments, _invoices, _uow);

    [Fact]
    public async Task Applies_payment_to_oldest_invoices_first_until_exhausted()
    {
        var payment = BuildConfirmedPayment(100m);
        var inv1 = BuildIssuedInvoice(40m);
        var inv2 = BuildIssuedInvoice(50m);
        var inv3 = BuildIssuedInvoice(30m);

        _payments.GetWithApplicationsAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);
        _invoices.GetOpenForCustomerAsync(CustomerId, Arg.Any<CancellationToken>())
            .Returns(new List<Invoice> { inv1, inv2, inv3 });
        _invoices.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, Invoice> { [inv1.Id] = inv1, [inv2.Id] = inv2, [inv3.Id] = inv3 });

        await CreateHandler().Handle(new ApplyPaymentFifoCommand(payment.Id), CancellationToken.None);

        payment.AppliedAmount.Should().Be(100m);
        payment.UnappliedAmount.Should().Be(0m);
        payment.Applications.Should().HaveCount(3);
        inv1.AmountDue.Should().Be(0m, "oldest invoice fully settled first");
        inv2.AmountDue.Should().Be(0m, "second-oldest fully settled");
        inv3.AmountDue.Should().Be(20m, "only the remaining 10 of the payment reaches the newest invoice");
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Leaves_overpayment_unapplied_when_open_invoices_are_exhausted()
    {
        var payment = BuildConfirmedPayment(100m);
        var inv = BuildIssuedInvoice(60m);

        _payments.GetWithApplicationsAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);
        _invoices.GetOpenForCustomerAsync(CustomerId, Arg.Any<CancellationToken>())
            .Returns(new List<Invoice> { inv });
        _invoices.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, Invoice> { [inv.Id] = inv });

        await CreateHandler().Handle(new ApplyPaymentFifoCommand(payment.Id), CancellationToken.None);

        inv.AmountDue.Should().Be(0m);
        payment.AppliedAmount.Should().Be(60m);
        payment.UnappliedAmount.Should().Be(40m, "leftover stays on the payment for later carry-forward");
        payment.Applications.Should().ContainSingle();
    }

    private static Payment BuildConfirmedPayment(decimal amount)
    {
        var payment = new Payment(
            paymentNumber: $"PAY-{Guid.NewGuid():N}",
            customerId: CustomerId,
            customerNameSnapshot: "Acme",
            direction: PaymentDirection.CustomerReceipt,
            paymentDate: DateTime.UtcNow.Date,
            method: PaymentMethod.BankTransfer,
            amount: amount,
            currency: "TRY")
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
        };
        payment.Confirm(null);
        payment.ClearDomainEvents();
        return payment;
    }

    private static Invoice BuildIssuedInvoice(decimal total)
    {
        var invoice = new Invoice($"INV-{Guid.NewGuid():N}".Substring(0, 12), CustomerId, "Acme", "TRY")
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
        };
        invoice.Lines.Add(new InvoiceLine("SKU-FIX", "Fixture", null, quantity: 1m, unitPrice: total));
        invoice.Recalculate();
        invoice.Issue(invoice.InvoiceNumber);
        return invoice;
    }
}
