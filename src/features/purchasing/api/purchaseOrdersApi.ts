import { apiClient } from '@/shared/api/apiClient';
import { cachedGet, invalidateHttpCache } from '@/shared/http/httpCache';
import type { ApiResponse, PagedResult } from '@/shared/types/api';
import type {
  CreatePurchaseOrderInput,
  PurchaseOrder,
  PurchaseOrderListParams,
  ReceivePurchaseOrderInput,
  UpdatePurchaseOrderInput,
} from '../model/purchaseOrder.types';

const BASE = '/purchase-orders';
const INVALIDATION = [/\/purchase-orders/i, /\/stock/i, /\/products/i] as const;

const mutate = <T>(p: Promise<{ data: ApiResponse<T> }>) =>
  p.then((r) => {
    invalidateHttpCache(INVALIDATION);
    return r.data;
  });

export const purchaseOrdersApi = {
  search: (params: PurchaseOrderListParams) =>
    cachedGet<ApiResponse<PagedResult<PurchaseOrder>>>(apiClient, BASE, { params }),

  getById: (id: string) => cachedGet<ApiResponse<PurchaseOrder>>(apiClient, `${BASE}/${id}`),

  create: (input: CreatePurchaseOrderInput) =>
    mutate(apiClient.post<ApiResponse<PurchaseOrder>>(BASE, input)),

  update: (input: UpdatePurchaseOrderInput) =>
    mutate(apiClient.put<ApiResponse<PurchaseOrder>>(`${BASE}/${input.id}`, input)),

  remove: (id: string) => mutate(apiClient.delete<ApiResponse<boolean>>(`${BASE}/${id}`)),

  submit: (id: string) =>
    mutate(apiClient.post<ApiResponse<PurchaseOrder>>(`${BASE}/${id}/submit`)),

  approve: (id: string) =>
    mutate(apiClient.post<ApiResponse<PurchaseOrder>>(`${BASE}/${id}/approve`)),

  cancel: (id: string, reason?: string | null) =>
    mutate(
      apiClient.post<ApiResponse<PurchaseOrder>>(`${BASE}/${id}/cancel`, {
        id,
        reason: reason ?? null,
      }),
    ),

  close: (id: string) => mutate(apiClient.post<ApiResponse<PurchaseOrder>>(`${BASE}/${id}/close`)),

  receive: (input: ReceivePurchaseOrderInput) =>
    mutate(apiClient.post<ApiResponse<PurchaseOrder>>(`${BASE}/${input.id}/receive`, input)),
};
