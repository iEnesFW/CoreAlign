using CoreAlign.Application.Common.Caching;
using CoreAlign.Application.Invoices.Commands;
using CoreAlign.Application.Invoices.Handlers;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Caching;
using CoreAlign.Infrastructure.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace CoreAlign.Application.Tests.Idempotency;

[Collection(IdempotencyTestCollection.Name)]
public class IssueCreditNoteIdempotencyTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid InvoiceId = Guid.NewGuid();

    private readonly IInvoiceRepository _invoiceRepository = Substitute.For<IInvoiceRepository>();
    private readonly IDocumentSequenceRepository _sequenceRepository = Substitute.For<IDocumentSequenceRepository>();
    private readonly IAccountingPeriodRepository _periodRepository = Substitute.For<IAccountingPeriodRepository>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IDistributedCacheService _cache;
    private readonly IssueCreditNoteCommandHandler _sut;

    private int _sequenceCounter;

    public IssueCreditNoteIdempotencyTests()
    {
        _sequenceRepository
            .ConsumeAsync(DocumentSequenceType.CreditNoteNumber, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(_ => $"CN-TEST-{Interlocked.Increment(ref _sequenceCounter):D4}");
        _cache = new InMemoryDistributedCacheService(
            new MemoryCache(new MemoryCacheOptions { SizeLimit = 1024 }),
            Options.Create(new CacheRegionOptions()));
        _invoiceRepository.GetCreditNotesForInvoiceAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Invoice>());
        _sut = new IssueCreditNoteCommandHandler(
            _invoiceRepository, _sequenceRepository, _periodRepository, _tenantContext, _cache);
    }

    [Fact]
    public async Task FirstCreditNote_ConsumesSequenceAndAddsInvoiceOnce()
    {
        var invoice = BuildIssuedInvoice();
        var line = invoice.Lines.First();
        _invoiceRepository.GetWithLinesAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);

        var result = await _sut.Handle(
            new IssueCreditNoteCommand(invoice.Id, new[] { new IssueCreditNoteLineInput(line.Id, 1m) }),
            default);

        result.Type.Should().Be(InvoiceType.CreditNote);
        await _invoiceRepository.Received(1).AddAsync(Arg.Any<Invoice>(), Arg.Any<CancellationToken>());
        _sequenceCounter.Should().Be(1);
    }

    [Fact]
    public async Task RetryWithSameInvoiceAndLines_IsIdempotent_SuppressesDuplicateCreditNote()
    {
        var invoice = BuildIssuedInvoice();
        var line = invoice.Lines.First();
        _invoiceRepository.GetWithLinesAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);

        Invoice? firstCreated = null;
        Invoice? secondCreated = null;
        var calls = 0;
        await _invoiceRepository.AddAsync(Arg.Do<Invoice>(i =>
        {
            if (calls++ == 0) firstCreated = i;
            else secondCreated = i;
        }), Arg.Any<CancellationToken>());

        var command = new IssueCreditNoteCommand(invoice.Id, new[] { new IssueCreditNoteLineInput(line.Id, 1m) });

        await _sut.Handle(command, default);
        await _sut.Handle(command, default);

        firstCreated.Should().NotBeNull();
        secondCreated.Should().BeNull("idempotency suppresses the duplicate AddAsync");
        _sequenceCounter.Should().Be(1, "duplicate command MUST NOT burn a new sequence number");
        await _invoiceRepository.Received(1).AddAsync(Arg.Any<Invoice>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RetryWithSameOperationId_ReturnsSameCreditNote()
    {
        var invoice = BuildIssuedInvoice();
        var line = invoice.Lines.First();
        _invoiceRepository.GetWithLinesAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);
        var operationId = Guid.NewGuid();
        var command = new IssueCreditNoteCommand(
            invoice.Id, new[] { new IssueCreditNoteLineInput(line.Id, 1m) }, OperationId: operationId);

        var first = await _sut.Handle(command, default);
        var second = await _sut.Handle(command, default);

        second.Id.Should().Be(first.Id);
        _sequenceCounter.Should().Be(1);
        await _invoiceRepository.Received(1).AddAsync(Arg.Any<Invoice>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RetryFromSameReturnRequest_ReplaysExistingCreditNoteDurably()
    {
        var invoice = BuildIssuedInvoice();
        var line = invoice.Lines.First();
        _invoiceRepository.GetWithLinesAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);
        var addedSoFar = new List<Invoice>();
        await _invoiceRepository.AddAsync(Arg.Do<Invoice>(addedSoFar.Add), Arg.Any<CancellationToken>());
        _invoiceRepository.GetCreditNotesForInvoiceAsync(invoice.Id, Arg.Any<CancellationToken>())
            .Returns(_ => addedSoFar.ToArray());

        var returnRequestId = Guid.NewGuid();
        var command = new IssueCreditNoteCommand(
            invoice.Id,
            new[] { new IssueCreditNoteLineInput(line.Id, 1m) },
            Reason: "RMA",
            ReturnRequestId: returnRequestId);

        var first = await _sut.Handle(command, default);
        var second = await _sut.Handle(command, default);

        addedSoFar.Should().HaveCount(1, "the return-request natural key replays the existing credit note");
        second.Id.Should().Be(first.Id);
        _sequenceCounter.Should().Be(1);
    }

    private static Invoice BuildIssuedInvoice()
    {
        var invoice = new Invoice("INV-001", CustomerId, "Acme", "TRY")
        {
            Id = InvoiceId,
            TenantId = TenantId,
            Customer = new Customer("Acme") { Id = CustomerId, TenantId = TenantId },
        };
        var line = new InvoiceLine(Guid.NewGuid(), "SKU-1", "Widget", 5m, 10m) { Id = Guid.NewGuid(), TenantId = TenantId };
        invoice.ReplaceLines(new[] { line });
        invoice.Issue("INV-001");
        return invoice;
    }
}
