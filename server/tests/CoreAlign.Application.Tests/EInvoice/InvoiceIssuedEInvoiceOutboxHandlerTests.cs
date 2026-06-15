using System.Text.Json;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Application.EInvoice;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoreAlign.Application.Tests.EInvoice;

public class InvoiceIssuedEInvoiceOutboxHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();

    private readonly IInvoiceRepository _invoices = Substitute.For<IInvoiceRepository>();
    private readonly ICustomerRepository _customers = Substitute.For<ICustomerRepository>();
    private readonly IElectronicInvoiceGateway _gateway = Substitute.For<IElectronicInvoiceGateway>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task Submits_invoice_and_persists_remote_uuid_on_success()
    {
        var invoice = BuildInvoice();
        _invoices.GetWithLinesAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);
        _customers.GetByIdAsync(invoice.CustomerId, Arg.Any<CancellationToken>())
            .Returns(new Customer("Demo Müşteri") { Id = CustomerId, TenantId = TenantId });
        _gateway.GatewayName.Returns("Stub");
        _gateway.SubmitAsync(Arg.Any<EInvoiceSubmissionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new EInvoiceSubmissionResult("STUB-ABC123", "Submitted", null, null));

        var sut = new InvoiceIssuedEInvoiceOutboxHandler(_invoices, _customers, _gateway, _unitOfWork, NullLogger<InvoiceIssuedEInvoiceOutboxHandler>.Instance);
        var payload = JsonSerializer.Serialize(new EInvoiceSubmissionRequestedPayload(TenantId, invoice.Id));

        var result = await sut.HandleAsync(payload, default);

        result.Outcome.Should().Be(OutboxHandlerOutcome.Processed);
        result.ResultOrError.Should().StartWith("Submitted:STUB-");
        invoice.EInvoiceUuid.Should().Be("STUB-ABC123");
        invoice.EInvoiceStatus.Should().Be("Submitted");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Marks_invoice_failed_when_gateway_reports_failure()
    {
        var invoice = BuildInvoice();
        _invoices.GetWithLinesAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);
        _customers.GetByIdAsync(invoice.CustomerId, Arg.Any<CancellationToken>())
            .Returns(new Customer("Demo Müşteri") { Id = CustomerId, TenantId = TenantId });
        _gateway.GatewayName.Returns("Stub");
        _gateway.SubmitAsync(Arg.Any<EInvoiceSubmissionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new EInvoiceSubmissionResult(null, "Failed", "Schema error", null));

        var sut = new InvoiceIssuedEInvoiceOutboxHandler(_invoices, _customers, _gateway, _unitOfWork, NullLogger<InvoiceIssuedEInvoiceOutboxHandler>.Instance);
        var payload = JsonSerializer.Serialize(new EInvoiceSubmissionRequestedPayload(TenantId, invoice.Id));

        var result = await sut.HandleAsync(payload, default);

        result.Outcome.Should().Be(OutboxHandlerOutcome.Failed);
        invoice.EInvoiceStatus.Should().Be("Failed");
        invoice.EInvoiceUuid.Should().BeNull();
    }

    [Fact]
    public async Task Skips_when_invoice_already_has_remote_uuid()
    {
        var invoice = BuildInvoice();
        invoice.RegisterEInvoice("EXISTING-UUID", "Submitted", null);
        _invoices.GetWithLinesAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);

        var sut = new InvoiceIssuedEInvoiceOutboxHandler(_invoices, _customers, _gateway, _unitOfWork, NullLogger<InvoiceIssuedEInvoiceOutboxHandler>.Instance);
        var payload = JsonSerializer.Serialize(new EInvoiceSubmissionRequestedPayload(TenantId, invoice.Id));

        var result = await sut.HandleAsync(payload, default);

        result.Outcome.Should().Be(OutboxHandlerOutcome.Processed);
        result.ResultOrError.Should().Be("AlreadySubmitted");
        await _gateway.DidNotReceive().SubmitAsync(Arg.Any<EInvoiceSubmissionRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Fails_when_invoice_not_found()
    {
        _invoices.GetWithLinesAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Invoice?)null);

        var sut = new InvoiceIssuedEInvoiceOutboxHandler(_invoices, _customers, _gateway, _unitOfWork, NullLogger<InvoiceIssuedEInvoiceOutboxHandler>.Instance);
        var payload = JsonSerializer.Serialize(new EInvoiceSubmissionRequestedPayload(TenantId, Guid.NewGuid()));

        var result = await sut.HandleAsync(payload, default);

        result.Outcome.Should().Be(OutboxHandlerOutcome.Failed);
    }

    private static Invoice BuildInvoice()
    {
        var invoice = new Invoice("INV-0001", CustomerId, "Demo Müşteri", "TRY")
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
        };
        var line = new InvoiceLine(Guid.NewGuid(), "SKU-1", "Widget", 1m, 100m)
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
        };
        line.SetLineNumber(1);
        line.ApplyPricing(
            quantity: 1m,
            unitPrice: 100m,
            lineDiscountPercent: 0m,
            lineDiscountAmount: 0m,
            taxRatePercent: 20m,
            taxRateId: null,
            isTaxInclusive: false,
            withholdingRatePercent: 0m,
            uomId: null,
            uomCode: "C62",
            description: null,
            revenueAccountCode: null,
            costCenter: null,
            project: null,
            originOrderLineId: null);
        invoice.ReplaceLines(new[] { line });
        invoice.Issue("INV-0001");
        return invoice;
    }
}
