import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  productImagesApi,
  type ProductImage,
  type UpdateProductImagePayload,
} from '../api/productImagesApi';

const queryKey = (productId: string) => ['products', productId, 'images'];

export const useProductImagesQuery = (productId: string | null) =>
  useQuery({
    queryKey: queryKey(productId ?? 'none'),
    queryFn: async () => {
      if (!productId) return [] as ProductImage[];
      const response = await productImagesApi.list(productId);
      return response.data ?? [];
    },
    enabled: !!productId,
  });

export const useUploadProductImage = (productId: string) => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      file,
      altText,
      makePrimary,
    }: {
      file: File;
      altText?: string;
      makePrimary?: boolean;
    }) => productImagesApi.upload(productId, file, { altText, makePrimary }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: queryKey(productId) });
    },
  });
};

export const useUpdateProductImage = (productId: string) => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ imageId, payload }: { imageId: string; payload: UpdateProductImagePayload }) =>
      productImagesApi.update(productId, imageId, payload),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: queryKey(productId) });
    },
  });
};

export const useDeleteProductImage = (productId: string) => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (imageId: string) => productImagesApi.remove(productId, imageId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: queryKey(productId) });
    },
  });
};
