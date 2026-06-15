using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Application.Tests.Inventory;

public class StockCountDomainTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid WarehouseId = Guid.NewGuid();

    private static StockCount NewCount(params StockCountLine[] lines)
    {
        var c = new StockCount("SC-1", WarehouseId, "WH1", "Main", DateTime.UtcNow)
        {
            TenantId = TenantId,
        };
        if (lines.Length > 0) c.ReplaceLines(lines);
        return c;
    }

    private static StockCountLine NewLine(decimal expected, decimal unitCost, Guid? productId = null) =>
        new(productId ?? Guid.NewGuid(), "SKU", "Widget", expected, unitCost);

    [Fact]
    public void BeginCounting_requires_lines()
    {
        var c = NewCount();
        var act = () => c.BeginCounting();
        act.Should().Throw<InvalidStockCountStateException>();
    }

    [Fact]
    public void BeginCounting_transitions_plan_to_counting()
    {
        var c = NewCount(NewLine(10m, 5m));
        c.BeginCounting();
        c.Status.Should().Be(StockCountStatus.Counting);
        c.CountingStartedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void RecordCount_computes_variance_quantity_and_cost()
    {
        var c = NewCount(NewLine(10m, 5m));
        c.BeginCounting();
        var line = c.Lines.First();
        c.RecordLineCount(line.Id, 8m, Guid.NewGuid(), null);
        line.CountedQuantity.Should().Be(8m);
        line.VarianceQuantity.Should().Be(-2m);
        line.VarianceCost.Should().Be(-10m);
        line.IsCounted.Should().BeTrue();
    }

    [Fact]
    public void Reconcile_requires_all_lines_counted()
    {
        var c = NewCount(NewLine(10m, 5m), NewLine(4m, 3m));
        c.BeginCounting();
        c.RecordLineCount(c.Lines.First().Id, 10m, null, null);
        var act = () => c.Reconcile(null);
        act.Should().Throw<InvalidStockCountStateException>();
    }

    [Fact]
    public void Reconcile_transitions_when_every_line_is_counted()
    {
        var c = NewCount(NewLine(10m, 5m), NewLine(4m, 3m));
        c.BeginCounting();
        foreach (var l in c.Lines) c.RecordLineCount(l.Id, l.ExpectedQuantity, null, null);
        c.Reconcile("ok");
        c.Status.Should().Be(StockCountStatus.Reconciliation);
        c.ReconciledAtUtc.Should().NotBeNull();
        c.Notes.Should().Be("ok");
    }

    [Fact]
    public void MarkPosted_only_allowed_from_reconciliation()
    {
        var c = NewCount(NewLine(10m, 5m));
        c.BeginCounting();
        var act = () => c.MarkPosted(Guid.NewGuid());
        act.Should().Throw<InvalidStockCountStateException>();
    }

    [Fact]
    public void TotalVariance_aggregates_lines()
    {
        var c = NewCount(NewLine(10m, 5m), NewLine(4m, 3m));
        c.BeginCounting();
        c.RecordLineCount(c.Lines.ElementAt(0).Id, 8m, null, null);
        c.RecordLineCount(c.Lines.ElementAt(1).Id, 6m, null, null);
        c.TotalVarianceQuantity.Should().Be(0m);
        c.TotalVarianceCost.Should().Be(-10m + 6m);
    }
}
