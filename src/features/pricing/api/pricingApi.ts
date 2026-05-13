import { apiClient } from '@/shared/api/apiClient';
import { cachedGet, invalidateHttpCache } from '@/shared/http/httpCache';
import type { ApiResponse } from '@/shared/types/api';
import type {
  AccountingPeriod,
  CreateCustomerProductPriceInput,
  CustomerProductPrice,
  ResolvedPrice,
} from '../model/pricing.types';

const ACCOUNTING_BASE = '/accounting';
const PRICING_BASE = '/pricing';
const INVALIDATION = [/\/pricing\//i, /\/accounting\//i] as const;

export const pricingApi = {
  resolvePrice: (productId: string, customerId: string, quantity = 1, currency?: string) =>
    cachedGet<ApiResponse<ResolvedPrice>>(apiClient, `${PRICING_BASE}/resolve`, {
      params: { productId, customerId, quantity, currency },
    }),

  listCustomerProductPrices: (customerId?: string, productId?: string) =>
    cachedGet<ApiResponse<CustomerProductPrice[]>>(
      apiClient,
      `${PRICING_BASE}/customer-product-prices`,
      { params: { customerId, productId } },
    ),

  createCustomerProductPrice: (input: CreateCustomerProductPriceInput) =>
    apiClient
      .post<ApiResponse<CustomerProductPrice>>(`${PRICING_BASE}/customer-product-prices`, input)
      .then((r) => {
        invalidateHttpCache(INVALIDATION);
        return r.data;
      }),

  updateCustomerProductPrice: (input: CustomerProductPrice) =>
    apiClient
      .put<
        ApiResponse<CustomerProductPrice>
      >(`${PRICING_BASE}/customer-product-prices/${input.id}`, input)
      .then((r) => {
        invalidateHttpCache(INVALIDATION);
        return r.data;
      }),

  deleteCustomerProductPrice: (id: string) =>
    apiClient
      .delete<ApiResponse<boolean>>(`${PRICING_BASE}/customer-product-prices/${id}`)
      .then((r) => {
        invalidateHttpCache(INVALIDATION);
        return r.data;
      }),

  listAccountingPeriods: (year?: number) =>
    cachedGet<ApiResponse<AccountingPeriod[]>>(apiClient, `${ACCOUNTING_BASE}/periods`, {
      params: year ? { year } : {},
    }),

  createAccountingPeriod: (year: number, month: number) =>
    apiClient
      .post<ApiResponse<AccountingPeriod>>(`${ACCOUNTING_BASE}/periods`, { year, month })
      .then((r) => {
        invalidateHttpCache(INVALIDATION);
        return r.data;
      }),

  closePeriod: (id: string, notes?: string | null) =>
    apiClient
      .post<ApiResponse<AccountingPeriod>>(`${ACCOUNTING_BASE}/periods/${id}/close`, {
        id,
        notes: notes ?? null,
      })
      .then((r) => {
        invalidateHttpCache(INVALIDATION);
        return r.data;
      }),

  reopenPeriod: (id: string) =>
    apiClient
      .post<ApiResponse<AccountingPeriod>>(`${ACCOUNTING_BASE}/periods/${id}/reopen`, { id })
      .then((r) => {
        invalidateHttpCache(INVALIDATION);
        return r.data;
      }),

  lockPeriod: (id: string) =>
    apiClient
      .post<ApiResponse<AccountingPeriod>>(`${ACCOUNTING_BASE}/periods/${id}/lock`, { id })
      .then((r) => {
        invalidateHttpCache(INVALIDATION);
        return r.data;
      }),
};
