import type { RoutingOperationType } from './manufacturing.types';

export type ProductionJobStatus =
  | 'Draft'
  | 'Released'
  | 'InProgress'
  | 'OnHold'
  | 'ReadyToComplete'
  | 'Completed'
  | 'Cancelled';

export type ProductionJobStepStatus =
  | 'Pending'
  | 'InProgress'
  | 'Completed'
  | 'Skipped'
  | 'Reopened';

export interface ProductionJobListSummary {
  id: string;
  jobNumber: string;
  productId: string;
  productName: string;
  status: ProductionJobStatus;
  plannedQuantity: number;
  completedQuantity: number;
  scrappedQuantity: number;
  unitOfMeasure: string;
  currentStepNumber: number | null;
  stepCount: number;
  dueDateUtc: string | null;
  createdAtUtc: string;
}

export interface ProductionJobDetail {
  id: string;
  jobNumber: string;
  productId: string;
  productName: string;
  status: ProductionJobStatus;
  plannedQuantity: number;
  completedQuantity: number;
  scrappedQuantity: number;
  unitOfMeasure: string;
  warehouseId: string | null;
  sourceRoutingId: string | null;
  routingCodeSnapshot: string | null;
  routingNameSnapshot: string | null;
  routingSnapshotVersion: number | null;
  currentStepNumber: number | null;
  plannedStartDateUtc: string | null;
  dueDateUtc: string | null;
  releasedAtUtc: string | null;
  startedAtUtc: string | null;
  completedAtUtc: string | null;
  cancelledAtUtc: string | null;
  cancellationReason: string | null;
  notes: string | null;
  concurrencyToken: number;
  createdAtUtc: string;
  updatedAtUtc: string;
  steps: ProductionJobStep[];
}

export interface ProductionJobStep {
  id: string;
  stepNumber: number;
  workCenterId: string | null;
  workCenterName: string;
  operationName: string;
  operationType: RoutingOperationType;
  status: ProductionJobStepStatus;
  isOptional: boolean;
  inputQuantity: number;
  goodQuantity: number;
  scrappedQuantity: number;
  setupTimeMinutes: number;
  runTimeMinutesPerUnit: number;
  runTimeMinutesPerSqm: number | null;
  scrapPercentage: number;
  actualSetupMinutes: number | null;
  actualRunMinutes: number | null;
  assignedOperatorId: string | null;
  startedAtUtc: string | null;
  finishedAtUtc: string | null;
  reworkCount: number;
  instructions: string | null;
}

// Commands
export interface CreateProductionJobInput {
  productId: string;
  plannedQuantity: number;
  unitOfMeasure: string;
  warehouseId?: string;
  routingId?: string;
  plannedStartDateUtc?: string;
  dueDateUtc?: string;
  notes?: string;
}

export interface ReleaseProductionJobInput {
  warehouseId: string;
}

export interface StartJobStepInput {
  operatorId: string;
}

export interface FinishJobStepInput {
  goodQuantity: number;
  scrappedQuantity: number;
  scrapReasonCodeId?: string;
  actualSetupMinutes?: number;
  actualRunMinutes?: number;
  operatorId: string;
}

export type SkipJobStepInput = Record<string, never>;

export interface ReworkToStepInput {
  targetStepNumber: number;
  fromStepNumber: number;
  reason: string;
}

export interface CancelProductionJobInput {
  reason?: string;
}

export interface CompleteProductionJobInput {
  completedQuantity: number;
  warehouseId: string;
}
