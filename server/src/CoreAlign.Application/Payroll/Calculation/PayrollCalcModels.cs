using CoreAlign.Domain.Entities.Payroll;

namespace CoreAlign.Application.Payroll.Calculation;

public sealed record PayrollCalcInput(
    decimal GrossSalary,
    decimal PriorCumulativeIncomeTaxBase,
    decimal PriorCumulativeMinWageBase,
    bool IsSgkIncentiveEligible,
    decimal OtherDeductions,
    PayrollParameters Parameters);

public sealed record PayrollCalcResult(
    decimal SgkBase,
    decimal SgkEmployee,
    decimal UnemploymentEmployee,
    decimal SgkEmployer,
    decimal UnemploymentEmployer,
    decimal IncomeTaxBaseThisPeriod,
    decimal CumulativeIncomeTaxBaseAfter,
    decimal IncomeTaxGross,
    decimal MinWageIncomeTaxExemption,
    decimal IncomeTaxNet,
    decimal StampTaxGross,
    decimal StampTaxExemption,
    decimal StampTaxNet,
    decimal MinWageBaseAfter,
    decimal TotalDeductions,
    decimal NetPay,
    decimal EmployerCost);
