using CoreAlign.Application.Invoices.Commands;
using CoreAlign.Application.Invoices.Handlers;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Invoices;

public class IssueCreditNoteCommandHandlerTests
{
    private readonly IInvoiceRepository _invoiceRepository = Substitute.For<IInvoiceRepository>();
    private readonly IDocumentSequenceRepository _sequenceRepository = Substitute.For<IDocumentSequenceRepository>();
    private readonly IAccountingPeriodRepository _periodRepository = Substitute.For<IAccountingPeriodRepository>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IssueCreditNoteCommandHandler _sut;

    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid InvoiceId = Guid.NewGuid();

    public IssueCreditNoteCommandHandlerTests()
    {
        _sequenceRepository
            .ConsumeAsync(DocumentSequenceType.CreditNoteNumber, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns("CN-TEST-0001");
        _invoiceRepository
            .GetCreditNotesForInvoiceAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Invoice>());
        _sut = new IssueCreditNoteCommandHandler(_invoiceRepository, _sequenceRepository, _periodRepository, _tenantContext);
    }

    [Fact]
    public async Task Issues_credit_note_with_selected_lines()
    {
        var invoice = BuildIssuedInvoice();
        var line1 = invoice.Lines.ElementAt(0);
        _invoiceRepository.GetWithLinesAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);

        var result = await _sut.Handle(new IssueCreditNoteCommand(
            invoice.Id,
            new[] { new IssueCreditNoteLineInput(line1.Id, 2m) }), default);

        result.Type.Should().Be(InvoiceType.CreditNote);
        result.OriginInvoiceId.Should().Be(invoice.Id);
        result.Lines.Should().HaveCount(1);
        result.Lines[0].Quantity.Should().Be(2m);
        result.Status.Should().Be(InvoiceStatus.Issued);
        await _invoiceRepository.Received(1).AddAsync(Arg.Any<Invoice>(), Arg.Any<CancellationToken>());
        invoice.CreditNoteId.Should().BeNull();
        result.OriginInvoiceId.Should().Be(invoice.Id);
    }

    [Fact]
    public async Task Throws_when_origin_invoice_not_issued()
    {
        var invoice = BuildDraftInvoice();
        _invoiceRepository.GetWithLinesAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);

        Func<Task> act = () => _sut.Handle(new IssueCreditNoteCommand(
            invoice.Id,
            new[] { new IssueCreditNoteLineInput(invoice.Lines.First().Id, 1m) }), default);

        await act.Should().ThrowAsync<InvoiceStatusTransitionException>();
    }

    [Fact]
    public async Task Throws_when_invoice_not_found()
    {
        _invoiceRepository.GetWithLinesAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Invoice?)null);

        Func<Task> act = () => _sut.Handle(new IssueCreditNoteCommand(
            Guid.NewGuid(),
            new[] { new IssueCreditNoteLineInput(Guid.NewGuid(), 1m) }), default);

        await act.Should().ThrowAsync<InvoiceNotFoundException>();
    }

    [Fact]
    public async Task Throws_when_quantity_exceeds_remaining_after_prior_credit()
    {
        var invoice = BuildIssuedInvoice();
        var line = invoice.Lines.First();
        var priorCredit = BuildCreditNote(invoice, line, 4m);
        _invoiceRepository.GetWithLinesAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);
        _invoiceRepository.GetCreditNotesForInvoiceAsync(invoice.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { priorCredit });

        Func<Task> act = () => _sut.Handle(new IssueCreditNoteCommand(
            invoice.Id,
            new[] { new IssueCreditNoteLineInput(line.Id, 2m) }), default);

        await act.Should().ThrowAsync<CannotIssueCreditNoteException>();
    }

    [Fact]
    public async Task Throws_when_line_not_part_of_invoice()
    {
        var invoice = BuildIssuedInvoice();
        _invoiceRepository.GetWithLinesAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);

        Func<Task> act = () => _sut.Handle(new IssueCreditNoteCommand(
            invoice.Id,
            new[] { new IssueCreditNoteLineInput(Guid.NewGuid(), 1m) }), default);

        await act.Should().ThrowAsync<CannotIssueCreditNoteException>();
    }

    [Fact]
    public async Task Emits_invoice_issued_event_marked_as_credit_note()
    {
        var invoice = BuildIssuedInvoice();
        var line = invoice.Lines.First();
        _invoiceRepository.GetWithLinesAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);

        Invoice? created = null;
        await _invoiceRepository.AddAsync(Arg.Do<Invoice>(i => created = i), Arg.Any<CancellationToken>());

        await _sut.Handle(new IssueCreditNoteCommand(invoice.Id, new[] { new IssueCreditNoteLineInput(line.Id, 1m) }), default);

        created.Should().NotBeNull();
        created!.Type.Should().Be(InvoiceType.CreditNote);
        var issuedEvent = created.DomainEvents.OfType<Domain.Events.InvoiceIssuedEvent>().Single();
        issuedEvent.Type.Should().Be(InvoiceType.CreditNote);
        issuedEvent.CustomerId.Should().Be(invoice.CustomerId);
    }

    private static Invoice BuildIssuedInvoice()
    {
        var invoice = new Invoice("INV-001", CustomerId, "Acme", "TRY")
        {
            Id = InvoiceId,
            TenantId = TenantId,
            Customer = new Customer("Acme") { Id = CustomerId, TenantId = TenantId },
        };
        var line1 = new InvoiceLine(Guid.NewGuid(), "SKU-1", "Widget", 5m, 10m) { Id = Guid.NewGuid(), TenantId = TenantId };
        var line2 = new InvoiceLine(Guid.NewGuid(), "SKU-2", "Gadget", 3m, 20m) { Id = Guid.NewGuid(), TenantId = TenantId };
        invoice.ReplaceLines(new[] { line1, line2 });
        invoice.Issue("INV-001");
        return invoice;
    }

    private static Invoice BuildDraftInvoice()
    {
        var invoice = new Invoice("INV-DRAFT", CustomerId, "Acme", "TRY")
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
            Customer = new Customer("Acme") { Id = CustomerId, TenantId = TenantId },
        };
        var line = new InvoiceLine(Guid.NewGuid(), "SKU-1", "Widget", 5m, 10m) { Id = Guid.NewGuid(), TenantId = TenantId };
        invoice.ReplaceLines(new[] { line });
        return invoice;
    }

    private static Invoice BuildCreditNote(Invoice source, InvoiceLine sourceLine, decimal quantity)
    {
        var cn = new Invoice("CN-PRIOR", source.CustomerId, source.CustomerNameSnapshot, source.Currency, InvoiceType.CreditNote)
        {
            Id = Guid.NewGuid(),
            TenantId = source.TenantId,
        };
        var creditLine = new InvoiceLine(sourceLine.ProductId ?? Guid.Empty, sourceLine.ProductSku, sourceLine.ProductName, quantity, sourceLine.UnitPrice)
        {
            Id = Guid.NewGuid(),
            TenantId = source.TenantId,
        };
        creditLine.ApplyPricing(
            quantity: quantity,
            unitPrice: sourceLine.UnitPrice,
            lineDiscountPercent: 0m,
            lineDiscountAmount: 0m,
            taxRatePercent: 0m,
            taxRateId: null,
            isTaxInclusive: false,
            withholdingRatePercent: 0m,
            uomId: null,
            uomCode: null,
            description: null,
            revenueAccountCode: null,
            costCenter: null,
            project: null,
            originOrderLineId: sourceLine.Id);
        cn.Lines.Add(creditLine);
        cn.Issue("CN-PRIOR");
        return cn;
    }
}
