using CoreAlign.Domain.Entities;

namespace CoreAlign.Application.Tests.Invoices;

public class InvoiceEInvoiceStatusTests
{
    private static Invoice BuildInvoice() => new("INV-1", Guid.NewGuid(), "Müşteri", "TRY");

    [Fact]
    public void Submitted_status_is_normalized_and_stamps_sent_at()
    {
        var invoice = BuildInvoice();

        var changed = invoice.ApplyEInvoiceStatus("submitted", null, null);

        changed.Should().BeTrue();
        invoice.EInvoiceStatus.Should().Be(EInvoiceStatuses.Submitted);
        invoice.EInvoiceSentAtUtc.Should().NotBeNull();
        invoice.EInvoiceLastSyncUtc.Should().NotBeNull();
    }

    [Fact]
    public void Terminal_status_does_not_regress_to_earlier_state()
    {
        var invoice = BuildInvoice();
        invoice.ApplyEInvoiceStatus(EInvoiceStatuses.Submitted, null, null);
        invoice.ApplyEInvoiceStatus(EInvoiceStatuses.Accepted, "1300", null);

        var changed = invoice.ApplyEInvoiceStatus(EInvoiceStatuses.Submitted, null, null);

        changed.Should().BeFalse();
        invoice.EInvoiceStatus.Should().Be(EInvoiceStatuses.Accepted);
        invoice.EInvoiceGibStatusCode.Should().Be("1300");
    }

    [Fact]
    public void Rejected_status_records_gib_code_and_reason()
    {
        var invoice = BuildInvoice();
        invoice.ApplyEInvoiceStatus(EInvoiceStatuses.Submitted, null, null);

        invoice.ApplyEInvoiceStatus(EInvoiceStatuses.Rejected, "1210", "Şema doğrulama hatası");

        invoice.EInvoiceStatus.Should().Be(EInvoiceStatuses.Rejected);
        invoice.EInvoiceGibStatusCode.Should().Be("1210");
        invoice.EInvoiceRejectReason.Should().Be("Şema doğrulama hatası");
    }

    [Fact]
    public void Register_e_invoice_preserves_existing_uuid_and_pdf_when_nulls_arrive()
    {
        var invoice = BuildInvoice();
        invoice.RegisterEInvoice("ETTN-123", EInvoiceStatuses.Submitted, "/pdf/1.pdf");

        invoice.RegisterEInvoice(null, EInvoiceStatuses.Failed, null);

        invoice.EInvoiceUuid.Should().Be("ETTN-123");
        invoice.EInvoicePdfPath.Should().Be("/pdf/1.pdf");
        invoice.EInvoiceStatus.Should().Be(EInvoiceStatuses.Failed);
    }

    [Fact]
    public void Failed_status_is_retryable_and_can_progress_to_accepted()
    {
        var invoice = BuildInvoice();
        invoice.ApplyEInvoiceStatus(EInvoiceStatuses.Failed, null, "timeout");

        var changed = invoice.ApplyEInvoiceStatus(EInvoiceStatuses.Accepted, "1300", null);

        changed.Should().BeTrue();
        invoice.EInvoiceStatus.Should().Be(EInvoiceStatuses.Accepted);
    }
}
