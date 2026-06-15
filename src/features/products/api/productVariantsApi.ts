import { apiClient } from '@/shared/api/apiClient';
import type { ApiResponse } from '@/shared/types/api';

export interface ProductVariant {
  id: string;
  parentProductId: string;
  sku: string;
  barcode: string | null;
  variantAttributesJson: string;
  priceOverride: number | null;
  stockQuantity: number;
  isActive: boolean;
  concurrencyToken: number;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface CreateProductVariantPayload {
  sku: string;
  barcode?: string | null;
  variantAttributesJson: string;
  priceOverride?: number | null;
  stockQuantity: number;
  isActive: boolean;
}

export interface UpdateProductVariantPayload {
  sku: string;
  barcode?: string | null;
  variantAttributesJson: string;
  priceOverride?: number | null;
  isActive: boolean;
}

const base = (productId: string) => `/products/${productId}/variants`;

export const productVariantsApi = {
  list: (productId: string) =>
    apiClient.get<ApiResponse<ProductVariant[]>>(base(productId)).then((r) => r.data),

  create: (productId: string, payload: CreateProductVariantPayload) =>
    apiClient.post<ApiResponse<ProductVariant>>(base(productId), payload).then((r) => r.data),

  update: (productId: string, variantId: string, payload: UpdateProductVariantPayload) =>
    apiClient
      .put<ApiResponse<ProductVariant>>(`${base(productId)}/${variantId}`, payload)
      .then((r) => r.data),

  remove: (productId: string, variantId: string) =>
    apiClient.delete<ApiResponse<boolean>>(`${base(productId)}/${variantId}`).then((r) => r.data),
};
