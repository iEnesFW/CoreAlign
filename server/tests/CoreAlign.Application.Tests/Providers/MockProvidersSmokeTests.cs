using CoreAlign.Application.Providers.BankReconciliation;
using CoreAlign.Application.Providers.CadImport;
using CoreAlign.Application.Providers.Calendar;
using CoreAlign.Application.Providers.CncExport;
using CoreAlign.Application.Providers.EFatura;
using CoreAlign.Application.Providers.Export;
using CoreAlign.Application.Providers.Freight;
using CoreAlign.Application.Providers.LabelPrinter;
using CoreAlign.Application.Providers.LaserMeter;
using CoreAlign.Application.Providers.Payment;
using CoreAlign.Infrastructure.Providers.BankReconciliation;
using CoreAlign.Infrastructure.Providers.CadImport.Mock;
using CoreAlign.Infrastructure.Providers.Calendar.Mock;
using CoreAlign.Infrastructure.Providers.CncExport.Mock;
using CoreAlign.Infrastructure.Providers.EFatura;
using CoreAlign.Infrastructure.Providers.Export.Mock;
using CoreAlign.Infrastructure.Providers.Freight.Mock;
using CoreAlign.Infrastructure.Providers.LabelPrinter.Mock;
using CoreAlign.Infrastructure.Providers.LaserMeter;
using CoreAlign.Infrastructure.Providers.Payment;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAlign.Application.Tests.Providers;

public class MockProvidersSmokeTests
{
    [Fact]
    public async Task MockEFaturaProvider_IssueAsync_returns_uuid_with_mock_prefix()
    {
        var sut = new MockEFaturaProvider();
        var doc = new EFaturaDocument(
            EFaturaDocumentType.Invoice,
            "INV-001",
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            "1234567890",
            "Buyer Co",
            new[] { new EFaturaLine(1m, "Item", 100m, 0.20m) },
            "TRY",
            120m);
        var request = new EFaturaIssueRequest(doc, UblXmlBase64: "x");

        var result = await sut.IssueAsync(request, CancellationToken.None);

        result.Uuid.Should().StartWith("MOCK-");
    }

    [Fact]
    public async Task MockPaymentProvider_CreateLinkAsync_returns_link_with_mock_payment_url()
    {
        var sut = new MockPaymentProvider();
        var req = new PaymentIntentRequest(150m, "TRY", "ORD-1", "Buyer", "buyer@example.com");
        var opts = new PaymentLinkOptions(15, "https://callback");

        var result = await sut.CreateLinkAsync(req, opts, CancellationToken.None);

        result.LinkUrl.Should().StartWith("https://mock.payment");
    }

    [Fact]
    public void ManualLaserMeterAdapter_uses_manual_entry_transport()
    {
        var sut = new ManualLaserMeterAdapter();

        sut.Transport.Should().Be(LaserMeterTransport.ManualEntry);
    }

    [Fact]
    public async Task MockLabelPrinter_RenderAsync_returns_label_with_raw_bytes()
    {
        var sut = new MockLabelPrinter();
        var template = new LabelTemplate("SKU-LBL", LabelPrinterFormat.PdfRoll62x100, "{Sku}", 62, 100);
        var variables = new Dictionary<string, object?> { ["Sku"] = "ABC-1" };

        var result = await sut.RenderAsync(template, variables, CancellationToken.None);

        result.Should().NotBeNull();
        result.RawBytes.Should().NotBeNullOrEmpty();
        result.Bytes.Should().Be(result.RawBytes.Length);
    }

    [Fact]
    public async Task MockCncExporter_ExportAsync_returns_dxf_stub_bytes()
    {
        var sut = new MockCncExportProvider();
        var plan = new CuttingPlanSnapshot(
            Guid.NewGuid(),
            new[] { new CncPiece(1000, 200, "P1") },
            "AL-6063",
            1);
        var opts = new CncExportOptions(true, true, 3m);

        var result = await sut.ExportAsync(plan, opts, CancellationToken.None);

        result.Format.Should().Be(CncExportFormat.Dxf);
        result.RawBytes.Should().NotBeNullOrEmpty();
        System.Text.Encoding.ASCII.GetString(result.RawBytes).Should().Contain("SECTION");
    }

    [Fact]
    public async Task MockCadImporter_ImportAsync_returns_two_run_candidates()
    {
        var sut = new MockCadImporter();
        using var stream = new MemoryStream(new byte[] { 0x00 });
        var options = new CadImportOptions(true, null, null);

        var result = await sut.ImportAsync(stream, "drawing.dxf", options, CancellationToken.None);

        result.RunCandidates.Should().HaveCount(2);
    }

    [Fact]
    public async Task MockFreightTrackingProvider_TrackAsync_returns_tracking_number_and_status()
    {
        var sut = new MockFreightTrackingProvider();
        var creds = new FreightCredentials("key", "client");

        var result = await sut.TrackAsync("MOCK-TR-001", creds, CancellationToken.None);

        result.TrackingNumber.Should().Be("MOCK-TR-001");
        result.Status.Should().Be(FreightStatus.Created);
        result.Events.Should().NotBeEmpty();
    }

    [Fact]
    public async Task MockBankReconciliationProvider_ParseAsync_returns_two_transactions()
    {
        var sut = new MockBankReconciliationProvider();
        using var stream = new MemoryStream(new byte[] { 0x00 });

        var result = await sut.ParseAsync(stream, creds: null, CancellationToken.None);

        result.Transactions.Should().HaveCount(2);
    }

    [Fact]
    public async Task MockCalendarProvider_PushAsync_returns_external_id_with_mock_cal_prefix()
    {
        var sut = new MockCalendarProvider();
        var ev = new CalendarEvent(
            null,
            "Survey",
            new DateTime(2026, 1, 1, 9, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc),
            null,
            null,
            Array.Empty<string>());
        var creds = new CalendarCredentials("tok", "refresh", "cal-1");

        var result = await sut.PushAsync(ev, creds, CancellationToken.None);

        result.ExternalId.Should().StartWith("mock-cal-");
        result.Success.Should().BeTrue();
    }

    [Fact]
    public void ExportFormatRegistry_Find_returns_null_when_no_exporter_registered()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var sut = new ExportFormatRegistry(services);

        var result = sut.Find<UnregisteredExportDoc>(ExportFormat.Xlsx);

        result.Should().BeNull();
    }

    private sealed record UnregisteredExportDoc(string Name);
}
