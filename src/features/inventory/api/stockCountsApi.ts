import { apiClient } from '@/shared/api/apiClient';
import { cachedGet, invalidateHttpCache } from '@/shared/http/httpCache';
import type { ApiResponse, PagedResult } from '@/shared/types/api';
import type {
  PlanStockCountInput,
  RecordCountInput,
  ReconcileStockCountInput,
  StockCount,
  StockCountListParams,
} from '../model/stockCount.types';

const BASE = '/stock/stock-counts';
const INVALIDATION = [/\/stock\//i, /\/products\//i] as const;

const mutate = <T>(p: Promise<{ data: ApiResponse<T> }>) =>
  p.then((r) => {
    invalidateHttpCache(INVALIDATION);
    return r.data;
  });

export const stockCountsApi = {
  list: (params: StockCountListParams) =>
    cachedGet<ApiResponse<PagedResult<StockCount>>>(apiClient, BASE, { params }),

  getById: (id: string) => cachedGet<ApiResponse<StockCount>>(apiClient, `${BASE}/${id}`),

  plan: (input: PlanStockCountInput) =>
    mutate(apiClient.post<ApiResponse<StockCount>>(`${BASE}/plan`, input)),

  start: (id: string) => mutate(apiClient.post<ApiResponse<StockCount>>(`${BASE}/${id}/start`)),

  record: (input: RecordCountInput) =>
    mutate(apiClient.post<ApiResponse<StockCount>>(`${BASE}/${input.id}/record`, input)),

  reconcile: (input: ReconcileStockCountInput) =>
    mutate(apiClient.post<ApiResponse<StockCount>>(`${BASE}/${input.id}/reconcile`, input)),

  post: (id: string) => mutate(apiClient.post<ApiResponse<StockCount>>(`${BASE}/${id}/post`)),

  cancel: (id: string) => mutate(apiClient.post<ApiResponse<StockCount>>(`${BASE}/${id}/cancel`)),
};
