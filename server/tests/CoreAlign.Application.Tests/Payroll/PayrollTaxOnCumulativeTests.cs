using CoreAlign.Domain.Entities.Payroll;

namespace CoreAlign.Application.Tests.Payroll;

public sealed class PayrollTaxOnCumulativeTests
{
    private static PayrollParameters WithSeededBrackets()
    {
        var parameters = new PayrollParameters(
            effectiveYear: 2026,
            effectiveFrom: new DateOnly(2026, 1, 1),
            sgkEmployeeRate: 0.14m,
            sgkEmployerRate: 0.205m,
            sgkEmployer5PointIncentiveRate: 0.155m,
            unemploymentEmployeeRate: 0.01m,
            unemploymentEmployerRate: 0.02m,
            sgkFloorMonthly: 26005.50m,
            sgkCeilingMultiplier: 7.5m,
            sgkCeilingMonthly: 195041.25m,
            stampTaxRate: 0.00759m,
            grossMinimumWage: 26005.50m,
            disability1Amount: 0m,
            disability2Amount: 0m,
            disability3Amount: 0m);

        parameters.AddTaxBracket(new PayrollTaxBracket(15m, 1, 158000m));
        parameters.AddTaxBracket(new PayrollTaxBracket(20m, 2, 330000m));
        parameters.AddTaxBracket(new PayrollTaxBracket(27m, 3, 1200000m));
        parameters.AddTaxBracket(new PayrollTaxBracket(35m, 4, 4300000m));
        parameters.AddTaxBracket(new PayrollTaxBracket(40m, 5, null));
        return parameters;
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(150000, 22500)]
    [InlineData(158000, 23700)]
    [InlineData(201000, 32300)]
    [InlineData(330000, 58100)]
    public void TaxOnCumulative_walks_marginal_brackets(decimal cumulativeBase, decimal expected)
    {
        var parameters = WithSeededBrackets();

        var tax = parameters.TaxOnCumulative(cumulativeBase);

        tax.Should().Be(expected);
    }

    [Fact]
    public void TaxOnCumulative_handles_top_open_ended_bracket()
    {
        var parameters = WithSeededBrackets();

        var below = 23700m + (330000m - 158000m) * 0.20m
            + (1200000m - 330000m) * 0.27m + (4300000m - 1200000m) * 0.35m;
        var expected = below + (5000000m - 4300000m) * 0.40m;

        var tax = parameters.TaxOnCumulative(5000000m);

        tax.Should().Be(expected);
    }

    [Fact]
    public void TaxOnCumulative_period_overload_is_difference_of_cumulatives()
    {
        var parameters = WithSeededBrackets();

        var periodTax = parameters.TaxOnCumulative(150000m, 51000m);

        periodTax.Should().Be(parameters.TaxOnCumulative(201000m) - parameters.TaxOnCumulative(150000m));
        periodTax.Should().Be(32300m - 22500m);
    }
}
