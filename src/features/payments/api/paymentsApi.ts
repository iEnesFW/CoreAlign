import { apiClient } from '@/shared/api/apiClient';
import { cachedGet, invalidateHttpCache } from '@/shared/http/httpCache';
import type { ApiResponse, PagedResult } from '@/shared/types/api';
import type { InvoiceSummary } from '@/features/invoices/model/invoice.types';
import type {
  ApplyPaymentInput,
  CreatePaymentInput,
  CustomerAging,
  CustomerLedgerEntry,
  Payment,
  PaymentApplicationItem,
  PaymentSummary,
} from '../model/payment.types';

const BASE = '/payments';
const INVALIDATION = [/\/payments/i, /\/invoices/i, /\/customers\//i] as const;

export interface PaymentsSearchParams {
  search?: string;
  customerId?: string;
  page?: number;
  pageSize?: number;
}

export const paymentsApi = {
  search: (params: PaymentsSearchParams) =>
    apiClient.get<ApiResponse<PagedResult<PaymentSummary>>>(BASE, { params }).then((r) => r.data),

  getById: (id: string) => apiClient.get<ApiResponse<Payment>>(`${BASE}/${id}`).then((r) => r.data),

  getByCustomer: (customerId: string) =>
    cachedGet<ApiResponse<PaymentSummary[]>>(apiClient, `${BASE}/by-customer/${customerId}`),

  getByInvoice: (invoiceId: string) =>
    cachedGet<ApiResponse<PaymentApplicationItem[]>>(apiClient, `${BASE}/by-invoice/${invoiceId}`),

  create: (input: CreatePaymentInput) =>
    apiClient.post<ApiResponse<Payment>>(BASE, input).then((r) => {
      invalidateHttpCache(INVALIDATION);
      return r.data;
    }),

  confirm: (id: string) =>
    apiClient.post<ApiResponse<Payment>>(`${BASE}/${id}/confirm`, { id }).then((r) => {
      invalidateHttpCache(INVALIDATION);
      return r.data;
    }),

  apply: (input: ApplyPaymentInput) =>
    apiClient.post<ApiResponse<Payment>>(`${BASE}/${input.id}/apply`, input).then((r) => {
      invalidateHttpCache(INVALIDATION);
      return r.data;
    }),

  unapply: (id: string, applicationId: string) =>
    apiClient.post<ApiResponse<Payment>>(`${BASE}/${id}/unapply/${applicationId}`).then((r) => {
      invalidateHttpCache(INVALIDATION);
      return r.data;
    }),

  voidPayment: (id: string, reason?: string | null) =>
    apiClient
      .post<ApiResponse<Payment>>(`${BASE}/${id}/void`, { id, reason: reason ?? null })
      .then((r) => {
        invalidateHttpCache(INVALIDATION);
        return r.data;
      }),

  getLedger: (customerId: string, fromUtc?: string, toUtc?: string, page = 1, pageSize = 50) =>
    cachedGet<ApiResponse<PagedResult<CustomerLedgerEntry>>>(
      apiClient,
      `/customers/${customerId}/ledger`,
      { params: { fromUtc, toUtc, page, pageSize } },
    ),

  getAging: (customerId: string, asOfUtc?: string) =>
    cachedGet<ApiResponse<CustomerAging>>(apiClient, `/customers/${customerId}/aging`, {
      params: asOfUtc ? { asOfUtc } : {},
    }),

  getOpenInvoices: (customerId: string) =>
    cachedGet<ApiResponse<InvoiceSummary[]>>(apiClient, `/customers/${customerId}/open-invoices`),
};
