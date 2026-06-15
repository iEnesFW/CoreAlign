using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.Pricing;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Services;

namespace CoreAlign.Application.Tests.Pricing;

public class PricingServiceResolveTests
{
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly ICustomerRepository _customers = Substitute.For<ICustomerRepository>();
    private readonly IPriceListRepository _priceLists = Substitute.For<IPriceListRepository>();
    private readonly ICustomerProductPriceRepository _customerProductPrices = Substitute.For<ICustomerProductPriceRepository>();
    private readonly IPricingDiscountRuleRepository _discountRules = Substitute.For<IPricingDiscountRuleRepository>();
    private readonly ITaxRuleRepository _taxRules = Substitute.For<ITaxRuleRepository>();
    private readonly ITaxRateRepository _taxRates = Substitute.For<ITaxRateRepository>();

    private PricingService BuildSut() => new(
        _products, _customers, _priceLists, _customerProductPrices,
        _discountRules, _taxRules, _taxRates);

    [Fact]
    public async Task ResolveTax_uses_matching_tax_rule_over_flat_rate()
    {
        var productId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var ruleId = Guid.NewGuid();
        var taxRule = new TaxRule("GAP-AGRI", "GAP Agricultural", TaxRuleScope.RegionAndProductClass,
            ratePercent: 1m, regionCode: "GAP", productClass: "AGRICULTURAL");
        typeof(TaxRule).GetProperty(nameof(TaxRule.Id))!.SetValue(taxRule, ruleId);

        _taxRules.ListActiveAtAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new[] { taxRule });

        var sut = BuildSut();
        var result = await sut.ResolveTaxAsync(new TaxResolutionContext(
            productId,
            ProductCategoryId: null,
            ProductClass: "AGRICULTURAL",
            customerId,
            CustomerRegionCode: "GAP",
            AsOfUtc: DateTime.UtcNow), default);

