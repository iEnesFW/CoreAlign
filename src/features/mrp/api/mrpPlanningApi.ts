import { apiClient } from '@/shared/api/apiClient';
import { cachedGet, invalidateHttpCache } from '@/shared/http/httpCache';
import type { ApiResponse, PagedResult } from '@/shared/types/api';
import type {
  ChangeImpactParams,
  ChangeImpactResult,
  ClassifyAbcResult,
  CommitMrpPlanInput,
  CompletePlannedProductionOrderResult,
  CompleteProductionOrderInput,
  FirmPlannedOrderInput,
  FirmProductionOrderInput,
  MrpActionMessage,
  MrpActionMessageParams,
  MrpCapacityLoadParams,
  MrpCapacityLoadResult,
  MrpItemPlan,
  MrpItemPlanParams,
  MrpPegging,
  MrpPlanResult,
  MrpPlanRun,
  MrpPlannedOrder,
  MrpPreviewParams,
  MrpTransferSuggestionsResult,
  PlannedProductionOrder,
  ReleasePlannedOrdersInput,
  ReleaseProductionOrderInput,
  ReleaseResult,
} from '../model/mrp-planning.types';

const PLAN_BASE = '/mrp/plan';
const CAPACITY_BASE = '/mrp/capacity';
const PRODUCT_BASE = '/mrp/products';
const ACTION_BASE = '/mrp/action-messages';
const PEGGING_BASE = '/mrp/pegging';
const PLANNED_ORDER_BASE = '/mrp/planned-orders';
const PRODUCTION_ORDER_BASE = '/mrp/production-orders';
const CHANGE_IMPACT_BASE = '/mrp/change-impact';
const DISTRIBUTION_BASE = '/mrp/distribution';

const INVALIDATION = [
  /\/mrp\/plan/i,
  /\/mrp\/action-messages/i,
  /\/mrp\/planned-orders/i,
  /\/mrp\/production-orders/i,
  /\/mrp\/pegging/i,
  /\/purchase-requisitions/i,
] as const;

const mutate = <T>(p: Promise<{ data: ApiResponse<T> }>): Promise<ApiResponse<T>> =>
  p.then((r) => {
    invalidateHttpCache(INVALIDATION);
    return r.data;
  });

const previewQuery = (params: MrpPreviewParams) => ({
  asOf: params.asOfDateUtc ?? undefined,
  bucket: params.bucketKind ?? 'Day',
  horizon: params.horizonDays ?? 60,
});

export const mrpPlanningApi = {
  preview: (params: MrpPreviewParams = {}) =>
    cachedGet<ApiResponse<MrpPlanResult>>(apiClient, `${PLAN_BASE}/preview`, {
      params: previewQuery(params),
    }),

  capacityLoad: (params: MrpCapacityLoadParams = {}) =>
    cachedGet<ApiResponse<MrpCapacityLoadResult>>(apiClient, `${CAPACITY_BASE}/load`, {
      params: {
        bucket: params.bucketKind ?? 'Day',
        horizon: params.horizonDays ?? 60,
      },
    }),

  itemPlan: ({ productId, ...params }: MrpItemPlanParams) =>
    cachedGet<ApiResponse<MrpItemPlan>>(apiClient, `${PLAN_BASE}/item/${productId}`, {
      params: previewQuery(params),
    }),

  listPlanRuns: (page = 1, pageSize = 25) =>
    cachedGet<ApiResponse<PagedResult<MrpPlanRun>>>(apiClient, `${PLAN_BASE}/runs`, {
      params: { page, pageSize },
    }),

  listActionMessages: (params: MrpActionMessageParams) =>
    cachedGet<ApiResponse<PagedResult<MrpActionMessage>>>(apiClient, ACTION_BASE, {
      params: {
        planRunId: params.planRunId ?? undefined,
        type: params.type ?? undefined,
        severity: params.severity ?? undefined,
        supplierId: params.supplierId ?? undefined,
        includeDismissed: params.includeDismissed ?? false,
        page: params.page ?? 1,
        pageSize: params.pageSize ?? 25,
      },
    }),

  pegging: (planRunId: string, componentProductId: string) =>
    cachedGet<ApiResponse<MrpPegging[]>>(
      apiClient,
      `${PEGGING_BASE}/${planRunId}/${componentProductId}`,
    ),

  changeImpact: ({ planRunId, sourceOrderLineId }: ChangeImpactParams) =>
    cachedGet<ApiResponse<ChangeImpactResult>>(
      apiClient,
      `${CHANGE_IMPACT_BASE}/${planRunId}/${sourceOrderLineId}`,
    ),

  transferSuggestions: () =>
    cachedGet<ApiResponse<MrpTransferSuggestionsResult>>(
      apiClient,
      `${DISTRIBUTION_BASE}/transfer-suggestions`,
    ),

  commit: (input: CommitMrpPlanInput) =>
    mutate(
      apiClient.post<ApiResponse<MrpPlanRun>>(`${PLAN_BASE}/commit`, {
        asOfDateUtc: input.asOfDateUtc ?? null,
        bucketKind: input.bucketKind ?? 'Day',
        horizonDays: input.horizonDays ?? 60,
        operationId: input.operationId,
      }),
    ),

  release: (input: ReleasePlannedOrdersInput) =>
    mutate(
      apiClient.post<ApiResponse<ReleaseResult>>(`${PLAN_BASE}/${input.planRunId}/release`, {
        planRunId: input.planRunId,
        plannedOrderIds: input.plannedOrderIds,
        operationId: input.operationId,
      }),
    ),

  firmPlannedOrder: (input: FirmPlannedOrderInput) =>
    mutate(
      apiClient.post<ApiResponse<MrpPlannedOrder>>(
        `${PLANNED_ORDER_BASE}/${input.plannedOrderId}/firm`,
        {
          overrideQuantity: input.overrideQuantity ?? null,
          overrideDueDateUtc: input.overrideDueDateUtc ?? null,
          operationId: input.operationId,
        },
      ),
    ),

  firmProductionOrder: (input: FirmProductionOrderInput) =>
    mutate(
      apiClient.post<ApiResponse<PlannedProductionOrder>>(
        `${PRODUCTION_ORDER_BASE}/${input.productionOrderId}/firm`,
        { operationId: input.operationId },
      ),
    ),

  releaseProductionOrder: (input: ReleaseProductionOrderInput) =>
    mutate(
      apiClient.post<ApiResponse<PlannedProductionOrder>>(
        `${PRODUCTION_ORDER_BASE}/${input.productionOrderId}/release`,
        { operationId: input.operationId },
      ),
    ),

  completeProductionOrder: (input: CompleteProductionOrderInput) =>
    mutate(
      apiClient.post<ApiResponse<CompletePlannedProductionOrderResult>>(
        `${PRODUCTION_ORDER_BASE}/${input.productionOrderId}/complete`,
        { operationId: input.operationId, warehouseId: input.warehouseId ?? null },
      ),
    ),

  dismissActionMessage: (id: string) =>
    mutate(apiClient.post<ApiResponse<void>>(`${ACTION_BASE}/${id}/dismiss`)),

  classifyAbc: () =>
    mutate(apiClient.post<ApiResponse<ClassifyAbcResult>>(`${PRODUCT_BASE}/classify-abc`, {})),
};
