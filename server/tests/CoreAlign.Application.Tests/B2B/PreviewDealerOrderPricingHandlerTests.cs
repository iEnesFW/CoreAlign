using CoreAlign.Application.B2B;
using CoreAlign.Application.B2B.DealerPortal;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.B2B;

public class PreviewDealerOrderPricingHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid ProductId = Guid.NewGuid();

    private readonly IPortalScopeService _scope = Substitute.For<IPortalScopeService>();
    private readonly ICustomerRepository _customers = Substitute.For<ICustomerRepository>();
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly IPricingService _pricing = Substitute.For<IPricingService>();

    private readonly PreviewDealerOrderPricingHandler _sut;

    public PreviewDealerOrderPricingHandlerTests()
    {
        _sut = new PreviewDealerOrderPricingHandler(_scope, _customers, _products, _pricing);
    }

    [Fact]
    public async Task Prices_the_basket_at_the_ordered_quantity_not_at_one()
    {
        SetupAuthorizedCustomerAndProduct();
        SetupTieredPricing();

        var result = await _sut.Handle(
            new PreviewDealerOrderPricingQuery(CustomerId, new[] { new DealerOrderLineInput(ProductId, 100m) }),
            default);

        result.Lines.Should().ContainSingle();
        result.Lines[0].UnitPrice.Should().Be(80m);
        result.Lines[0].Quantity.Should().Be(100m);
        result.Subtotal.Should().Be(8000m);
    }

    [Fact]
    public async Task Reports_the_same_unit_price_the_create_handler_would_book()
    {
        SetupAuthorizedCustomerAndProduct();
        SetupTieredPricing();

        var lines = new[] { new DealerOrderLineInput(ProductId, 100m) };
        var products = await _products.GetByIdsAsync(new[] { ProductId }, default);

        var preview = await _sut.Handle(new PreviewDealerOrderPricingQuery(CustomerId, lines), default);
        var booked = await DealerOrderPricingResolver.ResolveAsync(
            _pricing, products, CustomerId, "TRY", lines, DateTime.UtcNow, default);

        preview.Lines[0].UnitPrice.Should().Be(booked[0].UnitPrice);
    }

    [Fact]
    public async Task Carries_tax_into_the_total_so_the_dealer_sees_what_is_billed()
    {
        SetupAuthorizedCustomerAndProduct();
        SetupPricing(unitPrice: 100m, taxRatePercent: 20m);

        var result = await _sut.Handle(
            new PreviewDealerOrderPricingQuery(CustomerId, new[] { new DealerOrderLineInput(ProductId, 3m) }),
            default);

        result.Subtotal.Should().Be(300m);
        result.TaxTotal.Should().Be(60m);
        result.Total.Should().Be(360m);
    }

    [Fact]
    public async Task Refuses_a_customer_the_dealer_is_not_linked_to()
    {
        _scope.GetDealerAllowedCustomerIdsAsync(Arg.Any<CancellationToken>()).Returns(new[] { CustomerId });

        var act = async () => await _sut.Handle(
            new PreviewDealerOrderPricingQuery(Guid.NewGuid(), new[] { new DealerOrderLineInput(ProductId, 1m) }),
            default);

        await act.Should().ThrowAsync<DealerCustomerNotAuthorizedException>();
    }

    [Fact]
    public async Task Refuses_a_quantity_below_the_minimum_order_quantity()
    {
        SetupAuthorizedCustomerAndProduct();
        SetupPricing(unitPrice: 100m, taxRatePercent: 0m);
        _pricing.ResolveMinQuantityAsync(ProductId, CustomerId, Arg.Any<CancellationToken>()).Returns(10m);

        var act = async () => await _sut.Handle(
            new PreviewDealerOrderPricingQuery(CustomerId, new[] { new DealerOrderLineInput(ProductId, 4m) }),
            default);

        await act.Should().ThrowAsync<MinOrderQuantityNotMetException>();
    }

    [Fact]
    public async Task Refuses_an_empty_basket()
    {
        var act = async () => await _sut.Handle(
            new PreviewDealerOrderPricingQuery(CustomerId, Array.Empty<DealerOrderLineInput>()),
            default);

        await act.Should().ThrowAsync<InvalidOrderLineException>();
    }

    private void SetupAuthorizedCustomerAndProduct()
    {
        _scope.GetDealerAllowedCustomerIdsAsync(Arg.Any<CancellationToken>()).Returns(new[] { CustomerId });

        var customer = new Customer("Acme Holding") { Id = CustomerId, TenantId = TenantId };
        _customers.GetByIdAsync(CustomerId, Arg.Any<CancellationToken>()).Returns(customer);

        var product = new Product(sku: "SKU-A", name: "Widget", unit: "ADET", price: 100m, currency: "TRY");
        typeof(Product).GetProperty(nameof(Product.Id))!.SetValue(product, ProductId);
        typeof(Product).GetProperty(nameof(Product.TenantId))!.SetValue(product, TenantId);
        _products.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, Product> { [ProductId] = product });
    }

    private void SetupPricing(decimal unitPrice, decimal taxRatePercent) =>
        _pricing.ResolveAsync(Arg.Any<PriceResolutionRequest>(), Arg.Any<CancellationToken>())
            .Returns(call => Resolution(call.Arg<PriceResolutionRequest>(), unitPrice, taxRatePercent));

    // WHY a tier: the catalogue asks for quantity 1 and the order asks for the real quantity, so a
    // price that changes with quantity is the only shape that can expose them diverging.
    private void SetupTieredPricing() =>
        _pricing.ResolveAsync(Arg.Any<PriceResolutionRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var req = call.Arg<PriceResolutionRequest>();
                return Resolution(req, req.Quantity >= 50m ? 80m : 100m, 0m);
            });

    private static PriceResolutionResult Resolution(
        PriceResolutionRequest request,
        decimal unitPrice,
        decimal taxRatePercent) =>
        new(
            UnitPrice: unitPrice,
            Currency: request.RequestedCurrency ?? "TRY",
            DiscountPercent: 0m,
            Source: PriceSource.ProductListPrice,
            SourceLabel: "list",
            ReferenceListPrice: 100m,
            TaxRatePercent: taxRatePercent,
            IsTaxInclusive: false,
            TaxRateId: null,
            AppliedRecordId: null);
}
