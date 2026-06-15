using CoreAlign.Application.B2B;
using CoreAlign.Application.B2B.CustomerPortal;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.B2B;

public class CreateCustomerDirectOrderHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid OtherCustomerId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ProductId = Guid.NewGuid();

    private readonly IPortalScopeService _scope = Substitute.For<IPortalScopeService>();
    private readonly ICurrentUserAccessor _currentUser = Substitute.For<ICurrentUserAccessor>();
    private readonly IOrderRepository _orders = Substitute.For<IOrderRepository>();
    private readonly ICustomerRepository _customers = Substitute.For<ICustomerRepository>();
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly IPaymentTermRepository _paymentTerms = Substitute.For<IPaymentTermRepository>();
    private readonly IDocumentSequenceRepository _sequences = Substitute.For<IDocumentSequenceRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IPricingService _pricing = Substitute.For<IPricingService>();
    private readonly ICustomerAddressRepository _addresses = Substitute.For<ICustomerAddressRepository>();
    private readonly ICustomerLedgerRepository _ledger = Substitute.For<ICustomerLedgerRepository>();

    private readonly CreateCustomerDirectOrderHandler _sut;

    public CreateCustomerDirectOrderHandlerTests()
    {
        _currentUser.UserIdOrThrow().Returns(UserId);
        _scope.GetCurrentCustomerIdAsync(Arg.Any<CancellationToken>()).Returns(CustomerId);

        _sequences.ConsumeAsync(DocumentSequenceType.OrderNumber, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns("ORD-2026-9001");

        _pricing.ResolveAsync(Arg.Any<PriceResolutionRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var req = call.Arg<PriceResolutionRequest>();
                return new PriceResolutionResult(
                    UnitPrice: 50m,
                    Currency: req.RequestedCurrency ?? "TRY",
                    DiscountPercent: 0m,
                    Source: PriceSource.ProductListPrice,
                    SourceLabel: "list",
                    ReferenceListPrice: 50m,
                    TaxRatePercent: 0m,
                    IsTaxInclusive: false,
                    TaxRateId: null,
                    AppliedRecordId: null);
            });

        _sut = new CreateCustomerDirectOrderHandler(
            _scope, _currentUser, _orders, _customers, _products, _paymentTerms, _sequences, _uow, _pricing, _addresses, _ledger);
    }

    [Fact]
    public async Task HappyPath_creates_submitted_customer_origin_order()
    {
        SetupCustomerAndProduct();

        Order? captured = null;
        await _orders.AddAsync(Arg.Do<Order>(o => captured = o), Arg.Any<CancellationToken>());

        var orderId = await _sut.Handle(new CreateCustomerDirectOrderCommand(
            new[] { new CustomerDirectOrderLineInput(ProductId, 3m) }), default);

        captured.Should().NotBeNull();
        captured!.OriginPersona.Should().Be(OrderOriginPersona.Customer);
        captured.OriginCustomerUserId.Should().Be(UserId);
        captured.OriginDealerAccountId.Should().BeNull();
        captured.Status.Should().Be(OrderStatus.Submitted);
        captured.SubmittedAtUtc.Should().NotBeNull();
        captured.DealerApprovalStatus.Should().BeNull();
        captured.Lines.Should().HaveCount(1);
        captured.Channel.Should().Be("CustomerPortal");
        orderId.Should().Be(captured.Id);

        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Empty_lines_throws_and_does_not_persist()
    {
        var act = async () => await _sut.Handle(
            new CreateCustomerDirectOrderCommand(Array.Empty<CustomerDirectOrderLineInput>()), default);

        await act.Should().ThrowAsync<InvalidOrderLineException>();
        await _orders.DidNotReceive().AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Order_is_scoped_to_resolved_customer_only()
    {
        SetupCustomerAndProduct();

        Order? captured = null;
        await _orders.AddAsync(Arg.Do<Order>(o => captured = o), Arg.Any<CancellationToken>());

        await _sut.Handle(new CreateCustomerDirectOrderCommand(
            new[] { new CustomerDirectOrderLineInput(ProductId, 1m) }), default);

        captured.Should().NotBeNull();
        captured!.CustomerId.Should().Be(CustomerId);
        captured.CustomerId.Should().NotBe(OtherCustomerId);

        await _scope.Received(1).GetCurrentCustomerIdAsync(Arg.Any<CancellationToken>());
        await _customers.DidNotReceive().GetByIdAsync(OtherCustomerId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Currency_mismatch_throws_CurrencyMismatchException()
    {
        SetupCustomerAndProduct();

        _pricing.ResolveAsync(Arg.Any<PriceResolutionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PriceResolutionResult(
                UnitPrice: 50m,
                Currency: "USD",
                DiscountPercent: 0m,
                Source: PriceSource.ProductListPrice,
                SourceLabel: "list",
                ReferenceListPrice: 50m,
                TaxRatePercent: 0m,
                IsTaxInclusive: false,
                TaxRateId: null,
                AppliedRecordId: null));

        var act = async () => await _sut.Handle(new CreateCustomerDirectOrderCommand(
            new[] { new CustomerDirectOrderLineInput(ProductId, 1m) }), default);

        var ex = await act.Should().ThrowAsync<CurrencyMismatchException>();
        ex.Which.ProductId.Should().Be(ProductId);
        ex.Which.OrderCurrency.Should().Be("TRY");
        ex.Which.ResolvedCurrency.Should().Be("USD");

        await _orders.DidNotReceive().AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Unknown_product_throws()
    {
        var customer = new Customer("Acme", defaultCurrency: "TRY") { Id = CustomerId, TenantId = TenantId };
        _customers.GetByIdAsync(CustomerId, Arg.Any<CancellationToken>()).Returns(customer);
        _products.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, Product>());

        var act = async () => await _sut.Handle(new CreateCustomerDirectOrderCommand(
            new[] { new CustomerDirectOrderLineInput(ProductId, 1m) }), default);

        await act.Should().ThrowAsync<InvalidOrderLineException>();
        await _orders.DidNotReceive().AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
    }

    private void SetupCustomerAndProduct()
    {
        var customer = new Customer("Acme Holding", defaultCurrency: "TRY")
        {
            Id = CustomerId,
            TenantId = TenantId,
        };
        _customers.GetByIdAsync(CustomerId, Arg.Any<CancellationToken>()).Returns(customer);

        var product = new Product(
            sku: "SKU-CUST",
            name: "Direct widget",
            unit: "adet",
            price: 50m,
            currency: "TRY");
        typeof(Product).GetProperty(nameof(Product.Id))!.SetValue(product, ProductId);
        typeof(Product).GetProperty(nameof(Product.TenantId))!.SetValue(product, TenantId);
        _products.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, Product> { [ProductId] = product });
    }
}
