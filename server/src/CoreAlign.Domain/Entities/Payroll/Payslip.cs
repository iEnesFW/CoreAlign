using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Entities.Payroll;

public class Payslip : TenantEntity, IHasConcurrencyToken
{
    public long ConcurrencyToken { get; private set; }
    void IHasConcurrencyToken.BumpConcurrencyToken() => ConcurrencyToken++;

    public string PayslipNumber { get; private set; } = string.Empty;
    public Guid RunId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public string EmployeeNumber { get; private set; } = string.Empty;
    public string EmployeeFullName { get; private set; } = string.Empty;
    public string NationalId { get; private set; } = string.Empty;
    public int PeriodYear { get; private set; }
    public int PeriodMonth { get; private set; }
    public int DaysWorked { get; private set; } = 30;
    public Guid ParametersId { get; private set; }

    public decimal GrossEarnings { get; private set; }
    public decimal SgkBase { get; private set; }
    public decimal IncomeTaxBaseThisPeriod { get; private set; }
    public decimal CumulativeIncomeTaxBaseBefore { get; private set; }
    public decimal CumulativeIncomeTaxBaseAfter { get; private set; }
    public decimal CumulativeMinWageBaseBefore { get; private set; }
    public decimal CumulativeMinWageBaseAfter { get; private set; }
    public decimal SgkEmployee { get; private set; }
    public decimal UnemploymentEmployee { get; private set; }
    public decimal IncomeTaxGross { get; private set; }
    public decimal MinWageIncomeTaxExemptionApplied { get; private set; }
    public decimal MinWageStampTaxExemptionApplied { get; private set; }
    public decimal DisabilityExemptionApplied { get; private set; }
    public decimal IncomeTaxNet { get; private set; }
    public decimal StampTax { get; private set; }
    public decimal OtherDeductionsTotal { get; private set; }
    public decimal NetPay { get; private set; }
    public decimal SgkEmployer { get; private set; }
    public decimal UnemploymentEmployer { get; private set; }
    public decimal EmployerCost { get; private set; }

    public PayrollRun Run { get; private set; } = null!;
    public Employee Employee { get; private set; } = null!;
    public ICollection<PayslipEarningLine> EarningLines { get; private set; } = new List<PayslipEarningLine>();
    public ICollection<PayslipDeductionLine> DeductionLines { get; private set; } = new List<PayslipDeductionLine>();

    protected Payslip() { }

    public Payslip(
        string payslipNumber,
        Guid runId,
        Guid employeeId,
        string employeeNumber,
        string employeeFullName,
        string nationalId,
        int periodYear,
        int periodMonth,
        Guid parametersId,
        int daysWorked = 30)
    {
        if (string.IsNullOrWhiteSpace(payslipNumber))
        {
            throw new ArgumentException("Payslip number is required.", nameof(payslipNumber));
        }
        PayslipNumber = payslipNumber.Trim();
        RunId = runId;
        EmployeeId = employeeId;
        EmployeeNumber = employeeNumber;
        EmployeeFullName = employeeFullName;
        NationalId = nationalId;
        PeriodYear = periodYear;
        PeriodMonth = periodMonth;
        ParametersId = parametersId;
        DaysWorked = daysWorked;
    }

    public void ApplyComputation(
        decimal grossEarnings,
        decimal sgkBase,
        decimal incomeTaxBaseThisPeriod,
        decimal cumulativeIncomeTaxBaseBefore,
        decimal cumulativeIncomeTaxBaseAfter,
        decimal cumulativeMinWageBaseBefore,
        decimal cumulativeMinWageBaseAfter,
        decimal sgkEmployee,
        decimal unemploymentEmployee,
        decimal incomeTaxGross,
        decimal minWageIncomeTaxExemptionApplied,
        decimal minWageStampTaxExemptionApplied,
        decimal disabilityExemptionApplied,
        decimal incomeTaxNet,
        decimal stampTax,
        decimal otherDeductionsTotal,
        decimal netPay,
        decimal sgkEmployer,
        decimal unemploymentEmployer,
        decimal employerCost)
    {
        GrossEarnings = Math.Round(grossEarnings, 4);
        SgkBase = Math.Round(sgkBase, 4);
        IncomeTaxBaseThisPeriod = Math.Round(incomeTaxBaseThisPeriod, 4);
        CumulativeIncomeTaxBaseBefore = Math.Round(cumulativeIncomeTaxBaseBefore, 4);
        CumulativeIncomeTaxBaseAfter = Math.Round(cumulativeIncomeTaxBaseAfter, 4);
        CumulativeMinWageBaseBefore = Math.Round(cumulativeMinWageBaseBefore, 4);
        CumulativeMinWageBaseAfter = Math.Round(cumulativeMinWageBaseAfter, 4);
        SgkEmployee = Math.Round(sgkEmployee, 4);
        UnemploymentEmployee = Math.Round(unemploymentEmployee, 4);
        IncomeTaxGross = Math.Round(incomeTaxGross, 4);
        MinWageIncomeTaxExemptionApplied = Math.Round(minWageIncomeTaxExemptionApplied, 4);
        MinWageStampTaxExemptionApplied = Math.Round(minWageStampTaxExemptionApplied, 4);
        DisabilityExemptionApplied = Math.Round(disabilityExemptionApplied, 4);
        IncomeTaxNet = Math.Round(incomeTaxNet, 4);
        StampTax = Math.Round(stampTax, 4);
        OtherDeductionsTotal = Math.Round(otherDeductionsTotal, 4);
        NetPay = Math.Round(netPay, 4);
        SgkEmployer = Math.Round(sgkEmployer, 4);
        UnemploymentEmployer = Math.Round(unemploymentEmployer, 4);
        EmployerCost = Math.Round(employerCost, 4);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void AddEarningLine(PayslipEarningLine line)
    {
        line.AttachToPayslip(Id);
        EarningLines.Add(line);
    }

    public void AddDeductionLine(PayslipDeductionLine line)
    {
        line.AttachToPayslip(Id);
        DeductionLines.Add(line);
    }

    public void Anonymize(DateTime utcNow)
    {
        NationalId = "[silinmiş]";
        EmployeeFullName = "[silinmiş çalışan]";
        UpdatedAtUtc = utcNow;
    }
}

public class PayslipEarningLine : TenantEntity
{
    public Guid PayslipId { get; internal set; }
    public SalaryComponentType ComponentType { get; private set; }
    public decimal Amount { get; private set; }
    public bool TaxExempt { get; private set; }
    public bool SgkExempt { get; private set; }

    public Payslip Payslip { get; private set; } = null!;

    protected PayslipEarningLine() { }

    public PayslipEarningLine(SalaryComponentType componentType, decimal amount, bool taxExempt = false, bool sgkExempt = false)
    {
        ComponentType = componentType;
        Amount = Math.Round(amount, 4);
        TaxExempt = taxExempt;
        SgkExempt = sgkExempt;
    }

    internal void AttachToPayslip(Guid payslipId) => PayslipId = payslipId;
}

public class PayslipDeductionLine : TenantEntity
{
    public Guid PayslipId { get; internal set; }
    public DeductionType DeductionType { get; private set; }
    public decimal Amount { get; private set; }
    public bool IsRecurring { get; private set; }

    public Payslip Payslip { get; private set; } = null!;

    protected PayslipDeductionLine() { }

    public PayslipDeductionLine(DeductionType deductionType, decimal amount, bool isRecurring = false)
    {
        DeductionType = deductionType;
        Amount = Math.Round(amount, 4);
        IsRecurring = isRecurring;
    }

    internal void AttachToPayslip(Guid payslipId) => PayslipId = payslipId;
}
