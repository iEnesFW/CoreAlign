using CoreAlign.Domain.Entities.Payroll;
using CoreAlign.Domain.Enums;
using CoreAlign.Application.Accounting.Services;

namespace CoreAlign.Application.Payroll.GL;

public sealed record PayrollRunTotals(
    decimal TotalGross,
    decimal TotalSgkEmployee,
    decimal TotalSgkEmployer,
    decimal TotalUnemploymentEmployee,
    decimal TotalUnemploymentEmployer,
    decimal TotalIncomeTax,
    decimal TotalStampTax,
    decimal TotalNet,
    decimal TotalOtherDeductions)
{
    public static PayrollRunTotals From(PayrollRun run) => new(
        run.TotalGross,
        run.TotalSgkEmployee,
        run.TotalSgkEmployer,
        run.TotalUnemploymentEmployee,
        run.TotalUnemploymentEmployer,
        run.TotalIncomeTax,
        run.TotalStampTax,
        run.TotalNet,
        run.TotalDeductions
            - run.TotalSgkEmployee
            - run.TotalUnemploymentEmployee
            - run.TotalIncomeTax
            - run.TotalStampTax);
}

public static class PayrollGLLines
{
    public static IReadOnlyList<GLPostingLine> Accrual(PayrollRunTotals totals, bool reverse)
    {
        var laborExpense = totals.TotalGross + totals.TotalSgkEmployer + totals.TotalUnemploymentEmployer;
        var personnelNetPayable = totals.TotalNet + totals.TotalOtherDeductions;
        var taxesPayable = totals.TotalIncomeTax + totals.TotalStampTax;
        var sgkPayable = totals.TotalSgkEmployee + totals.TotalUnemploymentEmployee
            + totals.TotalSgkEmployer + totals.TotalUnemploymentEmployer;

        return reverse
            ? new[]
            {
                new GLPostingLine(GLPostingKey.LaborExpense, 0m, laborExpense),
                new GLPostingLine(GLPostingKey.PersonnelNetPayable, personnelNetPayable, 0m),
                new GLPostingLine(GLPostingKey.TaxesPayable, taxesPayable, 0m),
                new GLPostingLine(GLPostingKey.SgkPayable, sgkPayable, 0m),
            }
            : new[]
            {
                new GLPostingLine(GLPostingKey.LaborExpense, laborExpense, 0m),
                new GLPostingLine(GLPostingKey.PersonnelNetPayable, 0m, personnelNetPayable),
                new GLPostingLine(GLPostingKey.TaxesPayable, 0m, taxesPayable),
                new GLPostingLine(GLPostingKey.SgkPayable, 0m, sgkPayable),
            };
    }
}
