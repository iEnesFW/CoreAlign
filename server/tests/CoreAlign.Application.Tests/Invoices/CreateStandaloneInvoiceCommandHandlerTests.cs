using CoreAlign.Application.EInvoice;
using CoreAlign.Application.Invoices.Commands;
using CoreAlign.Application.Invoices.Handlers;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Invoices;

public class CreateStandaloneInvoiceCommandHandlerTests
{
    private readonly ICustomerRepository _customers = Substitute.For<ICustomerRepository>();
    private readonly ICustomerAddressRepository _addresses = Substitute.For<ICustomerAddressRepository>();
    private readonly IPaymentTermRepository _paymentTerms = Substitute.For<IPaymentTermRepository>();
    private readonly IInvoiceRepository _invoices = Substitute.For<IInvoiceRepository>();
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly IDocumentSequenceRepository _sequences = Substitute.For<IDocumentSequenceRepository>();
    private readonly IAccountingPeriodRepository _periods = Substitute.For<IAccountingPeriodRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IEmailService _email = Substitute.For<IEmailService>();
    private readonly IEInvoiceSubmissionOutbox _einvoice = Substitute.For<IEInvoiceSubmissionOutbox>();
    private readonly ICustomerLedgerRepository _ledger = Substitute.For<ICustomerLedgerRepository>();
    private readonly CreateStandaloneInvoiceCommandHandler _sut;

    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();

    public CreateStandaloneInvoiceCommandHandlerTests()
    {
        _sequences
            .ConsumeAsync(Arg.Any<DocumentSequenceType>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns("INV-STD-0001");
        _products
            .GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, Product>());

        _sut = new CreateStandaloneInvoiceCommandHandler(
            _customers, _addresses, _paymentTerms, _invoices, _products, _sequences,
            _periods, _uow, _email, _einvoice,
            new CoreAlign.Application.CustomerPortal.Credit.CreditLimitGuard(_ledger));
    }

    [Fact]
    public async Task Creates_invoice_without_order_with_lines()
    {
        var customer = BuildCustomer();
        _customers.GetByIdAsync(CustomerId, Arg.Any<CancellationToken>()).Returns(customer);

        Invoice? captured = null;
        await _invoices.AddAsync(Arg.Do<Invoice>(i => captured = i), Arg.Any<CancellationToken>());

        var cmd = new CreateStandaloneInvoiceCommand(
            CustomerId: CustomerId,
            IssueDate: DateTime.UtcNow,
            DueDays: 14,
            Currency: "TRY",
            Lines: new List<StandaloneInvoiceLineInput>
            {
                new(null, "SVC-CONS", "Consulting hours", "August retainer", 10m, 250m, TaxRatePercent: 20m),
                new(null, "SVC-SUP", "Support hours", null, 2m, 100m, TaxRatePercent: 20m),
            });

        var dto = await _sut.Handle(cmd, default);

        dto.Should().NotBeNull();
        dto.OrderId.Should().BeNull();
        dto.Status.Should().Be(InvoiceStatus.Issued);
        dto.Lines.Should().HaveCount(2);
        dto.Total.Should().Be(3240m);
        captured.Should().NotBeNull();
        captured!.OrderId.Should().BeNull();
        await _invoices.Received(1).AddAsync(Arg.Any<Invoice>(), Arg.Any<CancellationToken>());
        await _einvoice.Received(1).EnqueueSubmissionAsync(Arg.Any<EInvoiceSubmissionRequestedPayload>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_when_customer_missing()
    {
        _customers.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Customer?)null);
        var cmd = new CreateStandaloneInvoiceCommand(
            CustomerId: Guid.NewGuid(),
            IssueDate: DateTime.UtcNow,
            DueDays: 30,
            Currency: "TRY",
            Lines: new List<StandaloneInvoiceLineInput>
            {
                new(null, "X", "X", null, 1m, 1m),
            });

        Func<Task> act = () => _sut.Handle(cmd, default);

        await act.Should().ThrowAsync<CustomerNotFoundException>();
        await _invoices.DidNotReceive().AddAsync(Arg.Any<Invoice>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Sends_email_when_customer_has_email_address()
    {
        var customer = BuildCustomer();
        _customers.GetByIdAsync(CustomerId, Arg.Any<CancellationToken>()).Returns(customer);

        var cmd = new CreateStandaloneInvoiceCommand(
            CustomerId: CustomerId,
            IssueDate: DateTime.UtcNow,
            DueDays: 0,
            Currency: "TRY",
            Lines: new List<StandaloneInvoiceLineInput>
            {
                new(null, "S", "Service", null, 1m, 50m),
            });

        await _sut.Handle(cmd, default);

        await _email.Received(1).SendInvoiceIssuedAsync(
            "test@customer.example",
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<decimal>(),
            "TRY",
            Arg.Any<CancellationToken>());
    }

    private static Customer BuildCustomer()
    {
        return new Customer(
            name: "Standalone Customer",
            type: CustomerType.Business,
            code: "STD-1",
            legalName: "Standalone Customer A.Ş.",
            email: "test@customer.example")
        {
            Id = CustomerId,
            TenantId = TenantId,
        };
    }
}
