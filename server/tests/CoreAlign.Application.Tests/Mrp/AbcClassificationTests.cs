using CoreAlign.Application.Mrp;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Mrp;

public class AbcClassificationTests
{
    [Fact]
    public void Classifier_ranks_by_descending_usage_value_into_80_95_bands()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var c = Guid.NewGuid();
        var d = Guid.NewGuid();

        // Total = 1000. Cumulative shares: A=80% (boundary item stays A),
        // B=95%, remainder C.
        var inputs = new[]
        {
            new AbcUsageInput(a, 800m),
            new AbcUsageInput(b, 150m),
            new AbcUsageInput(c, 40m),
            new AbcUsageInput(d, 10m),
        };

        var result = AbcClassifier.Classify(inputs).ToDictionary(r => r.ProductId, r => r.AbcClass);

        result[a].Should().Be(AbcClass.A);
        result[b].Should().Be(AbcClass.B);
        result[c].Should().Be(AbcClass.C);
        result[d].Should().Be(AbcClass.C);
    }

    [Fact]
    public void Classifier_marks_zero_usage_products_as_C()
    {
        var hot = Guid.NewGuid();
        var dead = Guid.NewGuid();

        var result = AbcClassifier.Classify(new[]
        {
            new AbcUsageInput(hot, 500m),
            new AbcUsageInput(dead, 0m),
        }).ToDictionary(r => r.ProductId, r => r.AbcClass);

        result[hot].Should().Be(AbcClass.A);
        result[dead].Should().Be(AbcClass.C);
    }

    [Fact]
    public void Classifier_marks_all_C_when_total_value_is_zero()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();

        var result = AbcClassifier.Classify(new[]
        {
            new AbcUsageInput(p1, 0m),
            new AbcUsageInput(p2, 0m),
        }).ToDictionary(r => r.ProductId, r => r.AbcClass);

        result[p1].Should().Be(AbcClass.C);
        result[p2].Should().Be(AbcClass.C);
    }

    [Fact]
    public async Task Handler_assigns_classes_and_applies_class_defaults_to_unconfigured_products()
    {
        var high = Product("HIGH");
        var mid = Product("MID");
        var low = Product("LOW");

        var loader = Substitute.For<IAbcUsageDataLoader>();
        loader.LoadAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<AbcProductUsage>
            {
                new(high, 800m),
                new(mid, 150m),
                new(low, 50m),
            });
        var products = Substitute.For<IProductRepository>();

        var handler = new ClassifyProductsAbcHandler(loader, products);

        var summary = await handler.Handle(new ClassifyProductsAbcCommand(), default);

        high.AbcClass.Should().Be(AbcClass.A);
        mid.AbcClass.Should().Be(AbcClass.B);
        low.AbcClass.Should().Be(AbcClass.C);

        // Defaults applied because none had an explicit override (MinMax + 0 service level).
        high.ServiceLevelTarget.Should().Be(0.98m);
        high.LotSizingPolicy.Should().Be(LotSizingPolicy.EconomicOrderQuantity);
        mid.ServiceLevelTarget.Should().Be(0.95m);
        low.ServiceLevelTarget.Should().Be(0.90m);

        summary.TotalEvaluated.Should().Be(3);
        summary.ClassA.Should().Be(1);
        summary.ClassB.Should().Be(1);
        summary.ClassC.Should().Be(1);
        summary.PolicyDefaultsApplied.Should().Be(3);
    }

    [Fact]
    public async Task Handler_does_not_override_products_with_explicit_planning_policy()
    {
        var configured = Product("CFG");
        configured.SetPlanningPolicy(
            LotSizingPolicy.FixedOrderQuantity,
            fixedOrderQuantity: 25m,
            orderMultiple: 0m,
            eoqAnnualDemand: 0m,
            orderingCost: 0m,
            holdingCostRate: 0m,
            serviceLevelTarget: 0.85m);

        var loader = Substitute.For<IAbcUsageDataLoader>();
        loader.LoadAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<AbcProductUsage> { new(configured, 900m) });
        var products = Substitute.For<IProductRepository>();

        var handler = new ClassifyProductsAbcHandler(loader, products);

        var summary = await handler.Handle(new ClassifyProductsAbcCommand(), default);

        configured.AbcClass.Should().Be(AbcClass.A);
        configured.ServiceLevelTarget.Should().Be(0.85m);
        configured.LotSizingPolicy.Should().Be(LotSizingPolicy.FixedOrderQuantity);
        summary.PolicyDefaultsApplied.Should().Be(0);
    }

    private static Product Product(string sku) =>
        new(sku, sku) { TenantId = Guid.NewGuid() };
}
