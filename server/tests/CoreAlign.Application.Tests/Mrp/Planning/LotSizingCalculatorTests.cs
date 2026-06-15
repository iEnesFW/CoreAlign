using CoreAlign.Domain.Enums;
using CoreAlign.Infrastructure.Mrp.Planning;

namespace CoreAlign.Application.Tests.Mrp.Planning;

public class LotSizingCalculatorTests
{
    private readonly LotSizingCalculator _sut = new();
    private static readonly IReadOnlyList<decimal> NoUpcoming = new List<decimal>();

    [Fact]
    public void LotForLot_returns_exact_net_requirement()
    {
        var product = MrpPlanningTestData.Product(Guid.NewGuid(), "A", policy: LotSizingPolicy.LotForLot);
        var qty = _sut.Calculate(product, netRequirement: 37m, projectedAvailableBeforeReceipt: -37m, averageDailyDemand: 0m, NoUpcoming);
        qty.Should().Be(37m);
    }

    [Fact]
    public void NonPositive_net_requirement_returns_zero()
    {
        var product = MrpPlanningTestData.Product(Guid.NewGuid(), "A", policy: LotSizingPolicy.LotForLot);
        _sut.Calculate(product, 0m, 0m, 0m, NoUpcoming).Should().Be(0m);
        _sut.Calculate(product, -5m, 0m, 0m, NoUpcoming).Should().Be(0m);
    }

    [Theory]
    [InlineData(10, 25, 30)]
    [InlineData(10, 10, 10)]
    [InlineData(10, 1, 10)]
    public void FixedOrderQuantity_rounds_up_to_multiple(double foq, double net, double expected)
    {
        var product = MrpPlanningTestData.Product(Guid.NewGuid(), "A",
            policy: LotSizingPolicy.FixedOrderQuantity, fixedOrderQuantity: (decimal)foq);
        var qty = _sut.Calculate(product, (decimal)net, -(decimal)net, 0m, NoUpcoming);
        qty.Should().Be((decimal)expected);
    }

    [Fact]
    public void MinMax_raises_to_max_stock_target()
    {
        var product = MrpPlanningTestData.Product(Guid.NewGuid(), "A",
            policy: LotSizingPolicy.MinMax, reorderPoint: 50m, maxStock: 200m);
        var qty = _sut.Calculate(product, netRequirement: 40m, projectedAvailableBeforeReceipt: 10m, averageDailyDemand: 0m, NoUpcoming);
        qty.Should().Be(190m);
    }

    [Fact]
    public void MinMax_without_max_stock_uses_rop_times_two()
    {
        var product = MrpPlanningTestData.Product(Guid.NewGuid(), "A",
            policy: LotSizingPolicy.MinMax, reorderPoint: 50m);
        var qty = _sut.Calculate(product, netRequirement: 40m, projectedAvailableBeforeReceipt: 10m, averageDailyDemand: 0m, NoUpcoming);
        qty.Should().Be(90m);
    }

    [Fact]
    public void Eoq_uses_sqrt_2ds_over_h_then_satisfies_in_multiples()
    {
        var product = MrpPlanningTestData.Product(Guid.NewGuid(), "A",
            policy: LotSizingPolicy.EconomicOrderQuantity,
            eoqAnnualDemand: 12000m, orderingCost: 50m, holdingCostRate: 0.2m, unitCost: 6m);

        var eoq = (decimal)Math.Ceiling(Math.Sqrt((double)(2m * 12000m * 50m / (0.2m * 6m))));
        var qty = _sut.Calculate(product, netRequirement: 500m, projectedAvailableBeforeReceipt: -500m, averageDailyDemand: 0m, NoUpcoming);

        var multiples = Math.Ceiling(500m / eoq);
        qty.Should().Be(multiples * eoq);
    }

    [Fact]
    public void Eoq_falls_back_to_minmax_when_inputs_missing()
    {
        var product = MrpPlanningTestData.Product(Guid.NewGuid(), "A",
            policy: LotSizingPolicy.EconomicOrderQuantity, reorderPoint: 50m, maxStock: 200m);
        var qty = _sut.Calculate(product, netRequirement: 40m, projectedAvailableBeforeReceipt: 10m, averageDailyDemand: 0m, NoUpcoming);
        qty.Should().Be(190m);
    }

    [Fact]
    public void PeriodOrderQuantity_groups_upcoming_net_requirements()
    {
        var product = MrpPlanningTestData.Product(Guid.NewGuid(), "A", policy: LotSizingPolicy.PeriodOrderQuantity);
        var upcoming = new List<decimal> { 5m, 0m, 10m };
        var qty = _sut.Calculate(product, netRequirement: 20m, projectedAvailableBeforeReceipt: -20m, averageDailyDemand: 0m, upcoming);
        qty.Should().Be(35m);
    }

    [Fact]
    public void OrderMultiple_rounds_up_after_policy()
    {
        var product = MrpPlanningTestData.Product(Guid.NewGuid(), "A",
            policy: LotSizingPolicy.LotForLot, orderMultiple: 12m);
        var qty = _sut.Calculate(product, netRequirement: 25m, projectedAvailableBeforeReceipt: -25m, averageDailyDemand: 0m, NoUpcoming);
        qty.Should().Be(36m);
    }

    [Fact]
    public void MinOrderQuantity_enforced_then_remultipled()
    {
        var product = MrpPlanningTestData.Product(Guid.NewGuid(), "A",
            policy: LotSizingPolicy.LotForLot, orderMultiple: 10m, minOrderQuantity: 25m);
        var qty = _sut.Calculate(product, netRequirement: 4m, projectedAvailableBeforeReceipt: -4m, averageDailyDemand: 0m, NoUpcoming);
        qty.Should().Be(30m);
    }
}
