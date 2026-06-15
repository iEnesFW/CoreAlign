import { apiClient } from '@/shared/api/apiClient';
import type { ApiResponse, PagedResult } from '@/shared/types/api';
import type {
  CreateReturnRequestPayload,
  ReceiveReturnPayload,
  ReturnRequest,
  ReturnRequestListParams,
  ReturnRequestSummary,
} from '../model/return.types';

const BASE = '/returns';

export const returnsApi = {
  list: (params: ReturnRequestListParams) =>
    apiClient
      .get<ApiResponse<PagedResult<ReturnRequestSummary>>>(BASE, { params })
      .then((r) => r.data),

  getById: (id: string) =>
    apiClient.get<ApiResponse<ReturnRequest>>(`${BASE}/${id}`).then((r) => r.data),

  listByOrder: (orderId: string) =>
    apiClient
      .get<ApiResponse<ReturnRequestSummary[]>>(`${BASE}/by-order/${orderId}`)
      .then((r) => r.data),

  create: (payload: CreateReturnRequestPayload) =>
    apiClient.post<ApiResponse<ReturnRequest>>(BASE, payload).then((r) => r.data),

  approve: (id: string) =>
    apiClient.post<ApiResponse<ReturnRequest>>(`${BASE}/${id}/approve`).then((r) => r.data),

  reject: (id: string, reason?: string | null) =>
    apiClient
      .post<ApiResponse<ReturnRequest>>(`${BASE}/${id}/reject`, { reason: reason ?? null })
      .then((r) => r.data),

  cancel: (id: string) =>
    apiClient.post<ApiResponse<ReturnRequest>>(`${BASE}/${id}/cancel`).then((r) => r.data),

  receive: (id: string, payload: ReceiveReturnPayload) =>
    apiClient
      .post<ApiResponse<ReturnRequest>>(`${BASE}/${id}/receive`, {
        warehouseId: payload.warehouseId,
        autoIssueCreditNote: payload.autoIssueCreditNote ?? true,
      })
      .then((r) => r.data),
};
