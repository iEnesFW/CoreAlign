using CoreAlign.Application.Mrp;
using CoreAlign.Domain.Entities.Mrp;

namespace CoreAlign.Application.Tests.Mrp;

public class PeggingChainResolverTests
{
    private static MrpPegging Peg(
        Guid component,
        decimal qty,
        string sourceKind,
        Guid? parent,
        Guid? orderLine) =>
        new(component, qty, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), sourceKind, parent, orderLine);

    [Fact]
    public void ResolveUpstream_walks_component_to_root_sales_order()
    {
        var top = Guid.NewGuid();
        var sub = Guid.NewGuid();
        var raw = Guid.NewGuid();
        var orderLine = Guid.NewGuid();

        var pegs = new[]
        {
            Peg(top, 5m, "SalesOrder", null, orderLine),
            Peg(sub, 10m, "ProductionOrder", top, null),
            Peg(raw, 30m, "ProductionOrder", sub, null)
        };

        var chain = PeggingChainResolver.ResolveUpstream(pegs, raw);

        chain.Should().HaveCount(3);
        chain[0].ComponentProductId.Should().Be(raw);
        chain[1].ComponentProductId.Should().Be(sub);
        chain[2].ComponentProductId.Should().Be(top);
        chain[2].SourceKind.Should().Be("SalesOrder");
        chain[2].SourceOrderLineId.Should().Be(orderLine);
    }

    [Fact]
    public void ResolveUpstream_root_sales_order_returns_single_step()
    {
        var top = Guid.NewGuid();
        var orderLine = Guid.NewGuid();
        var pegs = new[] { Peg(top, 5m, "SalesOrder", null, orderLine) };

        var chain = PeggingChainResolver.ResolveUpstream(pegs, top);

        chain.Should().ContainSingle();
        chain[0].SourceParentProductId.Should().BeNull();
    }

    [Fact]
    public void ResolveUpstream_unknown_component_returns_empty()
    {
        var pegs = new[] { Peg(Guid.NewGuid(), 1m, "SalesOrder", null, Guid.NewGuid()) };

        var chain = PeggingChainResolver.ResolveUpstream(pegs, Guid.NewGuid());

        chain.Should().BeEmpty();
    }

    [Fact]
    public void ResolveUpstream_is_cycle_safe()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var pegs = new[]
        {
            Peg(a, 1m, "ProductionOrder", b, null),
            Peg(b, 1m, "ProductionOrder", a, null)
        };

        var chain = PeggingChainResolver.ResolveUpstream(pegs, a);

        chain.Should().HaveCountLessThanOrEqualTo(2, "the path guard must stop a parent cycle");
    }

    [Fact]
    public void ResolveUpstream_picks_dominant_quantity_peg_when_multiple_parents()
    {
        var component = Guid.NewGuid();
        var smallParent = Guid.NewGuid();
        var bigParent = Guid.NewGuid();
        var bigOrderLine = Guid.NewGuid();

        var pegs = new[]
        {
            Peg(component, 2m, "ProductionOrder", smallParent, null),
            Peg(component, 50m, "ProductionOrder", bigParent, null),
            Peg(bigParent, 50m, "SalesOrder", null, bigOrderLine)
        };

        var chain = PeggingChainResolver.ResolveUpstream(pegs, component);

        chain[0].SourceParentProductId.Should().Be(bigParent);
        chain.Last().SourceOrderLineId.Should().Be(bigOrderLine);
    }
}
