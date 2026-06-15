import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  productVariantsApi,
  type CreateProductVariantPayload,
  type ProductVariant,
  type UpdateProductVariantPayload,
} from '../api/productVariantsApi';

const queryKey = (productId: string) => ['products', productId, 'variants'];

export const useProductVariantsQuery = (productId: string | null) =>
  useQuery({
    queryKey: queryKey(productId ?? 'none'),
    queryFn: async () => {
      if (!productId) return [] as ProductVariant[];
      const response = await productVariantsApi.list(productId);
      return response.data ?? [];
    },
    enabled: !!productId,
  });

export const useCreateProductVariant = (productId: string) => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: CreateProductVariantPayload) =>
      productVariantsApi.create(productId, payload),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: queryKey(productId) });
    },
  });
};

export const useUpdateProductVariant = (productId: string) => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      variantId,
      payload,
    }: {
      variantId: string;
      payload: UpdateProductVariantPayload;
    }) => productVariantsApi.update(productId, variantId, payload),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: queryKey(productId) });
    },
  });
};

export const useDeleteProductVariant = (productId: string) => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (variantId: string) => productVariantsApi.remove(productId, variantId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: queryKey(productId) });
    },
  });
};
