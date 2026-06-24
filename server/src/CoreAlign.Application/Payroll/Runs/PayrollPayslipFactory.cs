using CoreAlign.Domain.Entities.Payroll;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Payroll.Runs;

internal sealed record ResolvedDeduction(DeductionType DeductionType, decimal Amount, bool IsRecurring);

internal sealed record EmployeeEarnings(
    decimal Gross,
    IReadOnlyList<PayslipEarningLine> EarningLines,
    IReadOnlyList<ResolvedDeduction> Deductions,
    decimal OtherDeductionsTotal);

internal static class PayrollPayslipFactory
{
    public static EmployeeEarnings ResolveEarnings(Employee employee, DateOnly period)
    {
        var earningLines = new List<PayslipEarningLine>
        {
            new(SalaryComponentType.BaseSalary, employee.BaseSalaryGross, taxExempt: false, sgkExempt: false),
        };

        var gross = employee.BaseSalaryGross;
        foreach (var component in employee.SalaryComponents
            .Where(c => c.IsRecurring && c.IsCurrentlyValid(period))
            .OrderBy(c => c.ComponentType))
        {
            earningLines.Add(new PayslipEarningLine(component.ComponentType, component.Amount, component.TaxExempt, component.SgkExempt));
            if (!component.TaxExempt)
            {
                gross += component.Amount;
            }
        }
        gross = Math.Round(gross, 4);

        var deductions = new List<ResolvedDeduction>();
        foreach (var deduction in employee.Deductions
            .Where(d => d.IsCurrentlyValid(period))
            .OrderBy(d => d.Priority)
            .ThenBy(d => d.EffectiveFrom))
        {
            var amount = ResolveDeductionAmount(deduction, gross);
            if (amount <= 0m) continue;
            deductions.Add(new ResolvedDeduction(deduction.DeductionType, amount, deduction.Percent.HasValue));
        }

        var otherDeductionsTotal = Math.Round(deductions.Sum(d => d.Amount), 4);
        return new EmployeeEarnings(gross, earningLines, deductions, otherDeductionsTotal);
    }

    private static decimal ResolveDeductionAmount(EmployeeDeduction deduction, decimal gross)
    {
        if (deduction.Percent.HasValue)
        {
            return Math.Round(gross * deduction.Percent.Value / 100m, 4);
        }
        var amount = deduction.Amount ?? 0m;
        if (deduction.RemainingBalance > 0m)
        {
            amount = Math.Min(amount, deduction.RemainingBalance);
        }
        return Math.Round(amount, 4);
    }
}
