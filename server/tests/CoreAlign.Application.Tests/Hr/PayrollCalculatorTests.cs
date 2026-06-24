using CoreAlign.Application.Payroll.Calculation;
using CoreAlign.Domain.Entities.Payroll;

namespace CoreAlign.Application.Tests.Hr;

public sealed class PayrollCalculatorTests
{
    private static PayrollParameters SeededParameters(
        bool minWageExemptionEnabled = true,
        decimal sgkEmployerRate = 0.205m,
        decimal sgkEmployer5PointIncentiveRate = 0.155m)
    {
        var parameters = new PayrollParameters(
            effectiveYear: 2026,
            effectiveFrom: new DateOnly(2026, 1, 1),
            sgkEmployeeRate: 0.14m,
            sgkEmployerRate: sgkEmployerRate,
            sgkEmployer5PointIncentiveRate: sgkEmployer5PointIncentiveRate,
            unemploymentEmployeeRate: 0.01m,
            unemploymentEmployerRate: 0.02m,
            sgkFloorMonthly: 26005.50m,
            sgkCeilingMultiplier: 7.5m,
            sgkCeilingMonthly: 195041.25m,
            stampTaxRate: 0.00759m,
            grossMinimumWage: 26005.50m,
            disability1Amount: 0m,
            disability2Amount: 0m,
            disability3Amount: 0m,
            minWageExemptionEnabled: minWageExemptionEnabled);

        parameters.AddTaxBracket(new PayrollTaxBracket(15m, 1, 158000m));
        parameters.AddTaxBracket(new PayrollTaxBracket(20m, 2, 330000m));
        parameters.AddTaxBracket(new PayrollTaxBracket(27m, 3, 1200000m));
        parameters.AddTaxBracket(new PayrollTaxBracket(35m, 4, 4300000m));
        parameters.AddTaxBracket(new PayrollTaxBracket(40m, 5, null));
        return parameters;
    }

    private static readonly IPayrollCalculationService Calculator = new PayrollCalculationService();

    [Fact]
    public void T1_worked_example_matches_every_component_to_two_decimals()
    {
        var input = new PayrollCalcInput(
            GrossSalary: 60000m,
            PriorCumulativeIncomeTaxBase: 150000m,
            PriorCumulativeMinWageBase: 132628.08m,
            IsSgkIncentiveEligible: false,
            OtherDeductions: 0m,
            Parameters: SeededParameters());

        var r = Calculator.Calculate(input);

        r.SgkBase.Should().Be(60000.00m);
        r.SgkEmployee.Should().Be(8400.00m);
        r.UnemploymentEmployee.Should().Be(600.00m);
        r.SgkEmployer.Should().Be(12300.00m);
        r.UnemploymentEmployer.Should().Be(1200.00m);
        r.IncomeTaxBaseThisPeriod.Should().Be(51000.00m);
        r.CumulativeIncomeTaxBaseAfter.Should().Be(201000.00m);
        r.IncomeTaxGross.Should().Be(9800.00m);
        r.MinWageIncomeTaxExemption.Should().Be(3315.70m);
        r.IncomeTaxNet.Should().Be(6484.30m);
        r.StampTaxGross.Should().Be(455.40m);
        r.StampTaxExemption.Should().Be(197.38m);
        r.StampTaxNet.Should().Be(258.02m);
        r.TotalDeductions.Should().Be(15742.32m);
        r.NetPay.Should().Be(44257.68m);
        r.EmployerCost.Should().Be(73500.00m);
    }

    [Fact]
    public void T2_income_tax_gross_splits_across_a_bracket_transition()
    {
        var input = new PayrollCalcInput(
            GrossSalary: 60000m,
            PriorCumulativeIncomeTaxBase: 150000m,
            PriorCumulativeMinWageBase: 0m,
            IsSgkIncentiveEligible: false,
            OtherDeductions: 0m,
            Parameters: SeededParameters(minWageExemptionEnabled: false));

        var r = Calculator.Calculate(input);

        var portionAt15 = (158000m - 150000m) * 0.15m;
        var portionAt20 = (201000m - 158000m) * 0.20m;

        r.CumulativeIncomeTaxBaseAfter.Should().Be(201000.00m);
        r.IncomeTaxGross.Should().Be(portionAt15 + portionAt20);
        r.IncomeTaxGross.Should().Be(9800.00m);
    }