        result.RatePercent.Should().Be(1m);
        result.TaxRuleId.Should().Be(ruleId);
        result.Source.Should().Be("TaxRule:GAP-AGRI");
    }

    [Fact]
    public async Task ResolveTax_uses_FallbackTaxRateId_when_matched_rule_rate_is_zero()
    {
        var productId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var fallbackRateId = Guid.NewGuid();
        var ruleId = Guid.NewGuid();
        var zeroRule = new TaxRule("EXPORT", "Exported Goods", TaxRuleScope.Region,
            ratePercent: 0m, regionCode: "EU", fallbackTaxRateId: fallbackRateId);
        typeof(TaxRule).GetProperty(nameof(TaxRule.Id))!.SetValue(zeroRule, ruleId);
        var fallback = new TaxRate("KDV20", "KDV 20", 20m);

        _taxRules.ListActiveAtAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new[] { zeroRule });
        _taxRates.GetByIdAsync(fallbackRateId, Arg.Any<CancellationToken>()).Returns(fallback);

        var sut = BuildSut();
        var result = await sut.ResolveTaxAsync(new TaxResolutionContext(
            productId,
            ProductCategoryId: null,
            ProductClass: null,
            customerId,
            CustomerRegionCode: "EU",
            AsOfUtc: DateTime.UtcNow), default);

        result.RatePercent.Should().Be(20m);
        result.TaxRuleId.Should().Be(ruleId);
        result.FallbackTaxRateId.Should().Be(fallbackRateId);
        result.Source.Should().Be("TaxRule:EXPORT");
    }

    [Fact]
    public async Task ResolveTax_falls_back_to_flat_tax_rate_when_no_rule_matches()
    {
        var productId = Guid.NewGuid();
        var taxRateId = Guid.NewGuid();
        var product = new Product("SKU-1", "Widget", "pcs", 100m, "TRY");
        typeof(Product).GetProperty(nameof(Product.Id))!.SetValue(product, productId);
        typeof(Product).GetProperty(nameof(Product.TaxRateId))!.SetValue(product, taxRateId);
        var flatRate = new TaxRate("KDV18", "KDV 18", 18m);

        _taxRules.ListActiveAtAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<TaxRule>());
        _products.GetByIdAsync(productId, Arg.Any<CancellationToken>()).Returns(product);
        _taxRates.GetByIdAsync(taxRateId, Arg.Any<CancellationToken>()).Returns(flatRate);

        var sut = BuildSut();
        var result = await sut.ResolveTaxAsync(new TaxResolutionContext(
            productId,
            ProductCategoryId: null,
            ProductClass: null,
            CustomerId: Guid.NewGuid(),
            CustomerRegionCode: null,
            AsOfUtc: DateTime.UtcNow), default);

        result.RatePercent.Should().Be(18m);
        result.TaxRuleId.Should().BeNull();
        result.Source.Should().Be("TaxRate:KDV18");
    }

    [Fact]
    public async Task ResolveDiscount_picks_highest_priority_matching_rule()
    {
        var productId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var customerGroupId = Guid.NewGuid();
        var lowPrio = new DiscountRule("LOW", "Low", DiscountRuleScope.ProductCategory, DiscountValueType.Percent, 5m,
            productCategoryId: categoryId, priority: 1);
        var highPrio = new DiscountRule("HIGH", "High", DiscountRuleScope.CustomerGroup, DiscountValueType.Percent, 20m,
            customerGroupId: customerGroupId, priority: 10);

        _discountRules.ListActiveAtAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new[] { lowPrio, highPrio });

        var sut = BuildSut();
        var result = await sut.ResolveDiscountAsync(new DiscountResolutionContext(
            productId,
            ProductCategoryId: categoryId,
            CustomerId: Guid.NewGuid(),
            CustomerGroupId: customerGroupId,
            Quantity: 1m,
            LineSubtotal: 100m,
            AsOfUtc: DateTime.UtcNow), default);

        result.AppliedDiscountRuleCode.Should().Be("HIGH");
        result.DiscountAmount.Should().Be(20m);
        result.DiscountPercent.Should().Be(20m);
    }

    [Fact]
    public async Task ResolveDiscount_returns_zero_when_no_rules_match()
    {
        _discountRules.ListActiveAtAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<DiscountRule>());

        var sut = BuildSut();
        var result = await sut.ResolveDiscountAsync(new DiscountResolutionContext(
            Guid.NewGuid(), null, Guid.NewGuid(), null, 1m, 100m, DateTime.UtcNow), default);

        result.DiscountAmount.Should().Be(0m);
        result.AppliedDiscountRuleId.Should().BeNull();
    }

    [Fact]
    public async Task ResolveMinQuantity_prefers_customer_override_over_product_default()
    {
        var productId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var product = new Product("SKU-1", "Widget", "pcs", 10m, "TRY");
        product.SetMinOrderQuantity(2m);
        var cpp = new CustomerProductPrice(customerId, productId, 8m);
        cpp.SetMinOrderQuantityOverride(5m);
        _customerProductPrices.GetForCustomerAndProductAsync(customerId, productId, Arg.Any<CancellationToken>())
            .Returns(new[] { cpp });
        _products.GetByIdAsync(productId, Arg.Any<CancellationToken>()).Returns(product);

        var result = await BuildSut().ResolveMinQuantityAsync(productId, customerId, default);

        result.Should().Be(5m);
    }

    [Fact]
    public async Task ResolveMinQuantity_falls_back_to_product_default_when_no_override()
    {
        var productId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var product = new Product("SKU-2", "Bolt", "pcs", 1m, "TRY");
        product.SetMinOrderQuantity(10m);
        _customerProductPrices.GetForCustomerAndProductAsync(customerId, productId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<CustomerProductPrice>());
        _products.GetByIdAsync(productId, Arg.Any<CancellationToken>()).Returns(product);

        var result = await BuildSut().ResolveMinQuantityAsync(productId, customerId, default);

        result.Should().Be(10m);
    }

    [Fact]
    public async Task ResolveMinQuantity_returns_null_when_neither_override_nor_product_specifies_one()
    {
        var productId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var product = new Product("SKU-3", "Tape", "pcs", 1m, "TRY");
        _customerProductPrices.GetForCustomerAndProductAsync(customerId, productId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<CustomerProductPrice>());
        _products.GetByIdAsync(productId, Arg.Any<CancellationToken>()).Returns(product);

        var result = await BuildSut().ResolveMinQuantityAsync(productId, customerId, default);

        result.Should().BeNull();
    }

    [Fact]
    public async Task ResolveMinQuantity_ignores_expired_override_and_falls_back_to_product()
    {
        var productId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var product = new Product("SKU-4", "Cable", "m", 5m, "TRY");
        product.SetMinOrderQuantity(3m);
        var expired = new CustomerProductPrice(
            customerId,
            productId,
            price: 4m,
            validFromUtc: DateTime.UtcNow.AddYears(-2),
            validUntilUtc: DateTime.UtcNow.AddDays(-1));
        expired.SetMinOrderQuantityOverride(50m);
        _customerProductPrices.GetForCustomerAndProductAsync(customerId, productId, Arg.Any<CancellationToken>())
            .Returns(new[] { expired });
        _products.GetByIdAsync(productId, Arg.Any<CancellationToken>()).Returns(product);

        var result = await BuildSut().ResolveMinQuantityAsync(productId, customerId, default);

        result.Should().Be(3m);
    }

    [Fact]
    public async Task ResolveMinQuantity_ignores_future_dated_override_and_falls_back_to_product()
    {
        var productId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var product = new Product("SKU-5", "Pipe", "m", 2m, "TRY");
        product.SetMinOrderQuantity(7m);
        var notYet = new CustomerProductPrice(
            customerId,
            productId,
            price: 1.5m,
            validFromUtc: DateTime.UtcNow.AddDays(5));
        notYet.SetMinOrderQuantityOverride(99m);
        _customerProductPrices.GetForCustomerAndProductAsync(customerId, productId, Arg.Any<CancellationToken>())
            .Returns(new[] { notYet });
        _products.GetByIdAsync(productId, Arg.Any<CancellationToken>()).Returns(product);

        var result = await BuildSut().ResolveMinQuantityAsync(productId, customerId, default);

        result.Should().Be(7m);
    }
}
