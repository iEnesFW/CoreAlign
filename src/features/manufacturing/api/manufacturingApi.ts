import { apiClient } from '@/shared/api/apiClient';
import { cachedGet, invalidateHttpCache } from '@/shared/http/httpCache';
import type { ApiResponse } from '@/shared/types/api';
import type {
  AssignRoutingToProductInput,
  CreateRoutingInput,
  CreateWorkCenterInput,
  CreateWorkCenterOperatorInput,
  ProductionRouting,
  ProductionRoutingSummary,
  RoutingStatus,
  SetRoutingStepsInput,
  UpdateRoutingInput,
  UpdateWorkCenterInput,
  UpdateWorkCenterOperatorInput,
  WorkCenter,
  WorkCenterOperator,
} from '../model/manufacturing.types';
import type {
  CancelProductionJobInput,
  CompleteProductionJobInput,
  CreateProductionJobInput,
  FinishJobStepInput,
  ProductionJobDetail,
  ProductionJobListSummary,
  ProductionJobStatus,
  ReleaseProductionJobInput,
  ReworkToStepInput,
  StartJobStepInput,
} from '../model/productionJob.types';

const ROUTINGS = '/production-routings';
const WORK_CENTERS = '/work-centers';
const OPERATORS = '/work-center-operators';
const JOBS = '/production-jobs';

const ROUTING_INVALIDATION = [/\/production-routings/i] as const;
const WORK_CENTER_INVALIDATION = [/\/work-centers/i] as const;
const OPERATOR_INVALIDATION = [/\/work-center-operators/i] as const;
const JOB_INVALIDATION = [/\/production-jobs/i] as const;

export const routingsApi = {
  list: (status?: RoutingStatus) =>
    cachedGet<ApiResponse<ProductionRoutingSummary[]>>(apiClient, ROUTINGS, { params: { status } }),

  getById: (id: string) =>
    cachedGet<ApiResponse<ProductionRouting>>(apiClient, `${ROUTINGS}/${id}`),

  create: (input: CreateRoutingInput) =>
    apiClient.post<ApiResponse<ProductionRouting>>(ROUTINGS, input).then((r) => {
      invalidateHttpCache(ROUTING_INVALIDATION);
      return r.data;
    }),

  update: (input: UpdateRoutingInput) =>
    apiClient.put<ApiResponse<ProductionRouting>>(`${ROUTINGS}/${input.id}`, input).then((r) => {
      invalidateHttpCache(ROUTING_INVALIDATION);
      return r.data;
    }),

  setSteps: (input: SetRoutingStepsInput) =>
    apiClient
      .put<ApiResponse<ProductionRouting>>(`${ROUTINGS}/${input.routingId}/steps`, input)
      .then((r) => {
        invalidateHttpCache(ROUTING_INVALIDATION);
        return r.data;
      }),

  transition: (id: string, action: 'activate' | 'archive' | 'restore') =>
    apiClient
      .post<ApiResponse<ProductionRouting>>(`${ROUTINGS}/${id}/${action}`, null)
      .then((r) => {
        invalidateHttpCache(ROUTING_INVALIDATION);
        return r.data;
      }),

  remove: (id: string) =>
    apiClient.delete<ApiResponse<unknown>>(`${ROUTINGS}/${id}`).then((r) => {
      invalidateHttpCache(ROUTING_INVALIDATION);
      return r.data;
    }),

  assignToProduct: (input: AssignRoutingToProductInput) =>
    apiClient.post<ApiResponse<unknown>>(`${ROUTINGS}/assign-product`, input).then((r) => {
      invalidateHttpCache([/\/products/i]);
      return r.data;
    }),
};

export const workCentersApi = {
  list: (includeInactive = false) =>
    cachedGet<ApiResponse<WorkCenter[]>>(apiClient, WORK_CENTERS, { params: { includeInactive } }),

  create: (input: CreateWorkCenterInput) =>
    apiClient.post<ApiResponse<WorkCenter>>(WORK_CENTERS, input).then((r) => {
      invalidateHttpCache(WORK_CENTER_INVALIDATION);
      return r.data;
    }),

  update: (input: UpdateWorkCenterInput) =>
    apiClient.put<ApiResponse<WorkCenter>>(`${WORK_CENTERS}/${input.id}`, input).then((r) => {
      invalidateHttpCache(WORK_CENTER_INVALIDATION);
      return r.data;
    }),
};

export const operatorsApi = {
  list: (workCenterId?: string, employeeId?: string) =>
    cachedGet<ApiResponse<WorkCenterOperator[]>>(apiClient, OPERATORS, {
      params: { workCenterId, employeeId },
    }),

  create: (input: CreateWorkCenterOperatorInput) =>
    apiClient.post<ApiResponse<WorkCenterOperator>>(OPERATORS, input).then((r) => {
      invalidateHttpCache(OPERATOR_INVALIDATION);
      return r.data;
    }),

  update: (input: UpdateWorkCenterOperatorInput) =>
    apiClient.put<ApiResponse<WorkCenterOperator>>(`${OPERATORS}/${input.id}`, input).then((r) => {
      invalidateHttpCache(OPERATOR_INVALIDATION);
      return r.data;
    }),

  setActive: (id: string, active: boolean) =>
    apiClient
      .post<ApiResponse<unknown>>(`${OPERATORS}/${id}/${active ? 'activate' : 'deactivate'}`, null)
      .then((r) => {
        invalidateHttpCache(OPERATOR_INVALIDATION);
        return r.data;
      }),
};

