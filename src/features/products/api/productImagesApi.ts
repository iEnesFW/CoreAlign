import { apiClient } from '@/shared/api/apiClient';
import type { ApiResponse } from '@/shared/types/api';

export interface ProductImage {
  id: string;
  productId: string;
  storageKey: string;
  publicUrl: string;
  contentType: string;
  sizeBytes: number;
  altText: string | null;
  displayOrder: number;
  isPrimary: boolean;
  uploadedAtUtc: string;
}

export interface UpdateProductImagePayload {
  altText?: string | null;
  displayOrder: number;
  isPrimary: boolean;
}

const base = (productId: string) => `/products/${productId}/images`;

export const productImagesApi = {
  list: (productId: string) =>
    apiClient.get<ApiResponse<ProductImage[]>>(base(productId)).then((r) => r.data),

  upload: async (
    productId: string,
    file: File,
    options?: { altText?: string; makePrimary?: boolean },
  ) => {
    const form = new FormData();
    form.append('file', file);
    if (options?.altText) form.append('altText', options.altText);
    form.append('makePrimary', String(options?.makePrimary ?? false));
    const response = await apiClient.post<ApiResponse<ProductImage>>(base(productId), form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
    return response.data;
  },

  update: async (productId: string, imageId: string, payload: UpdateProductImagePayload) => {
    const response = await apiClient.put<ApiResponse<ProductImage>>(
      `${base(productId)}/${imageId}`,
      payload,
    );
    return response.data;
  },

  remove: async (productId: string, imageId: string) => {
    const response = await apiClient.delete<ApiResponse<boolean>>(`${base(productId)}/${imageId}`);
    return response.data;
  },
};
