import { apiClient } from '@/shared/api/apiClient';
import type { ApiResponse, PagedResult } from '@/shared/types/api';
import type {
  CreateRecurringInvoiceInput,
  RecurringInvoiceListParams,
  RecurringInvoiceTemplate,
  RecurringInvoiceTemplateSummary,
  UpdateRecurringInvoiceInput,
} from '../model/recurringInvoice.types';

const BASE = '/recurring-invoices';

interface RunNowResult {
  invoiceId: string | null;
}

export const recurringInvoicesApi = {
  list: (params: RecurringInvoiceListParams) =>
    apiClient
      .get<ApiResponse<PagedResult<RecurringInvoiceTemplateSummary>>>(BASE, { params })
      .then((r) => r.data),

  getById: (id: string) =>
    apiClient.get<ApiResponse<RecurringInvoiceTemplate>>(`${BASE}/${id}`).then((r) => r.data),

  create: (input: CreateRecurringInvoiceInput) =>
    apiClient.post<ApiResponse<RecurringInvoiceTemplate>>(BASE, input).then((r) => r.data),

  update: (input: UpdateRecurringInvoiceInput) =>
    apiClient
      .put<ApiResponse<RecurringInvoiceTemplate>>(`${BASE}/${input.id}`, input)
      .then((r) => r.data),

  pause: (id: string) =>
    apiClient
      .post<ApiResponse<RecurringInvoiceTemplate>>(`${BASE}/${id}/pause`)
      .then((r) => r.data),

  resume: (id: string) =>
    apiClient
      .post<ApiResponse<RecurringInvoiceTemplate>>(`${BASE}/${id}/resume`)
      .then((r) => r.data),

  cancel: (id: string) =>
    apiClient
      .post<ApiResponse<RecurringInvoiceTemplate>>(`${BASE}/${id}/cancel`)
      .then((r) => r.data),

  runNow: (id: string) =>
    apiClient.post<ApiResponse<RunNowResult>>(`${BASE}/${id}/run-now`).then((r) => r.data),
};
