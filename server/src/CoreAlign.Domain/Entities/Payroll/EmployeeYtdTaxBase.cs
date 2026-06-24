using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities.Payroll;

public class EmployeeYtdTaxBase : TenantEntity
{
    public Guid EmployeeId { get; private set; }
    public int Year { get; private set; }
    public decimal CumulativeIncomeTaxBase { get; private set; }
    public decimal CumulativeMinWageBase { get; private set; }
    public int LastPeriodMonth { get; private set; }

    protected EmployeeYtdTaxBase() { }

    public EmployeeYtdTaxBase(Guid employeeId, int year)
    {
        EmployeeId = employeeId;
        Year = year;
    }

    public void Accumulate(decimal incomeTaxBase, decimal minWageBase, int periodMonth)
    {
        CumulativeIncomeTaxBase = Math.Round(CumulativeIncomeTaxBase + incomeTaxBase, 4);
        CumulativeMinWageBase = Math.Round(CumulativeMinWageBase + minWageBase, 4);
        LastPeriodMonth = periodMonth;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
