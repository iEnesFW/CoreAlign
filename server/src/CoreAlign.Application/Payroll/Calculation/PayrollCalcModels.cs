using CoreAlign.Domain.Entities.Payroll;

namespace CoreAlign.Application.Payroll.Calculation;

public sealed record PayrollCalcInput(
    decimal GrossSalary,
    decimal PriorCumulativeIncomeTaxBase,
    decimal PriorCumulativeMinWageBase,
    bool IsSgkIncentiveEligible,
    decimal OtherDeductions,
    PayrollParameters Parameters,
    // SGK base gross (excludes SgkExempt components). Defaults to GrossSalary so a caller that does
    // not distinguish keeps the prior single-gross behaviour.
    decimal? SgkGrossSalary = null);

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
