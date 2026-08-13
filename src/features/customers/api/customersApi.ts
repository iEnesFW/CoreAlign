import { apiClient } from '@/shared/api/apiClient';
import { cachedGet, invalidateHttpCache } from '@/shared/http/httpCache';
import type { ApiResponse, PagedResult } from '@/shared/types/api';
import type { Tag } from '@/shared/model/tag.types';
import type {
  Customer,
  CreateCustomerInput,
  CustomerAddress,
  CustomerAddressInput,
  CustomerAnalytics,
  CustomerContact,
  CustomerContactInput,
  CustomerDuplicateCheckParams,
  CustomerDuplicateMatch,
  CustomerListParams,
  CustomerNote,
  CustomerOverview,
  CustomerSummary,
  CustomerTransaction,
  UpdateCustomerAddressInput,
  UpdateCustomerContactInput,
  UpdateCustomerInput,
} from '../model/customer.types';

const BASE = '/customers';

const CUSTOMERS_INVALIDATION = [/\/customers/i] as const;

export const customersApi = {
  list: (params: CustomerListParams) =>
    cachedGet<ApiResponse<PagedResult<Customer>>>(apiClient, BASE, { params }),

  exportList: (params: {
    format: 'Xlsx' | 'Csv';
    search?: string | null;
    isActive?: boolean | null;
  }) =>
    apiClient
      .get<Blob>(`${BASE}/export`, {
        params: {
          format: params.format,
          search: params.search ?? undefined,
          isActive: params.isActive ?? undefined,
        },
        responseType: 'blob',
      })
      .then((r) => r.data),

  duplicateCheck: (params: CustomerDuplicateCheckParams) =>
    apiClient
      .get<ApiResponse<CustomerDuplicateMatch[]>>(`${BASE}/duplicate-check`, { params })
      .then((r) => r.data),

  getById: (id: string) => cachedGet<ApiResponse<Customer>>(apiClient, `${BASE}/${id}`),

  getSummary: (id: string) =>
    cachedGet<ApiResponse<CustomerSummary>>(apiClient, `${BASE}/${id}/summary`),

  getOverview: (id: string) =>
    cachedGet<ApiResponse<CustomerOverview>>(apiClient, `${BASE}/${id}/overview`),

  getAnalytics: (id: string, monthsBack = 12) =>
    cachedGet<ApiResponse<CustomerAnalytics>>(apiClient, `${BASE}/${id}/analytics`, {
      params: { monthsBack },
    }),

  getTransactions: (id: string, page = 1, pageSize = 50) =>
    apiClient
      .get<ApiResponse<PagedResult<CustomerTransaction>>>(`${BASE}/${id}/transactions`, {
        params: { page, pageSize },
      })
      .then((r) => r.data),

  getNotes: (id: string) =>
    apiClient.get<ApiResponse<CustomerNote[]>>(`${BASE}/${id}/notes`).then((r) => r.data),

  addNote: (id: string, body: string) =>
    apiClient.post<ApiResponse<CustomerNote>>(`${BASE}/${id}/notes`, { body }).then((r) => {
      invalidateHttpCache(CUSTOMERS_INVALIDATION);
      return r.data;
    }),

  create: (input: CreateCustomerInput) =>
    apiClient.post<ApiResponse<Customer>>(BASE, input).then((r) => {
      invalidateHttpCache(CUSTOMERS_INVALIDATION);
      return r.data;
    }),

  update: (input: UpdateCustomerInput) =>
    apiClient.put<ApiResponse<Customer>>(`${BASE}/${input.id}`, input).then((r) => {
      invalidateHttpCache(CUSTOMERS_INVALIDATION);
      return r.data;
    }),

  remove: (id: string) =>
    apiClient.delete<ApiResponse<boolean>>(`${BASE}/${id}`).then((r) => {
      invalidateHttpCache(CUSTOMERS_INVALIDATION);
      return r.data;
    }),

  getAddresses: (id: string) =>
    cachedGet<ApiResponse<CustomerAddress[]>>(apiClient, `${BASE}/${id}/addresses`),

  createAddress: (input: CustomerAddressInput) =>
    apiClient
      .post<ApiResponse<CustomerAddress>>(`${BASE}/${input.customerId}/addresses`, input)
      .then((r) => {
        invalidateHttpCache(CUSTOMERS_INVALIDATION);
        return r.data;
      }),

  updateAddress: (input: UpdateCustomerAddressInput) =>
    apiClient
      .put<ApiResponse<CustomerAddress>>(`${BASE}/${input.customerId}/addresses/${input.id}`, input)
      .then((r) => {
        invalidateHttpCache(CUSTOMERS_INVALIDATION);
        return r.data;
      }),

  deleteAddress: (customerId: string, id: string) =>
    apiClient.delete<ApiResponse<boolean>>(`${BASE}/${customerId}/addresses/${id}`).then((r) => {
      invalidateHttpCache(CUSTOMERS_INVALIDATION);
      return r.data;
    }),

  getContacts: (id: string) =>
    cachedGet<ApiResponse<CustomerContact[]>>(apiClient, `${BASE}/${id}/contacts`),

  createContact: (input: CustomerContactInput) =>
    apiClient
      .post<ApiResponse<CustomerContact>>(`${BASE}/${input.customerId}/contacts`, input)
      .then((r) => {
        invalidateHttpCache(CUSTOMERS_INVALIDATION);
        return r.data;
      }),

  updateContact: (input: UpdateCustomerContactInput) =>
    apiClient
      .put<ApiResponse<CustomerContact>>(`${BASE}/${input.customerId}/contacts/${input.id}`, input)
      .then((r) => {
        invalidateHttpCache(CUSTOMERS_INVALIDATION);
        return r.data;
      }),

  deleteContact: (customerId: string, id: string) =>
    apiClient.delete<ApiResponse<boolean>>(`${BASE}/${customerId}/contacts/${id}`).then((r) => {
      invalidateHttpCache(CUSTOMERS_INVALIDATION);
      return r.data;
    }),

  downloadStatement: (
    customerId: string,
    params: { from?: string | null; to?: string | null; format: 'pdf' | 'xlsx' },
  ) =>
    apiClient.get<Blob>(`${BASE}/${customerId}/statement`, {
      params: {
        from: params.from ?? undefined,
        to: params.to ?? undefined,
        format: params.format,
      },
      responseType: 'blob',
    }),

  listTags: (customerId: string) =>
    cachedGet<ApiResponse<Tag[]>>(apiClient, `${BASE}/${customerId}/tags`),

  attachTag: (customerId: string, tagId: string) =>
    apiClient.post<void>(`${BASE}/${customerId}/tags/${tagId}`).then((r) => {
      invalidateHttpCache(CUSTOMERS_INVALIDATION);
      return r.data;
    }),

  detachTag: (customerId: string, tagId: string) =>
    apiClient.delete<void>(`${BASE}/${customerId}/tags/${tagId}`).then((r) => {
      invalidateHttpCache(CUSTOMERS_INVALIDATION);
      return r.data;
    }),

  merge: (input: MergeCustomersInput) =>
    apiClient.post<ApiResponse<MergeCustomersResult>>(`${BASE}/merge`, input).then((r) => {
      invalidateHttpCache(CUSTOMERS_INVALIDATION);
      return r.data;
    }),
};

export interface MergeCustomersInput {
  operationId: string;
  sourceCustomerId: string;
  targetCustomerId: string;
  sourceUpdatedAtUtc: string;
  targetUpdatedAtUtc: string;
  notes?: string | null;
}

export interface MergeCustomersResult {
  operationId: string;
  sourceCustomerId: string;
  targetCustomerId: string;
  executedAtUtc: string;
  ordersMoved: number;
  invoicesMoved: number;
  paymentsMoved: number;
  addressesMoved: number;
  contactsMoved: number;
  commentsMoved: number;
  ledgerEntriesMoved: number;
  transactionsMoved: number;
  tagLinksMoved: number;
  dealerLinksMoved: number;
  customerUsersMoved: number;
  otherRecordsMoved: number;
  replayedFromIdempotency: boolean;
}
