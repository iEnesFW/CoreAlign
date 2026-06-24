using CoreAlign.Domain.Entities.Payroll;

namespace CoreAlign.Application.Payroll.Calculation;

public sealed class PayrollCalculationService : IPayrollCalculationService
{
    public PayrollCalcResult Calculate(PayrollCalcInput input)
    {
        var p = input.Parameters;
        var gross = input.GrossSalary;

        var sgkBase = Round2(Math.Min(Math.Max(gross, p.SgkFloorMonthly), p.SgkCeilingMonthly));
        var sgkEmployee = Round2(sgkBase * p.SgkEmployeeRate);
        var unemploymentEmployee = Round2(sgkBase * p.UnemploymentEmployeeRate);
        var employerRate = input.IsSgkIncentiveEligible ? p.SgkEmployer5PointIncentiveRate : p.SgkEmployerRate;
        var sgkEmployer = Round2(sgkBase * employerRate);
        var unemploymentEmployer = Round2(sgkBase * p.UnemploymentEmployerRate);

        var incomeTaxBaseThisPeriod = Round2(gross - sgkEmployee - unemploymentEmployee);
        var cumulativeAfter = Round2(input.PriorCumulativeIncomeTaxBase + incomeTaxBaseThisPeriod);
        var incomeTaxGross = Round2(
            p.TaxOnCumulative(cumulativeAfter) - p.TaxOnCumulative(input.PriorCumulativeIncomeTaxBase));

        decimal minWageExemption;
        decimal minWageBaseAfter;
        if (p.MinWageExemptionEnabled)
        {
            var mwItBaseMonth = Round2(
                p.GrossMinimumWage * (1m - p.SgkEmployeeRate - p.UnemploymentEmployeeRate));
            var rawExemption = p.TaxOnCumulative(input.PriorCumulativeMinWageBase + mwItBaseMonth)
                - p.TaxOnCumulative(input.PriorCumulativeMinWageBase);
            minWageExemption = Round2(Math.Min(Math.Max(0m, rawExemption), incomeTaxGross));
            minWageBaseAfter = Round2(input.PriorCumulativeMinWageBase + mwItBaseMonth);
        }
        else
        {
            minWageExemption = 0m;
            minWageBaseAfter = Round2(input.PriorCumulativeMinWageBase);
        }

        var incomeTaxNet = Round2(Math.Max(0m, incomeTaxGross - minWageExemption));

        var stampTaxGross = Round2(gross * p.StampTaxRate);
        var stampTaxExemption = p.MinWageExemptionEnabled
            ? Round2(p.GrossMinimumWage * p.StampTaxRate)
            : 0m;
        var stampTaxNet = Round2(Math.Max(0m, stampTaxGross - stampTaxExemption));

        var totalDeductions = Round2(
            sgkEmployee + unemploymentEmployee + incomeTaxNet + stampTaxNet + input.OtherDeductions);
        var netPay = Round2(gross - totalDeductions);
        var employerCost = Round2(gross + sgkEmployer + unemploymentEmployer);

        return new PayrollCalcResult(
            SgkBase: sgkBase,
            SgkEmployee: sgkEmployee,
            UnemploymentEmployee: unemploymentEmployee,
            SgkEmployer: sgkEmployer,
            UnemploymentEmployer: unemploymentEmployer,
            IncomeTaxBaseThisPeriod: incomeTaxBaseThisPeriod,
            CumulativeIncomeTaxBaseAfter: cumulativeAfter,
            IncomeTaxGross: incomeTaxGross,
            MinWageIncomeTaxExemption: minWageExemption,
            IncomeTaxNet: incomeTaxNet,
            StampTaxGross: stampTaxGross,
            StampTaxExemption: stampTaxExemption,
            StampTaxNet: stampTaxNet,
            MinWageBaseAfter: minWageBaseAfter,
            TotalDeductions: totalDeductions,
            NetPay: netPay,
            EmployerCost: employerCost);
    }

    private static decimal Round2(decimal value) => Math.Round(value, 2, MidpointRounding.ToEven);
}
