import { apiClient } from '@/shared/api/apiClient';
import { cachedGet, invalidateHttpCache } from '@/shared/http/httpCache';
import type { ApiResponse, PagedResult } from '@/shared/types/api';
import type {
  AddProductComponentInput,
  CreateProductInput,
  Product,
  ProductComponent,
  ProductListParams,
  StockTransaction,
  UpdateProductComponentInput,
  UpdateProductInput,
} from '../model/product.types';

const BASE = '/products';

const PRODUCTS_INVALIDATION = [/\/products/i] as const;

export const productsApi = {
  list: (params: ProductListParams) =>
    cachedGet<ApiResponse<PagedResult<Product>>>(apiClient, BASE, { params }),

  getById: (id: string) => cachedGet<ApiResponse<Product>>(apiClient, `${BASE}/${id}`),

  create: (input: CreateProductInput) =>
    apiClient.post<ApiResponse<Product>>(BASE, input).then((r) => {
      invalidateHttpCache(PRODUCTS_INVALIDATION);
      return r.data;
    }),

  update: (input: UpdateProductInput) =>
    apiClient.put<ApiResponse<Product>>(`${BASE}/${input.id}`, input).then((r) => {
      invalidateHttpCache(PRODUCTS_INVALIDATION);
      return r.data;
    }),

  remove: (id: string) =>
    apiClient.delete<ApiResponse<boolean>>(`${BASE}/${id}`).then((r) => {
      invalidateHttpCache(PRODUCTS_INVALIDATION);
      return r.data;
    }),

  getTransactions: (id: string, page = 1, pageSize = 50) =>
    apiClient
      .get<ApiResponse<PagedResult<StockTransaction>>>(`${BASE}/${id}/transactions`, {
        params: { page, pageSize },
      })
      .then((r) => r.data),

  getComponents: (id: string) =>
    cachedGet<ApiResponse<ProductComponent[]>>(apiClient, `${BASE}/${id}/components`),

  addComponent: (input: AddProductComponentInput) =>
    apiClient
      .post<ApiResponse<ProductComponent>>(`${BASE}/${input.parentProductId}/components`, input)
      .then((r) => {
        invalidateHttpCache(PRODUCTS_INVALIDATION);
        return r.data;
      }),

  updateComponent: (input: UpdateProductComponentInput) =>
    apiClient
      .put<
        ApiResponse<ProductComponent>
      >(`${BASE}/${input.parentProductId}/components/${input.id}`, input)
      .then((r) => {
        invalidateHttpCache(PRODUCTS_INVALIDATION);
        return r.data;
      }),

  removeComponent: (parentProductId: string, id: string) =>
    apiClient
      .delete<ApiResponse<boolean>>(`${BASE}/${parentProductId}/components/${id}`)
      .then((r) => {
        invalidateHttpCache(PRODUCTS_INVALIDATION);
        return r.data;
      }),
};
