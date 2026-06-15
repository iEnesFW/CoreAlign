using CoreAlign.Application.Stock.Substitute;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;
using NSubSubstitute = NSubstitute.Substitute;

namespace CoreAlign.Application.Tests.Stock.SubstituteResolver;

public class ProductSubstituteResolverTests
{
    private readonly IProductSubstituteRepository _substitutes = NSubSubstitute.For<IProductSubstituteRepository>();
    private readonly IProductRepository _products = NSubSubstitute.For<IProductRepository>();

    private ProductSubstituteResolver BuildSut() => new(_substitutes, _products);

    private void StubFrontier(params (Guid Source, ProductSubstitute[] Edges)[] levels)
    {
        foreach (var (source, edges) in levels)
        {
            _substitutes.ListByProductAsync(source, Arg.Any<CancellationToken>()).Returns(edges);
        }
        _substitutes.ListByProductsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var ids = ((IEnumerable<Guid>)call[0]).ToHashSet();
                var matched = levels
                    .Where(l => ids.Contains(l.Source))
                    .SelectMany(l => l.Edges)
                    .Distinct()
                    .ToList();
                return (IReadOnlyList<ProductSubstitute>)matched;
            });
    }

    [Fact]
    public async Task ResolveAsync_returns_empty_when_no_substitute_rules_exist()
    {
        var productId = Guid.NewGuid();
        _substitutes.ListByProductAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ProductSubstitute>());
        _substitutes.ListByProductsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ProductSubstitute>());

        var result = await BuildSut().ResolveAsync(productId, requiredQuantity: 10m);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ResolveAsync_returns_single_suggestion_for_direct_substitute_at_depth_one()
    {
        var productA = Guid.NewGuid();
        var productB = Guid.NewGuid();
        var rule = new ProductSubstitute(productA, productB, conversionRate: 1m, priority: 0);

        StubFrontier(
            (productA, new[] { rule }),
            (productB, Array.Empty<ProductSubstitute>()));

        _products.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, Product>
            {
                [productB] = BuildProduct(productB, "SKU-B", "Product B"),
            });

        var result = await BuildSut().ResolveAsync(productA, requiredQuantity: 5m);

        result.Should().HaveCount(1);
        result[0].ProductId.Should().Be(productB);
        result[0].Depth.Should().Be(1);
        result[0].ConversionRate.Should().Be(1m);
    }

    [Fact]
    public async Task ResolveAsync_traverses_chain_up_to_max_depth_returning_two_suggestions()
    {
        var productA = Guid.NewGuid();
        var productB = Guid.NewGuid();
        var productC = Guid.NewGuid();

        StubFrontier(
            (productA, new[] { new ProductSubstitute(productA, productB, 1m) }),
            (productB, new[] { new ProductSubstitute(productB, productC, 1m) }),
            (productC, Array.Empty<ProductSubstitute>()));

        _products.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, Product>
            {
                [productB] = BuildProduct(productB, "SKU-B", "Product B"),
                [productC] = BuildProduct(productC, "SKU-C", "Product C"),
            });

        var result = await BuildSut().ResolveAsync(productA, requiredQuantity: 1m, maxDepth: 3);

        result.Should().HaveCount(2);
        result.Single(s => s.ProductId == productB).Depth.Should().Be(1);
        result.Single(s => s.ProductId == productC).Depth.Should().Be(2);
    }

    [Fact]
    public async Task ResolveAsync_prevents_cycle_when_chain_loops_back_to_origin()
    {
        var productA = Guid.NewGuid();
        var productB = Guid.NewGuid();

        StubFrontier(
            (productA, new[] { new ProductSubstitute(productA, productB, 1m) }),
            (productB, new[] { new ProductSubstitute(productB, productA, 1m) }));

        _products.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, Product>
            {
                [productB] = BuildProduct(productB, "SKU-B", "Product B"),
            });

        var result = await BuildSut().ResolveAsync(productA, requiredQuantity: 1m, maxDepth: 3);

        result.Should().HaveCount(1);
        result[0].ProductId.Should().Be(productB);
        result.Should().NotContain(s => s.ProductId == productA);
    }

    [Fact]
    public async Task ResolveAsync_uses_bidirectional_rule_in_reverse_when_starting_from_substitute_side()
    {
        var productA = Guid.NewGuid();
        var productB = Guid.NewGuid();
        var rule = new ProductSubstitute(productA, productB, conversionRate: 1m, isBidirectional: true);

        StubFrontier(
            (productB, new[] { rule }),
            (productA, Array.Empty<ProductSubstitute>()));

        _products.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, Product>
            {
                [productA] = BuildProduct(productA, "SKU-A", "Product A"),
            });

        var result = await BuildSut().ResolveAsync(productB, requiredQuantity: 1m);

        result.Should().HaveCount(1);
        result[0].ProductId.Should().Be(productA);
    }

    [Fact]
    public async Task ResolveAsync_orders_same_depth_suggestions_by_priority_ascending()
    {
        var productA = Guid.NewGuid();
        var productHigh = Guid.NewGuid();
        var productLow = Guid.NewGuid();

        StubFrontier(
            (productA, new[]
            {
                new ProductSubstitute(productA, productLow, 1m, priority: 5),
                new ProductSubstitute(productA, productHigh, 1m, priority: 1),
            }),
            (productHigh, Array.Empty<ProductSubstitute>()),
            (productLow, Array.Empty<ProductSubstitute>()));

        _products.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, Product>
            {
                [productHigh] = BuildProduct(productHigh, "SKU-HI", "High"),
                [productLow] = BuildProduct(productLow, "SKU-LO", "Low"),
            });

        var result = await BuildSut().ResolveAsync(productA, requiredQuantity: 1m);

        result.Should().HaveCount(2);
        result[0].ProductId.Should().Be(productHigh);
        result[0].Priority.Should().Be(1);
        result[1].ProductId.Should().Be(productLow);
        result[1].Priority.Should().Be(5);
    }

    [Fact]
    public async Task ResolveAsync_multiplies_conversion_rates_along_chain_cumulatively()
    {
        var productA = Guid.NewGuid();
        var productB = Guid.NewGuid();
        var productC = Guid.NewGuid();

        StubFrontier(
            (productA, new[] { new ProductSubstitute(productA, productB, conversionRate: 0.8m) }),
            (productB, new[] { new ProductSubstitute(productB, productC, conversionRate: 0.9m) }),
            (productC, Array.Empty<ProductSubstitute>()));

        _products.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, Product>
            {
                [productB] = BuildProduct(productB, "SKU-B", "Product B"),
                [productC] = BuildProduct(productC, "SKU-C", "Product C"),
            });

        var result = await BuildSut().ResolveAsync(productA, requiredQuantity: 1m, maxDepth: 3);

        var suggestionB = result.Single(s => s.ProductId == productB);
        var suggestionC = result.Single(s => s.ProductId == productC);
        suggestionB.ConversionRate.Should().Be(0.8m);
        suggestionC.ConversionRate.Should().Be(0.72m);
    }

    private static Product BuildProduct(Guid id, string sku, string name)
    {
        var product = new Product(sku, name, "pcs", 0m, "TRY");
        product.Id = id;
        return product;
    }
}