export const productionJobsApi = {
  list: (status?: ProductionJobStatus, productId?: string) =>
    cachedGet<ApiResponse<ProductionJobListSummary[]>>(apiClient, JOBS, {
      params: { status, productId },
    }),

  getById: (id: string) => cachedGet<ApiResponse<ProductionJobDetail>>(apiClient, `${JOBS}/${id}`),

  create: (input: CreateProductionJobInput) =>
    apiClient.post<ApiResponse<ProductionJobDetail>>(JOBS, input).then((r) => {
      invalidateHttpCache(JOB_INVALIDATION);
      return r.data;
    }),

  release: (id: string, input: ReleaseProductionJobInput) =>
    apiClient.post<ApiResponse<ProductionJobDetail>>(`${JOBS}/${id}/release`, input).then((r) => {
      invalidateHttpCache(JOB_INVALIDATION);
      return r.data;
    }),

  startStep: (id: string, stepNumber: number, input: StartJobStepInput) =>
    apiClient
      .post<ApiResponse<ProductionJobDetail>>(`${JOBS}/${id}/steps/${stepNumber}/start`, input)
      .then((r) => {
        invalidateHttpCache(JOB_INVALIDATION);
        return r.data;
      }),

  finishStep: (id: string, stepNumber: number, input: FinishJobStepInput) =>
    apiClient
      .post<ApiResponse<ProductionJobDetail>>(`${JOBS}/${id}/steps/${stepNumber}/finish`, input)
      .then((r) => {
        invalidateHttpCache(JOB_INVALIDATION);
        return r.data;
      }),

  skipStep: (id: string, stepNumber: number) =>
    apiClient
      .post<ApiResponse<ProductionJobDetail>>(`${JOBS}/${id}/steps/${stepNumber}/skip`, {})
      .then((r) => {
        invalidateHttpCache(JOB_INVALIDATION);
        return r.data;
      }),

  rework: (id: string, input: ReworkToStepInput) =>
    apiClient.post<ApiResponse<ProductionJobDetail>>(`${JOBS}/${id}/rework`, input).then((r) => {
      invalidateHttpCache(JOB_INVALIDATION);
      return r.data;
    }),

  putOnHold: (id: string) =>
    apiClient.post<ApiResponse<ProductionJobDetail>>(`${JOBS}/${id}/hold`, null).then((r) => {
      invalidateHttpCache(JOB_INVALIDATION);
      return r.data;
    }),

  resume: (id: string) =>
    apiClient.post<ApiResponse<ProductionJobDetail>>(`${JOBS}/${id}/resume`, null).then((r) => {
      invalidateHttpCache(JOB_INVALIDATION);
      return r.data;
    }),

  cancel: (id: string, input: CancelProductionJobInput) =>
    apiClient.post<ApiResponse<ProductionJobDetail>>(`${JOBS}/${id}/cancel`, input).then((r) => {
      invalidateHttpCache(JOB_INVALIDATION);
      return r.data;
    }),

  complete: (id: string, input: CompleteProductionJobInput) =>
    apiClient.post<ApiResponse<ProductionJobDetail>>(`${JOBS}/${id}/complete`, input).then((r) => {
      invalidateHttpCache(JOB_INVALIDATION);
      return r.data;
    }),
};

export interface KioskStepDto {
  jobId: string;
  jobNumber: string;
  productName: string;
  stepNumber: number;
  operationName: string;
  inputQuantity: number;
  status: string;
  startedAtUtc: string | null;
  assignedOperatorId: string | null;
  setupTimeMinutes: number;
  runTimeMinutesPerUnit: number;
}

export const kioskApi = {
  verifyPin: (operatorId: string, pinCode: string) =>
    apiClient
      .post<ApiResponse<{ workCenterId: string; employeeId: string }>>(
        '/kiosk/manufacturing/verify-pin',
        {
          operatorId,
          pinCode,
        },
      )
      .then((r) => r.data),

  getActiveSteps: (workCenterId: string) =>
    apiClient
      .get<
        ApiResponse<KioskStepDto[]>
      >(`/kiosk/manufacturing/work-centers/${workCenterId}/active-steps`)
      .then((r) => r.data),
};

export const dashboardApi = {
  getKpis: (startDate: string, endDate: string) =>
    cachedGet<ApiResponse<unknown>>(apiClient, '/manufacturing-dashboard/kpis', {
      params: { startDateUtc: startDate, endDateUtc: endDate },
    }),
};
