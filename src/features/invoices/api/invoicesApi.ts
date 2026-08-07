import { apiClient } from '@/shared/api/apiClient';
import { cachedGet } from '@/shared/http/httpCache';
import type { ApiResponse, PagedResult } from '@/shared/types/api';
import type {
  CreateStandaloneInvoiceInput,
  CreditedLineQuantity,
  GenerateInvoiceRequest,
  Invoice,
  InvoiceAggregates,
  InvoiceListParams,
  InvoiceSummary,
  IssueCreditNotePayload,
} from '../model/invoice.types';

const BASE = '/invoices';

export const invoicesApi = {
  list: (params: InvoiceListParams) =>
    apiClient.get<ApiResponse<PagedResult<InvoiceSummary>>>(BASE, { params }).then((r) => r.data),

  aggregates: (search?: string, customerId?: string, fiscalYear?: number) =>
    apiClient
      .get<ApiResponse<InvoiceAggregates>>(`${BASE}/aggregates`, {
        params: { search, customerId, fiscalYear },
      })
      .then((r) => r.data),

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

  writeOff: (id: string, reason?: string | null) =>
    apiClient
      .post<ApiResponse<Invoice>>(`${BASE}/${id}/write-off`, { reason: reason ?? null })
      .then((r) => r.data),

  getCreditNotes: (id: string) =>
    cachedGet<ApiResponse<InvoiceSummary[]>>(apiClient, `${BASE}/${id}/credit-notes`),

  getCreditedByLine: (id: string) =>
    apiClient
      .get<ApiResponse<CreditedLineQuantity[]>>(`${BASE}/${id}/credited-by-line`)
      .then((r) => r.data),

  createStandalone: (input: CreateStandaloneInvoiceInput) =>
    apiClient.post<ApiResponse<Invoice>>(`${BASE}/standalone`, input).then((r) => r.data),

  issueCreditNote: (id: string, payload: IssueCreditNotePayload) =>
    apiClient.post<ApiResponse<Invoice>>(`${BASE}/${id}/credit-notes`, payload).then((r) => r.data),
};
