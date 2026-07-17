export type RoutingStatus = 'Draft' | 'Active' | 'Archived';

export type RoutingOperationType =
  | 'Cutting'
  | 'Edging'
  | 'Tempering'
  | 'Lamination'
  | 'Drilling'
  | 'Sandblasting'
  | 'Washing'
  | 'QualityControl'
  | 'Packaging'
  | 'Other';

export type OperatorQualificationLevel = 'Trainee' | 'Qualified' | 'Expert';

export interface RoutingStep {
  id: string;
  stepNumber: number;
  workCenterId: string;
  workCenterName: string;
  operationName: string;
  operationType: RoutingOperationType;
  setupTimeMinutes: number;
  runTimeMinutesPerUnit: number;
  runTimeMinutesPerSqm: number | null;
  scrapPercentage: number;
  instructions: string | null;
  isOptional: boolean;
}

export interface ProductionRouting {
  id: string;
  code: string;
  name: string;
  description: string | null;
  status: RoutingStatus;
  concurrencyToken: number;
  createdAtUtc: string;
  updatedAtUtc: string;
  steps: RoutingStep[];
}

export interface ProductionRoutingSummary {
  id: string;
  code: string;
  name: string;
  status: RoutingStatus;
  stepCount: number;
  updatedAtUtc: string;
}

export interface WorkCenter {
  id: string;
  code: string;
  name: string;
  dailyCapacityMinutes: number;
  isActive: boolean;
}

export interface WorkCenterOperator {
  id: string;
  workCenterId: string;
  workCenterCode: string;
  workCenterName: string;
  employeeId: string;
  employeeName: string;
  employeeActive: boolean;
  qualificationLevel: OperatorQualificationLevel;
  isPrimary: boolean;
  isActive: boolean;
  certifiedOn: string | null;
  notes: string | null;
}

export interface RoutingStepInput {
  stepNumber: number;
  workCenterId: string;
  operationName: string;
  operationType: RoutingOperationType;
  setupTimeMinutes: number;
  runTimeMinutesPerUnit: number;
  runTimeMinutesPerSqm: number | null;
  scrapPercentage: number;
  instructions: string | null;
  isOptional: boolean;
}

export interface CreateRoutingInput {
  code: string;
  name: string;
  description: string | null;
}

export interface UpdateRoutingInput extends CreateRoutingInput {
  id: string;
}

export interface SetRoutingStepsInput {
  routingId: string;
  steps: RoutingStepInput[];
}

export interface CreateWorkCenterInput {
  code: string;
  name: string;
  dailyCapacityMinutes: number;
}

export interface UpdateWorkCenterInput extends CreateWorkCenterInput {
  id: string;
  isActive: boolean;
}

export interface CreateWorkCenterOperatorInput {
  workCenterId: string;
  employeeId: string;
  qualificationLevel: OperatorQualificationLevel;
  isPrimary: boolean;
  certifiedOn: string | null;
  notes: string | null;
  pinCode: string | null;
}

export interface UpdateWorkCenterOperatorInput {
  id: string;
  qualificationLevel: OperatorQualificationLevel;
  isPrimary: boolean;
  isActive: boolean;
  certifiedOn: string | null;
  notes: string | null;
  pinCode: string | null;
}

export interface AssignRoutingToProductInput {
  productId: string;
  routingId: string | null;
}
