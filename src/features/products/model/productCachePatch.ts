import type { ApiResponse, PagedResult } from '@/shared/types/api';
import type { Product, UpdateProductInput } from './product.types';

export type PagedProductsResponse = ApiResponse<PagedResult<Product>>;

const mergeDefined = (row: Product, input: UpdateProductInput): Product => {
  const next: Record<string, unknown> = { ...row };
  for (const [key, value] of Object.entries(input)) {
    if (value !== undefined) next[key] = value;
  }
  return next as unknown as Product;
};

export const patchProductInPaged = (
  old: PagedProductsResponse | undefined,
  input: UpdateProductInput,
): PagedProductsResponse | undefined => {
  if (!old?.data?.items) return old;
  if (!old.data.items.some((item) => item.id === input.id)) return old;
  return {
    ...old,
    data: {
      ...old.data,
      items: old.data.items.map((item) =>
        item.id === input.id ? mergeDefined(item, input) : item,
      ),
    },
  };
};
