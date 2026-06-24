export type EmploymentStatus = 'Active' | 'OnLeave' | 'Terminated';

export type EmploymentType = 'FullTime' | 'PartTime' | 'Seasonal';

export type SalaryBasis = 'Gross' | 'Net';

export type DisabilityDegree = 'None' | 'Degree1' | 'Degree2' | 'Degree3';

export type PayrollRunStatus = 'Draft' | 'Calculated' | 'Approved' | 'Posted' | 'Paid';

export type PayrollRunType = 'Regular' | 'OffCycle';

export type SalaryComponentType =
  | 'BaseSalary'
  | 'Meal'
  | 'Transport'
  | 'Bonus'
  | 'Premium'
  | 'Overtime'
  | 'Family'
  | 'Child';

export type DeductionType =
  | 'Advance'
  | 'Garnishment'
  | 'UnionDues'
  | 'PrivatePensionBES'
  | 'Custom';

export const EMPLOYMENT_STATUSES: EmploymentStatus[] = ['Active', 'OnLeave', 'Terminated'];

export const EMPLOYMENT_TYPES: EmploymentType[] = ['FullTime', 'PartTime', 'Seasonal'];

export const SALARY_BASES: SalaryBasis[] = ['Gross', 'Net'];

export const PAYROLL_RUN_STATUSES: PayrollRunStatus[] = [
  'Draft',
  'Calculated',
  'Approved',
  'Posted',
  'Paid',
];

export const SALARY_COMPONENT_TYPES: SalaryComponentType[] = [
  'BaseSalary',
  'Meal',
  'Transport',
  'Bonus',
  'Premium',
  'Overtime',
  'Family',
  'Child',
];

export const DEDUCTION_TYPES: DeductionType[] = [
  'Advance',
  'Garnishment',
  'UnionDues',
  'PrivatePensionBES',
  'Custom',
];
