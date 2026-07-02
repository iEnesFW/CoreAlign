using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Tests.Payments;

public class PaymentDateUtcNormalizationTests
{
    private static Payment BuildPayment(DateTime paymentDate) => new(
        paymentNumber: "PAY-0001",
        customerId: Guid.NewGuid(),
        customerNameSnapshot: "Customer",
        direction: PaymentDirection.CustomerReceipt,
        paymentDate: paymentDate,
        method: PaymentMethod.BankTransfer,
        amount: 100m,
        currency: "TRY");

    [Fact]
    public void Creating_payment_with_unspecified_kind_date_normalizes_to_utc()
    {
        var unspecified = new DateTime(2026, 7, 2, 0, 0, 0, DateTimeKind.Unspecified);

        var payment = BuildPayment(unspecified);

        payment.PaymentDate.Kind.Should().Be(DateTimeKind.Utc);
        payment.PostingDate.Kind.Should().Be(DateTimeKind.Utc);
        payment.PaymentDate.Should().Be(new DateTime(2026, 7, 2, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void Updating_payment_details_normalizes_all_dates_to_utc()
    {
        var payment = BuildPayment(DateTime.UtcNow);
        var unspecifiedDate = new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Unspecified);
        var unspecifiedDue = new DateTime(2026, 8, 15, 0, 0, 0, DateTimeKind.Unspecified);

        payment.UpdateDetails(
            paymentDate: unspecifiedDate,
            postingDate: unspecifiedDate.Date,
            method: PaymentMethod.Check,
            amount: 250m,
            exchangeRate: 1m,
            bankAccountInfo: null,
            referenceNumber: null,
            checkNumber: "CHK-1",
            checkDueDate: unspecifiedDue,
            notes: null);

        payment.PaymentDate.Kind.Should().Be(DateTimeKind.Utc);
        payment.PostingDate.Kind.Should().Be(DateTimeKind.Utc);
        payment.CheckDueDate!.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Updating_payment_details_with_null_check_due_date_keeps_it_null()
    {
        var payment = BuildPayment(DateTime.UtcNow);

        payment.UpdateDetails(
            paymentDate: DateTime.UtcNow,
            postingDate: DateTime.UtcNow.Date,
            method: PaymentMethod.BankTransfer,
            amount: 100m,
            exchangeRate: 1m,
            bankAccountInfo: null,
            referenceNumber: null,
            checkNumber: null,
            checkDueDate: null,
            notes: null);

        payment.CheckDueDate.Should().BeNull();
    }
}
