import { apiClient } from '@/shared/api/apiClient';
import { cachedGet, invalidateHttpCache } from '@/shared/http/httpCache';
import type { ApiResponse, PagedResult } from '@/shared/types/api';
import type {
  CreateMyServiceTicketInput,
  InitiatePaymentInput,
  InitiatePaymentResult,
  MyGlassProjectSummary,
  MyInvoice,
  MyInvoiceSummary,
  MyPayment,
  MyPaymentSummary,
  MyProjectInstallationStatus,
  MyServiceTicket,
  MyWarrantyContract,
} from '../model/customerPortal.types';

const PORTAL_BASE = '/customer-portal';
const PORTAL_INVALIDATION = [/\/customer-portal/i] as const;

export interface MyInvoicesListParams {
  page?: number;
  pageSize?: number;
  search?: string;
}

export interface MyProjectsListParams {
  page?: number;
  pageSize?: number;
  search?: string;
}

export const customerPortalApi = {
  listMyWarranties: () =>
    cachedGet<ApiResponse<MyWarrantyContract[]>>(apiClient, `${PORTAL_BASE}/warranty-contracts`),

  getMyWarranty: (id: string) =>
    cachedGet<ApiResponse<MyWarrantyContract>>(
      apiClient,
      `${PORTAL_BASE}/warranty-contracts/${id}`,
    ),

  listMyServiceTickets: () =>
    cachedGet<ApiResponse<MyServiceTicket[]>>(apiClient, `${PORTAL_BASE}/service-tickets`),

  getMyServiceTicket: (id: string) =>
    cachedGet<ApiResponse<MyServiceTicket>>(apiClient, `${PORTAL_BASE}/service-tickets/${id}`),

  createMyServiceTicket: (input: CreateMyServiceTicketInput) =>
    apiClient
      .post<ApiResponse<MyServiceTicket>>(`${PORTAL_BASE}/service-tickets`, input)
      .then((r) => {
        invalidateHttpCache(PORTAL_INVALIDATION);
        return r.data;
      }),

  listMyInvoices: (params: MyInvoicesListParams) =>
    apiClient
      .get<ApiResponse<PagedResult<MyInvoiceSummary>>>(`${PORTAL_BASE}/invoices`, { params })
      .then((r) => r.data),

  getMyInvoice: (id: string) =>
    cachedGet<ApiResponse<MyInvoice>>(apiClient, `${PORTAL_BASE}/invoices/${id}`),

  downloadInvoicePdf: (id: string) =>
    apiClient.get<Blob>(`${PORTAL_BASE}/invoices/${id}/download-pdf`, { responseType: 'blob' }),

  listMyPayments: () =>
    cachedGet<ApiResponse<MyPaymentSummary[]>>(apiClient, `${PORTAL_BASE}/payments`),

  getMyPayment: (id: string) =>
    cachedGet<ApiResponse<MyPayment>>(apiClient, `${PORTAL_BASE}/payments/${id}`),

  initiatePayment: (input: InitiatePaymentInput) =>
    apiClient
      .post<ApiResponse<InitiatePaymentResult>>(`${PORTAL_BASE}/payments/initiate`, input)
      .then((r) => {
        invalidateHttpCache(PORTAL_INVALIDATION);
        return r.data;
      }),

  listMyProjects: (params: MyProjectsListParams) =>
    apiClient
      .get<ApiResponse<PagedResult<MyGlassProjectSummary>>>(`${PORTAL_BASE}/glass-projects`, {
        params,
      })
      .then((r) => r.data),

  getProjectInstallationStatus: (id: string) =>
    cachedGet<ApiResponse<MyProjectInstallationStatus>>(
      apiClient,
      `${PORTAL_BASE}/glass-projects/${id}/installation-status`,
    ),
};