    [Fact]
    public void T3_sgk_base_is_capped_at_the_monthly_ceiling()
    {
        var input = new PayrollCalcInput(
            GrossSalary: 300000m,
            PriorCumulativeIncomeTaxBase: 0m,
            PriorCumulativeMinWageBase: 0m,
            IsSgkIncentiveEligible: false,
            OtherDeductions: 0m,
            Parameters: SeededParameters());

        var r = Calculator.Calculate(input);

        r.SgkBase.Should().Be(195041.25m);
        r.SgkEmployee.Should().Be(27305.78m);
        r.UnemploymentEmployee.Should().Be(1950.41m);
        r.SgkEmployer.Should().Be(39983.46m);
        r.UnemploymentEmployer.Should().Be(3900.82m);
    }

    [Fact]
    public void T4_min_wage_exemption_uses_gib_method_not_employee_marginal()
    {
        var enabled = new PayrollCalcInput(
            GrossSalary: 60000m,
            PriorCumulativeIncomeTaxBase: 150000m,
            PriorCumulativeMinWageBase: 132628.08m,
            IsSgkIncentiveEligible: false,
            OtherDeductions: 0m,
            Parameters: SeededParameters());

        var withExemption = Calculator.Calculate(enabled);

        var mwItBaseMonth = 22104.68m;
        var employeeMarginal = Math.Round(mwItBaseMonth * 0.20m, 2, MidpointRounding.ToEven);

        employeeMarginal.Should().Be(4420.94m);
        withExemption.MinWageIncomeTaxExemption.Should().Be(3315.70m);
        withExemption.MinWageIncomeTaxExemption.Should().NotBe(employeeMarginal);

        var disabled = new PayrollCalcInput(
            GrossSalary: 60000m,
            PriorCumulativeIncomeTaxBase: 150000m,
            PriorCumulativeMinWageBase: 132628.08m,
            IsSgkIncentiveEligible: false,
            OtherDeductions: 0m,
            Parameters: SeededParameters(minWageExemptionEnabled: false));

        var withoutExemption = Calculator.Calculate(disabled);

        withoutExemption.MinWageIncomeTaxExemption.Should().Be(0m);
        (withoutExemption.IncomeTaxNet - withExemption.IncomeTaxNet)
            .Should().Be(withExemption.MinWageIncomeTaxExemption);
    }

    [Fact]
    public void T5_mid_year_hire_with_zero_prior_cumulative_taxes_from_bracket_one()
    {
        var input = new PayrollCalcInput(
            GrossSalary: 60000m,
            PriorCumulativeIncomeTaxBase: 0m,
            PriorCumulativeMinWageBase: 0m,
            IsSgkIncentiveEligible: false,
            OtherDeductions: 0m,
            Parameters: SeededParameters(minWageExemptionEnabled: false));

        var r = Calculator.Calculate(input);

        r.IncomeTaxBaseThisPeriod.Should().Be(51000.00m);
        r.CumulativeIncomeTaxBaseAfter.Should().Be(51000.00m);
        r.IncomeTaxGross.Should().Be(7650.00m);
    }

    [Fact]
    public void T6_midpoint_components_round_half_to_even()
    {
        var input = new PayrollCalcInput(
            GrossSalary: 26005.50m,
            PriorCumulativeIncomeTaxBase: 0m,
            PriorCumulativeMinWageBase: 0m,
            IsSgkIncentiveEligible: false,
            OtherDeductions: 0m,
            Parameters: SeededParameters(minWageExemptionEnabled: false));

        var r = Calculator.Calculate(input);

        r.StampTaxGross.Should().Be(197.38m);
        r.UnemploymentEmployee.Should().Be(260.06m);
    }

    [Fact]
    public void T7_employer_rate_injection_drives_employer_cost_with_no_hardcoded_constants()
    {
        var highRate = new PayrollCalcInput(
            GrossSalary: 60000m,
            PriorCumulativeIncomeTaxBase: 0m,
            PriorCumulativeMinWageBase: 0m,
            IsSgkIncentiveEligible: true,
            OtherDeductions: 0m,
            Parameters: SeededParameters(sgkEmployer5PointIncentiveRate: 0.205m));

        var lowRate = new PayrollCalcInput(
            GrossSalary: 60000m,
            PriorCumulativeIncomeTaxBase: 0m,
            PriorCumulativeMinWageBase: 0m,
            IsSgkIncentiveEligible: true,
            OtherDeductions: 0m,
            Parameters: SeededParameters(sgkEmployer5PointIncentiveRate: 0.155m));

        Calculator.Calculate(highRate).EmployerCost.Should().Be(73500.00m);
        Calculator.Calculate(lowRate).EmployerCost.Should().Be(70500.00m);
    }
}
