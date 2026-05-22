import { apiClient } from '@/shared/api/apiClient';
import { cachedGet } from '@/shared/http/httpCache';
import type { ApiResponse, PagedResult } from '@/shared/types/api';
import type {
  GenerateInvoiceRequest,
  Invoice,
  InvoiceListParams,
  InvoiceSummary,
} from '../model/invoice.types';

const BASE = '/invoices';

export const invoicesApi = {
  list: (params: InvoiceListParams) =>
    apiClient.get<ApiResponse<PagedResult<InvoiceSummary>>>(BASE, { params }).then((r) => r.data),

  getById: (id: string) => apiClient.get<ApiResponse<Invoice>>(`${BASE}/${id}`).then((r) => r.data),

  getByOrder: (orderId: string) =>
    apiClient.get<ApiResponse<InvoiceSummary[]>>(`/orders/${orderId}/invoices`).then((r) => r.data),

  generateFromOrder: (orderId: string, request?: GenerateInvoiceRequest) =>
    apiClient
      .post<ApiResponse<Invoice>>(`${BASE}/from-order/${orderId}`, request ?? {})
      .then((r) => r.data),

  markPaid: (id: string) =>
    apiClient.post<ApiResponse<Invoice>>(`${BASE}/${id}/mark-paid`).then((r) => r.data),

  cancel: (id: string) =>
    apiClient.post<ApiResponse<boolean>>(`${BASE}/${id}/cancel`).then((r) => r.data),

  getCreditNotes: (id: string) =>
    cachedGet<ApiResponse<InvoiceSummary[]>>(apiClient, `${BASE}/${id}/credit-notes`),
};
