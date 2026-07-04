using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Application.Tests.IncomingInvoices;

public class IncomingInvoiceEntityTests
{
    private static IncomingInvoice Build() => new(
        ettn: "ETTN-1",
        senderVkn: "1234567890",
        senderName: "Tedarikçi A.Ş.",
        invoiceNumber: "GIB-2026-001",
        issueDate: new DateTime(2026, 7, 1),
        providerName: "nilvera",
        providerStatus: "Delivered");

    [Fact]
    public void New_invoice_starts_in_new_status_with_utc_issue_date()
    {
        var invoice = Build();

        invoice.Status.Should().Be(IncomingInvoiceStatus.New);
        invoice.IssueDate.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Mark_processed_links_vendor_bill_and_stamps_time()
    {
        var invoice = Build();
        var billId = Guid.NewGuid();

        invoice.MarkProcessed(billId);

        invoice.Status.Should().Be(IncomingInvoiceStatus.Processed);
        invoice.LinkedVendorBillId.Should().Be(billId);
        invoice.ProcessedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Processing_twice_throws()
    {
        var invoice = Build();
        invoice.MarkProcessed(Guid.NewGuid());

        var act = () => invoice.MarkProcessed(Guid.NewGuid());

        act.Should().Throw<IncomingInvoiceAlreadyProcessedException>();
    }

    [Fact]
    public void Ignoring_a_processed_invoice_throws()
    {
        var invoice = Build();
        invoice.MarkProcessed(Guid.NewGuid());

        var act = () => invoice.MarkIgnored("late");

        act.Should().Throw<IncomingInvoiceAlreadyProcessedException>();
    }

    [Fact]
    public void Processing_an_ignored_invoice_throws()
    {
        var invoice = Build();
        invoice.MarkIgnored("duplicate");

        var act = () => invoice.MarkProcessed(Guid.NewGuid());

        act.Should().Throw<IncomingInvoiceIgnoredException>();
    }

    [Fact]
    public void Ettn_is_required()
    {
        var act = () => new IncomingInvoice(" ", "123", null, "n", DateTime.UtcNow, "nilvera", null);

        act.Should().Throw<ArgumentException>();
    }
}
