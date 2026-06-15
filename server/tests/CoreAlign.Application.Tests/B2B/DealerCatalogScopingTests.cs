using CoreAlign.Application.B2B;
using CoreAlign.Application.B2B.DealerPortal;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.B2B;

public class DealerCatalogScopingTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid DealerId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();

    private readonly IPortalScopeService _scope = Substitute.For<IPortalScopeService>();
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly IPricingService _pricing = Substitute.For<IPricingService>();
    private readonly IDealerCustomerLinkRepository _links = Substitute.For<IDealerCustomerLinkRepository>();
    private readonly ICustomerDealerProductVisibilityRepository _visibility = Substitute.For<ICustomerDealerProductVisibilityRepository>();

    public DealerCatalogScopingTests()
    {
        _scope.GetCurrentDealerAccountIdAsync(Arg.Any<CancellationToken>()).Returns(DealerId);
        _pricing.ResolveBatchAsync(Arg.Any<IReadOnlyList<PriceResolutionRequest>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var requests = call.Arg<IReadOnlyList<PriceResolutionRequest>>();
                return requests
                    .Select(r => new PriceResolutionResult(
                        UnitPrice: 100m,
                        Currency: "TRY",
                        DiscountPercent: 0m,
                        Source: PriceSource.ProductListPrice,
                        SourceLabel: "list",
                        ReferenceListPrice: 100m,
                        TaxRatePercent: 0m,
                        IsTaxInclusive: false,
                        TaxRateId: null,
                        AppliedRecordId: null))
                    .ToList();
            });
    }

    [Fact]
    public async Task When_customer_has_no_whitelist_dealer_sees_full_catalog()
    {
        _scope.GetDealerAllowedCustomerIdsAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { CustomerId });

        var link = BuildLink();
        _links.GetByDealerAndCustomerAsync(DealerId, CustomerId, Arg.Any<CancellationToken>()).Returns(link);
        _visibility.HasAnyForLinkAsync(link.Id, Arg.Any<CancellationToken>()).Returns(false);

        var products = new[] { BuildProduct(Guid.NewGuid(), "SKU-A"), BuildProduct(Guid.NewGuid(), "SKU-B") };
        _products.SearchAsync(
                Arg.Any<string?>(),
                Arg.Any<bool?>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<IReadOnlyCollection<Guid>?>(),
                Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<Product>)products, 2));

        var handler = new ListDealerCatalogProductsHandler(_scope, _products, _pricing, _links, _visibility);

        var result = await handler.Handle(
            new ListDealerCatalogProductsQuery(Search: null, CustomerId: CustomerId, Page: 1, PageSize: 20),
            default);

        result.Total.Should().Be(2);
        await _products.Received(1).SearchAsync(
            Arg.Any<string?>(),
            Arg.Any<bool?>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Is<IReadOnlyCollection<Guid>?>(ids => ids == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task When_customer_has_whitelist_dealer_sees_only_those_products()
    {
        _scope.GetDealerAllowedCustomerIdsAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { CustomerId });

        var link = BuildLink();
        var visibleIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        _links.GetByDealerAndCustomerAsync(DealerId, CustomerId, Arg.Any<CancellationToken>()).Returns(link);
        _visibility.HasAnyForLinkAsync(link.Id, Arg.Any<CancellationToken>()).Returns(true);
        _visibility.ListVisibleProductIdsAsync(link.Id, Arg.Any<CancellationToken>()).Returns(visibleIds);

        var products = visibleIds.Select(id => BuildProduct(id, $"SKU-{id:N}".Substring(0, 12))).ToArray();
        _products.SearchAsync(
                Arg.Any<string?>(),
                Arg.Any<bool?>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<IReadOnlyCollection<Guid>?>(),
                Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<Product>)products, 2));

        var handler = new ListDealerCatalogProductsHandler(_scope, _products, _pricing, _links, _visibility);

        var result = await handler.Handle(
            new ListDealerCatalogProductsQuery(Search: null, CustomerId: CustomerId, Page: 1, PageSize: 20),
            default);

        result.Items.Should().HaveCount(2);
        await _products.Received(1).SearchAsync(
            Arg.Any<string?>(),
            Arg.Any<bool?>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Is<IReadOnlyCollection<Guid>?>(ids => ids != null && ids.SequenceEqual(visibleIds)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task When_customerId_is_not_provided_whitelist_is_ignored()
    {
        var products = new[] { BuildProduct(Guid.NewGuid(), "SKU-X") };
        _products.SearchAsync(
                Arg.Any<string?>(),
                Arg.Any<bool?>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<IReadOnlyCollection<Guid>?>(),
                Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<Product>)products, 1));

        var handler = new ListDealerCatalogProductsHandler(_scope, _products, _pricing, _links, _visibility);

        var result = await handler.Handle(
            new ListDealerCatalogProductsQuery(Search: null, CustomerId: null, Page: 1, PageSize: 20),
            default);

        result.Items.Should().HaveCount(1);
        await _visibility.DidNotReceive().HasAnyForLinkAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _visibility.DidNotReceive().ListVisibleProductIdsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _products.Received(1).SearchAsync(
            Arg.Any<string?>(),
            Arg.Any<bool?>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Is<IReadOnlyCollection<Guid>?>(ids => ids == null),
            Arg.Any<CancellationToken>());
    }

    private static DealerCustomerLink BuildLink()
    {
        var link = new DealerCustomerLink(DealerId, CustomerId, assignedByUserId: null);
        typeof(DealerCustomerLink).GetProperty(nameof(DealerCustomerLink.Id))!.SetValue(link, Guid.NewGuid());
        typeof(DealerCustomerLink).GetProperty(nameof(DealerCustomerLink.TenantId))!.SetValue(link, TenantId);
        return link;
    }

    private static Product BuildProduct(Guid id, string sku)
    {
        var product = new Product(sku: sku, name: "Test", unit: "adet", price: 1m, currency: "TRY");
        typeof(Product).GetProperty(nameof(Product.Id))!.SetValue(product, id);
        typeof(Product).GetProperty(nameof(Product.TenantId))!.SetValue(product, TenantId);
        return product;
    }
}
