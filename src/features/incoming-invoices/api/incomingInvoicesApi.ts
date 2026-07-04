import { apiClient } from '@/shared/api/apiClient';
import type { ApiResponse, PagedResult } from '@/shared/types/api';
import type {
  IgnoreIncomingInvoiceInput,
  IncomingInvoiceDto,
  IncomingInvoiceListParams,
  ProcessIncomingInvoiceInput,
  ProcessIncomingInvoiceResult,
} from '../model/incomingInvoice.types';

const BASE = '/incoming-invoices';

export const incomingInvoicesApi = {
  list: (params: IncomingInvoiceListParams) =>
    apiClient
      .get<ApiResponse<PagedResult<IncomingInvoiceDto>>>(BASE, { params })
      .then((r) => r.data),

  getById: (id: string) =>
    apiClient.get<ApiResponse<IncomingInvoiceDto>>(`${BASE}/${id}`).then((r) => r.data),

  process: (id: string, input: ProcessIncomingInvoiceInput) =>
    apiClient
      .post<ApiResponse<ProcessIncomingInvoiceResult>>(`${BASE}/${id}/process`, input)
      .then((r) => r.data),

  ignore: (id: string, input: IgnoreIncomingInvoiceInput) =>
    apiClient
      .post<ApiResponse<IncomingInvoiceDto>>(`${BASE}/${id}/ignore`, input)
      .then((r) => r.data),
};
