using CoreAlign.Application.Orders.EventHandlers;
using CoreAlign.Domain.Events;

namespace CoreAlign.Application.Tests.Orders;

public class BomResolverTests
{
    [Fact]
    public void Leaf_product_returns_itself_with_line_quantity()
    {
        var leaf = Guid.NewGuid();
        var tree = new Dictionary<Guid, IReadOnlyList<(Guid ComponentId, decimal Quantity)>>();

        var result = BomResolver.ExpandToLeaves(
            new[] { new OrderLineSnapshot(leaf, 3m) },
            tree);

        result.Should().ContainKey(leaf).WhoseValue.Should().Be(3m);
    }

    [Fact]
    public void Composite_decomposes_into_children_with_multiplier()
    {
        var bundle = Guid.NewGuid();
        var partA = Guid.NewGuid();
        var partB = Guid.NewGuid();
        var tree = new Dictionary<Guid, IReadOnlyList<(Guid ComponentId, decimal Quantity)>>
        {
            [bundle] = new[] { (partA, 2m), (partB, 5m) },
        };

        var result = BomResolver.ExpandToLeaves(
            new[] { new OrderLineSnapshot(bundle, 4m) },
            tree);

        result[partA].Should().Be(8m);
        result[partB].Should().Be(20m);
        result.Should().NotContainKey(bundle);
    }

    [Fact]
    public void Nested_composite_resolves_recursively()
    {
        var bundle = Guid.NewGuid();
        var subBundle = Guid.NewGuid();
        var leaf = Guid.NewGuid();
        var tree = new Dictionary<Guid, IReadOnlyList<(Guid ComponentId, decimal Quantity)>>
        {
            [bundle] = new[] { (subBundle, 3m) },
            [subBundle] = new[] { (leaf, 4m) },
        };

        var result = BomResolver.ExpandToLeaves(
            new[] { new OrderLineSnapshot(bundle, 2m) },
            tree);

        result[leaf].Should().Be(24m);
        result.Should().HaveCount(1);
    }

    [Fact]
    public void Shared_component_across_lines_aggregates()
    {
        var bundle1 = Guid.NewGuid();
        var bundle2 = Guid.NewGuid();
        var shared = Guid.NewGuid();
        var tree = new Dictionary<Guid, IReadOnlyList<(Guid ComponentId, decimal Quantity)>>
        {
            [bundle1] = new[] { (shared, 2m) },
            [bundle2] = new[] { (shared, 3m) },
        };

        var result = BomResolver.ExpandToLeaves(
            new[]
            {
                new OrderLineSnapshot(bundle1, 4m),
                new OrderLineSnapshot(bundle2, 5m),
            },
            tree);

        result[shared].Should().Be(4m * 2m + 5m * 3m);
    }
}
