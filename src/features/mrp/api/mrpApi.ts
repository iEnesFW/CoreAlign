import { apiClient } from '@/shared/api/apiClient';
import { cachedGet, invalidateHttpCache } from '@/shared/http/httpCache';
import type { ApiResponse, PagedResult } from '@/shared/types/api';
import type {
  ConvertRequisitionInput,
  CreatePurchaseRequisitionInput,
  DemandForecast,
  MrpDashboard,
  MrpSuggestionResult,
  PurchaseRequisition,
  RequisitionListParams,
  StockProjection,
} from '../model/mrp.types';

const MRP_BASE = '/mrp';
const REQ_BASE = '/purchase-requisitions';
const INVALIDATION = [/\/mrp/i, /\/purchase-requisitions/i, /\/purchase-orders/i] as const;

const mutate = <T>(p: Promise<{ data: ApiResponse<T> }>): Promise<ApiResponse<T>> =>
  p.then((r) => {
    invalidateHttpCache(INVALIDATION);
    return r.data;
  });

export const mrpApi = {
  dashboard: (topN = 20) =>
    cachedGet<ApiResponse<MrpDashboard>>(apiClient, `${MRP_BASE}/dashboard`, {
      params: { topN },
    }),

  stockProjection: (productId: string, daysAhead = 30) =>
    cachedGet<ApiResponse<StockProjection>>(
      apiClient,
      `${MRP_BASE}/stock-projection/${productId}`,
      {
        params: { daysAhead },
      },
    ),

  demandForecast: (productId: string, windowDays = 90) =>
    cachedGet<ApiResponse<DemandForecast>>(apiClient, `${MRP_BASE}/demand-forecast/${productId}`, {
      params: { windowDays },
    }),

  generateSuggestions: (asOfDateUtc?: string | null) =>
    mutate(
      apiClient.post<ApiResponse<MrpSuggestionResult>>(`${MRP_BASE}/generate-suggestions`, {
        asOfDateUtc: asOfDateUtc ?? null,
      }),
    ),

  listRequisitions: (params: RequisitionListParams) =>
    cachedGet<ApiResponse<PagedResult<PurchaseRequisition>>>(apiClient, REQ_BASE, { params }),

  createRequisition: (input: CreatePurchaseRequisitionInput) =>
    mutate(apiClient.post<ApiResponse<PurchaseRequisition>>(REQ_BASE, input)),

  submitRequisition: (id: string) =>
    mutate(apiClient.post<ApiResponse<PurchaseRequisition>>(`${REQ_BASE}/${id}/submit`)),

  approveRequisition: (id: string) =>
    mutate(apiClient.post<ApiResponse<PurchaseRequisition>>(`${REQ_BASE}/${id}/approve`)),

  rejectRequisition: (id: string, reason?: string | null) =>
    mutate(
      apiClient.post<ApiResponse<PurchaseRequisition>>(`${REQ_BASE}/${id}/reject`, {
        id,
        reason: reason ?? null,
      }),
    ),

  cancelRequisition: (id: string, reason?: string | null) =>
    mutate(
      apiClient.post<ApiResponse<PurchaseRequisition>>(`${REQ_BASE}/${id}/cancel`, {
        id,
        reason: reason ?? null,
      }),
    ),

  convertRequisition: (input: ConvertRequisitionInput) =>
    mutate(apiClient.post<ApiResponse<string>>(`${REQ_BASE}/${input.id}/convert`, input)),
};
