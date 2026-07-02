import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { productsApi } from '../api/productsApi';
import { productKeys } from './productKeys';
import { patchProductInPaged, type PagedProductsResponse } from '../model/productCachePatch';
import type {
  AddProductComponentInput,
  CreateProductInput,
  ProductListParams,
  UpdateProductComponentInput,
  UpdateProductInput,
} from '../model/product.types';

export const useProductsQuery = (params: ProductListParams, options?: { enabled?: boolean }) =>
  useQuery({
    queryKey: productKeys.list(params),
    queryFn: () => productsApi.list(params),
    placeholderData: (previous) => previous,
    enabled: options?.enabled ?? true,
  });

export const useProductQuery = (id: string | null) =>
  useQuery({
    queryKey: productKeys.detail(id),
    queryFn: () => productsApi.getById(id as string),
    enabled: id !== null,
  });

export const useStockTransactionsQuery = (id: string | null) =>
  useQuery({
    queryKey: productKeys.transactions(id),
    queryFn: () => productsApi.getTransactions(id as string),
    enabled: id !== null,
  });

export const useCreateProduct = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: CreateProductInput) => productsApi.create(input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: productKeys.lists() });
    },
  });
};

export const useUpdateProduct = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: UpdateProductInput) => productsApi.update(input),
    onMutate: async (input) => {
      await queryClient.cancelQueries({ queryKey: productKeys.lists() });
      const snapshots = queryClient.getQueriesData<PagedProductsResponse>({
        queryKey: productKeys.lists(),
      });
      queryClient.setQueriesData<PagedProductsResponse>({ queryKey: productKeys.lists() }, (old) =>
        patchProductInPaged(old, input),
      );
      return { snapshots };
    },
    onError: (_error, _input, context) => {
      context?.snapshots.forEach(([key, data]) => queryClient.setQueryData(key, data));
    },
    onSettled: (_data, _error, input) => {
      queryClient.invalidateQueries({ queryKey: productKeys.lists() });
      queryClient.invalidateQueries({ queryKey: productKeys.detail(input.id) });
    },
  });
};

export const useDeleteProduct = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => productsApi.remove(id),
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: productKeys.lists() });
      queryClient.removeQueries({ queryKey: productKeys.detail(id) });
    },
  });
};

export const useProductComponentsQuery = (parentProductId: string | null) =>
  useQuery({
    queryKey: productKeys.components(parentProductId),
    queryFn: () => productsApi.getComponents(parentProductId as string),
    enabled: parentProductId !== null,
  });

export const useAddProductComponent = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: AddProductComponentInput) => productsApi.addComponent(input),
    onSuccess: (_, vars) =>
      queryClient.invalidateQueries({ queryKey: productKeys.components(vars.parentProductId) }),
  });
};

export const useUpdateProductComponent = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: UpdateProductComponentInput) => productsApi.updateComponent(input),
    onSuccess: (_, vars) =>
      queryClient.invalidateQueries({ queryKey: productKeys.components(vars.parentProductId) }),
  });
};

export const useRemoveProductComponent = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ parentProductId, id }: { parentProductId: string; id: string }) =>
      productsApi.removeComponent(parentProductId, id),
    onSuccess: (_, vars) =>
      queryClient.invalidateQueries({ queryKey: productKeys.components(vars.parentProductId) }),
  });
};
