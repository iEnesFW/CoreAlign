import type { DeductionType, PayrollRunStatus, PayrollRunType, SalaryComponentType } from './enums';

export interface PayrollRunListItem {
  id: string;
  runNumber: string;
  periodYear: number;
  periodMonth: number;
  runType: PayrollRunType;
  status: PayrollRunStatus;
  currency: string;
  totalGross: number;
  totalNet: number;
  totalEmployerCost: number;
  payslipCount: number;
  calculatedAtUtc: string | null;
  approvedAtUtc: string | null;
  postedAtUtc: string | null;
  paidAtUtc: string | null;
  createdAtUtc: string;
}

export interface PayrollRun {
  id: string;
  runNumber: string;
  periodYear: number;
  periodMonth: number;
  runType: PayrollRunType;
  status: PayrollRunStatus;
  currency: string;
  parametersId: string;
  totalGross: number;
  totalSgkEmployee: number;
  totalSgkEmployer: number;
  totalUnemploymentEmployee: number;
  totalUnemploymentEmployer: number;
  totalIncomeTax: number;
  totalStampTax: number;
  totalDeductions: number;
  totalNet: number;
  totalEmployerCost: number;
  payslipCount: number;
  calculatedAtUtc: string | null;
  approvedByUserId: string | null;
  approvedAtUtc: string | null;
  postedAtUtc: string | null;
  paidAtUtc: string | null;
  createdAtUtc: string;
}

export interface PayslipEarningLine {
  id: string;
  componentType: SalaryComponentType;
  amount: number;
  taxExempt: boolean;
  sgkExempt: boolean;
}

export interface PayslipDeductionLine {
  id: string;
  deductionType: DeductionType;
  amount: number;
  isRecurring: boolean;
}

export interface Payslip {
  id: string;
  payslipNumber: string;
  runId: string;
  employeeId: string;
  employeeNumber: string;
  employeeFullName: string;
  nationalIdMasked: string | null;
  periodYear: number;
  periodMonth: number;
  daysWorked: number;
  parametersId: string;
  grossEarnings: number;
  sgkBase: number;
  incomeTaxBaseThisPeriod: number;
  cumulativeIncomeTaxBaseBefore: number;
  cumulativeIncomeTaxBaseAfter: number;
  cumulativeMinWageBaseBefore: number;
  cumulativeMinWageBaseAfter: number;
  sgkEmployee: number;
  unemploymentEmployee: number;
  incomeTaxGross: number;
  minWageIncomeTaxExemptionApplied: number;
  minWageStampTaxExemptionApplied: number;
  disabilityExemptionApplied: number;
  incomeTaxNet: number;
  stampTax: number;
  otherDeductionsTotal: number;
  netPay: number;
  sgkEmployer: number;
  unemploymentEmployer: number;
  employerCost: number;
  earningLines: PayslipEarningLine[];
  deductionLines: PayslipDeductionLine[];
}

export interface PayrollRunListParams {
  status?: PayrollRunStatus;
  periodYear?: number;
  page?: number;
  pageSize?: number;
}

export interface CreatePayrollRunInput {
  periodYear: number;
  periodMonth: number;
  runType?: PayrollRunType;
  currency?: string;
  description?: string | null;
}

export interface PayrollRunActionInput {
  id: string;
}
