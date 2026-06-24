export interface PayrollTaxBracket {
  id: string;
  ratePercent: number;
  sortOrder: number;
  upperBound: number | null;
}

export interface PayrollParameters {
  id: string;
  tenantId: string;
  isGlobal: boolean;
  effectiveYear: number;
  effectiveFrom: string;
  effectiveTo: string | null;
  isActive: boolean;
  description: string | null;
  sgkEmployeeRate: number;
  sgkEmployerRate: number;
  sgkEmployer5PointIncentiveRate: number;
  unemploymentEmployeeRate: number;
  unemploymentEmployerRate: number;
  sgkFloorMonthly: number;
  sgkCeilingMultiplier: number;
  sgkCeilingMonthly: number;
  stampTaxRate: number;
  grossMinimumWage: number;
  minWageExemptionEnabled: boolean;
  disability1Amount: number;
  disability2Amount: number;
  disability3Amount: number;
  taxBrackets: PayrollTaxBracket[];
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface CreatePayrollTaxBracketInput {
  ratePercent: number;
  sortOrder: number;
  upperBound?: number | null;
}

export interface CreatePayrollParametersInput {
  effectiveYear: number;
  effectiveFrom: string;
  sgkEmployeeRate: number;
  sgkEmployerRate: number;
  sgkEmployer5PointIncentiveRate: number;
  unemploymentEmployeeRate: number;
  unemploymentEmployerRate: number;
  sgkFloorMonthly: number;
  sgkCeilingMultiplier: number;
  sgkCeilingMonthly: number;
  stampTaxRate: number;
  grossMinimumWage: number;
  disability1Amount: number;
  disability2Amount: number;
  disability3Amount: number;
  taxBrackets: CreatePayrollTaxBracketInput[];
  minWageExemptionEnabled?: boolean;
  effectiveTo?: string | null;
  description?: string | null;
}

export interface UpdatePayrollParametersInput {
  id: string;
  sgkEmployeeRate: number;
  sgkEmployerRate: number;
  sgkEmployer5PointIncentiveRate: number;
  unemploymentEmployeeRate: number;
  unemploymentEmployerRate: number;
  sgkFloorMonthly: number;
  sgkCeilingMultiplier: number;
  sgkCeilingMonthly: number;
  stampTaxRate: number;
  grossMinimumWage: number;
  disability1Amount: number;
  disability2Amount: number;
  disability3Amount: number;
  minWageExemptionEnabled: boolean;
  isActive: boolean;
  effectiveFrom: string;
  effectiveTo?: string | null;
  description?: string | null;
}
