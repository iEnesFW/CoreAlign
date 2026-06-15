using CoreAlign.Application.Quotes.Commands;
using CoreAlign.Application.Quotes.Handlers;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Quotes;

public class CreateQuoteCommandHandlerTests
{
    private readonly IQuoteRepository _quotes = Substitute.For<IQuoteRepository>();
    private readonly ICustomerRepository _customers = Substitute.For<ICustomerRepository>();
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly ICustomerAddressRepository _addresses = Substitute.For<ICustomerAddressRepository>();
    private readonly IPaymentTermRepository _paymentTerms = Substitute.For<IPaymentTermRepository>();
    private readonly IDocumentSequenceRepository _sequences = Substitute.For<IDocumentSequenceRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly CreateQuoteCommandHandler _sut;

    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid ProductId = Guid.NewGuid();

    public CreateQuoteCommandHandlerTests()
    {
        _sequences
            .ConsumeAsync(Arg.Any<DocumentSequenceType>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns("QUO-AUTO-0001");
        _sut = new CreateQuoteCommandHandler(_quotes, _customers, _products, _addresses, _paymentTerms, _sequences, _uow);
    }

    [Fact]
    public async Task Creates_quote_with_lines_and_auto_assigned_number_when_blank()
    {
        var customer = new Customer("Acme") { Id = CustomerId, TenantId = TenantId };
        var product = new Product("SKU-1", "Widget", "EA", 100m) { Id = ProductId, TenantId = TenantId };
        _customers.GetByIdAsync(CustomerId, Arg.Any<CancellationToken>()).Returns(customer);
        _products.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, Product> { [ProductId] = product });

        Quote? captured = null;
        await _quotes.AddAsync(Arg.Do<Quote>(q => captured = q), Arg.Any<CancellationToken>());

        var cmd = new CreateQuoteCommand(
            QuoteNumber: null,
            CustomerId: CustomerId,
            QuoteDate: DateTime.UtcNow,
            ValidUntilUtc: DateTime.UtcNow.AddDays(15),
            Currency: "TRY",
            Notes: null,
            Lines: new List<QuoteLineInput>
            {
                new(ProductId, 3m, 100m, TaxRatePercent: 20m),
            });

        var dto = await _sut.Handle(cmd, default);

        dto.Should().NotBeNull();
        dto.QuoteNumber.Should().Be("QUO-AUTO-0001");
        dto.Status.Should().Be(QuoteStatus.Draft);
        dto.Lines.Should().HaveCount(1);
        dto.Total.Should().Be(360m);
        captured.Should().NotBeNull();
        captured!.CustomerSnapshot.Should().NotBeNull();
        captured.CustomerSnapshot!.LegalName.Should().Be("Acme");
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_when_explicit_number_collides()
    {
        var customer = new Customer("Acme") { Id = CustomerId, TenantId = TenantId };
        var product = new Product("SKU-1", "Widget", "EA", 100m) { Id = ProductId, TenantId = TenantId };
        _customers.GetByIdAsync(CustomerId, Arg.Any<CancellationToken>()).Returns(customer);
        _products.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, Product> { [ProductId] = product });
        _quotes.QuoteNumberExistsAsync("QUO-DUP", null, Arg.Any<CancellationToken>()).Returns(true);

        var cmd = new CreateQuoteCommand(
            QuoteNumber: "QUO-DUP",
            CustomerId: CustomerId,
            QuoteDate: DateTime.UtcNow,
            ValidUntilUtc: DateTime.UtcNow.AddDays(15),
            Currency: "TRY",
            Notes: null,
            Lines: new List<QuoteLineInput> { new(ProductId, 1m, 50m) });

        Func<Task> act = () => _sut.Handle(cmd, default);

        await act.Should().ThrowAsync<DuplicateQuoteNumberException>();
    }

    [Fact]
    public async Task Throws_when_product_not_found()
    {
        var customer = new Customer("Acme") { Id = CustomerId, TenantId = TenantId };
        _customers.GetByIdAsync(CustomerId, Arg.Any<CancellationToken>()).Returns(customer);
        _products.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, Product>());

        var cmd = new CreateQuoteCommand(
            QuoteNumber: null,
            CustomerId: CustomerId,
            QuoteDate: DateTime.UtcNow,
            ValidUntilUtc: DateTime.UtcNow.AddDays(15),
            Currency: "TRY",
            Notes: null,
            Lines: new List<QuoteLineInput> { new(ProductId, 1m, 50m) });

        Func<Task> act = () => _sut.Handle(cmd, default);

        await act.Should().ThrowAsync<InvalidQuoteLineException>();
    }
}
