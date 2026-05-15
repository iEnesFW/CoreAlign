import { apiClient } from '@/shared/api/apiClient';
import { cachedGet, invalidateHttpCache } from '@/shared/http/httpCache';
import type { ApiResponse, PagedResult } from '@/shared/types/api';
import type {
  Customer,
  CreateCustomerInput,
  CustomerAddress,
  CustomerAddressInput,
  CustomerContact,
  CustomerContactInput,
  CustomerListParams,
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

  getById: (id: string) => cachedGet<ApiResponse<Customer>>(apiClient, `${BASE}/${id}`),

  getSummary: (id: string) =>
    cachedGet<ApiResponse<CustomerSummary>>(apiClient, `${BASE}/${id}/summary`),

  getOverview: (id: string) =>
    cachedGet<ApiResponse<CustomerOverview>>(apiClient, `${BASE}/${id}/overview`),

  getTransactions: (id: string, page = 1, pageSize = 50) =>
    apiClient
      .get<ApiResponse<PagedResult<CustomerTransaction>>>(`${BASE}/${id}/transactions`, {
        params: { page, pageSize },
      })
      .then((r) => r.data),

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
};
