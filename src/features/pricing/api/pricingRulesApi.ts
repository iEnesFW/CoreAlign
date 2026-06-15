import { apiClient } from '@/shared/api/apiClient';
import { cachedGet, invalidateHttpCache } from '@/shared/http/httpCache';
import type { ApiResponse } from '@/shared/types/api';
import type {
  DiscountRule,
  DiscountRuleInput,
  DiscountRuleUpdateInput,
  PriceListItem,
  PriceListItemInput,
  PriceListItemUpdateInput,
  TaxRule,
  TaxRuleInput,
  TaxRuleUpdateInput,
} from '../model/pricingRules.types';

const PRICE_LIST_INVALIDATION = [/\/price-lists\//i, /\/master-data\/price-lists/i] as const;
const DISCOUNT_INVALIDATION = [/\/discount-rules/i] as const;
const TAX_INVALIDATION = [/\/tax-rules/i] as const;

export const priceListItemsApi = {
  list: (priceListId: string) =>
    cachedGet<ApiResponse<PriceListItem[]>>(apiClient, `/price-lists/${priceListId}/items`),

  add: (input: PriceListItemInput) =>
    apiClient
      .post<ApiResponse<PriceListItem>>(`/price-lists/${input.priceListId}/items`, input)
      .then((r) => {
        invalidateHttpCache(PRICE_LIST_INVALIDATION);
        return r.data;
      }),

  update: (input: PriceListItemUpdateInput) =>
    apiClient
      .put<ApiResponse<PriceListItem>>(`/price-lists/${input.priceListId}/items/${input.id}`, input)
      .then((r) => {
        invalidateHttpCache(PRICE_LIST_INVALIDATION);
        return r.data;
      }),

  remove: (priceListId: string, id: string) =>
    apiClient.delete<ApiResponse<boolean>>(`/price-lists/${priceListId}/items/${id}`).then((r) => {
      invalidateHttpCache(PRICE_LIST_INVALIDATION);
      return r.data;
    }),
};

export const discountRulesApi = {
  list: (isActive?: boolean) =>
    cachedGet<ApiResponse<DiscountRule[]>>(apiClient, '/discount-rules', {
      params: isActive === undefined ? {} : { isActive },
    }),
  getById: (id: string) => cachedGet<ApiResponse<DiscountRule>>(apiClient, `/discount-rules/${id}`),
  create: (input: DiscountRuleInput) =>
    apiClient.post<ApiResponse<DiscountRule>>('/discount-rules', input).then((r) => {
      invalidateHttpCache(DISCOUNT_INVALIDATION);
      return r.data;
    }),
  update: (input: DiscountRuleUpdateInput) =>
    apiClient.put<ApiResponse<DiscountRule>>(`/discount-rules/${input.id}`, input).then((r) => {
      invalidateHttpCache(DISCOUNT_INVALIDATION);
      return r.data;
    }),
  remove: (id: string) =>
    apiClient.delete<ApiResponse<boolean>>(`/discount-rules/${id}`).then((r) => {
      invalidateHttpCache(DISCOUNT_INVALIDATION);
      return r.data;
    }),
};

export const taxRulesApi = {
  list: (isActive?: boolean) =>
    cachedGet<ApiResponse<TaxRule[]>>(apiClient, '/tax-rules', {
      params: isActive === undefined ? {} : { isActive },
    }),
  getById: (id: string) => cachedGet<ApiResponse<TaxRule>>(apiClient, `/tax-rules/${id}`),
  create: (input: TaxRuleInput) =>
    apiClient.post<ApiResponse<TaxRule>>('/tax-rules', input).then((r) => {
      invalidateHttpCache(TAX_INVALIDATION);
      return r.data;
    }),
  update: (input: TaxRuleUpdateInput) =>
    apiClient.put<ApiResponse<TaxRule>>(`/tax-rules/${input.id}`, input).then((r) => {
      invalidateHttpCache(TAX_INVALIDATION);
      return r.data;
    }),
  remove: (id: string) =>
    apiClient.delete<ApiResponse<boolean>>(`/tax-rules/${id}`).then((r) => {
      invalidateHttpCache(TAX_INVALIDATION);
      return r.data;
    }),
};
