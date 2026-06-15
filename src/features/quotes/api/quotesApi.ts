import { apiClient } from '@/shared/api/apiClient';
import type { ApiResponse, PagedResult } from '@/shared/types/api';
import type {
  CreateQuotePayload,
  Quote,
  QuoteListParams,
  QuoteSummary,
} from '../model/quote.types';

const BASE = '/quotes';

export const quotesApi = {
  list: (params: QuoteListParams) =>
    apiClient.get<ApiResponse<PagedResult<QuoteSummary>>>(BASE, { params }).then((r) => r.data),

  getById: (id: string) => apiClient.get<ApiResponse<Quote>>(`${BASE}/${id}`).then((r) => r.data),

  create: (payload: CreateQuotePayload) =>
    apiClient.post<ApiResponse<Quote>>(BASE, payload).then((r) => r.data),

  remove: (id: string) =>
    apiClient.delete<ApiResponse<boolean>>(`${BASE}/${id}`).then((r) => r.data),

  send: (id: string) =>
    apiClient.post<ApiResponse<Quote>>(`${BASE}/${id}/send`).then((r) => r.data),

  accept: (id: string) =>
    apiClient.post<ApiResponse<Quote>>(`${BASE}/${id}/accept`).then((r) => r.data),

  reject: (id: string, reason?: string | null) =>
    apiClient
      .post<ApiResponse<Quote>>(`${BASE}/${id}/reject`, { reason: reason ?? null })
      .then((r) => r.data),

  convertToOrder: (id: string) =>
    apiClient
      .post<ApiResponse<{ id: string }>>(`${BASE}/${id}/convert-to-order`)
      .then((r) => r.data),

  downloadPdf: (id: string) =>
    apiClient.get<Blob>(`${BASE}/${id}/pdf`, { responseType: 'blob' }).then((r) => r.data),
};
