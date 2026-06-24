import type {
  DeductionType,
  DisabilityDegree,
  EmploymentStatus,
  EmploymentType,
  SalaryBasis,
  SalaryComponentType,
} from './enums';

export interface EmployeeListItem {
  id: string;
  employeeNumber: string;
  firstName: string;
  lastName: string;
  fullName: string;
  nationalIdMasked: string | null;
  status: EmploymentStatus;
  employmentType: EmploymentType;
  department: string | null;
  title: string | null;
  hireDate: string;
  terminationDate: string | null;
  baseSalaryGross: number;
  salaryCurrency: string;
  ibanMasked: string | null;
}

export interface SalaryComponent {
  id: string;
  componentType: SalaryComponentType;
  amount: number;
  isRecurring: boolean;
  taxExempt: boolean;
  sgkExempt: boolean;
  effectiveFrom: string;
  effectiveTo: string | null;
  isActive: boolean;
}

export interface EmployeeDeduction {
  id: string;
  deductionType: DeductionType;
  amount: number | null;
  percent: number | null;
  remainingBalance: number;
  priority: number;
  effectiveFrom: string;
  effectiveTo: string | null;
  isActive: boolean;
}

export interface Employee {
  id: string;
  employeeNumber: string;
  firstName: string;
  lastName: string;
  fullName: string;
  nationalIdMasked: string | null;
  sgkRegistrationNo: string | null;
  email: string | null;
  phone: string | null;
  hireDate: string;
  terminationDate: string | null;
  status: EmploymentStatus;
  department: string | null;
  title: string | null;
  employmentType: EmploymentType;
  salaryBasis: SalaryBasis;
  baseSalaryGross: number;
  salaryCurrency: string;
  ibanMasked: string | null;
  bankName: string | null;
  isSgkIncentiveEligible: boolean;
  disabilityDegree: DisabilityDegree;
  isRetiredWorking: boolean;
  sgkExempt: boolean;
  dependentCount: number;
  spouseEmployed: boolean;
  terminationReason: string | null;
  salaryComponents: SalaryComponent[];
  deductions: EmployeeDeduction[];
  createdAtUtc: string;
}

export interface EmployeeListParams {
  search?: string;
  status?: EmploymentStatus;
  page?: number;
  pageSize?: number;
}

export interface CreateEmployeeInput {
  firstName: string;
  lastName: string;
  nationalId: string;
  hireDate: string;
  baseSalaryGross: number;
  employmentType?: EmploymentType;
  salaryBasis?: SalaryBasis;
  salaryCurrency?: string;
  sgkRegistrationNo?: string | null;
  email?: string | null;
  phone?: string | null;
  department?: string | null;
  title?: string | null;
  iban?: string | null;
  bankName?: string | null;
  isSgkIncentiveEligible?: boolean;
  disabilityDegree?: DisabilityDegree;
  isRetiredWorking?: boolean;
  sgkExempt?: boolean;
  dependentCount?: number;
  spouseEmployed?: boolean;
}

export interface UpdateEmployeeInput {
  id: string;
  firstName: string;
  lastName: string;
  email?: string | null;
  phone?: string | null;
  department?: string | null;
  title?: string | null;
  iban?: string | null;
  bankName?: string | null;
  dependentCount: number;
  spouseEmployed: boolean;
}

export interface UpdateBaseSalaryInput {
  id: string;
  baseSalaryGross: number;
  effectiveDate: string;
}

export interface TerminateEmployeeInput {
  id: string;
  terminationDate: string;
  reason?: string | null;
}

export interface LeaveInput {
  id: string;
}

export interface ReturnFromLeaveInput {
  id: string;
}

export interface SalaryComponentInput {
  id: string;
  componentType: SalaryComponentType;
  amount: number;
  effectiveFrom: string;
  isRecurring: boolean;
  taxExempt: boolean;
  sgkExempt: boolean;
  effectiveTo?: string | null;
}

export interface UpdateSalaryComponentInput extends SalaryComponentInput {
  componentId: string;
}

export interface DeductionInput {
  id: string;
  deductionType: DeductionType;
  effectiveFrom: string;
  amount?: number | null;
  percent?: number | null;
  remainingBalance: number;
  priority: number;
  effectiveTo?: string | null;
}

export interface UpdateDeductionInput extends DeductionInput {
  deductionId: string;
}
