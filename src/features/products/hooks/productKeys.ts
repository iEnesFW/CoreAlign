import type { ProductListParams } from '../model/product.types';

export const productKeys = {
  all: ['products'] as const,
  lists: () => [...productKeys.all, 'list'] as const,
  list: (params: ProductListParams) => [...productKeys.lists(), params] as const,
  details: () => [...productKeys.all, 'detail'] as const,
  detail: (id: string | null) => [...productKeys.details(), id] as const,
  transactions: (id: string | null) => [...productKeys.all, 'transactions', id] as const,
  components: (id: string | null) => [...productKeys.all, 'components', id] as const,
};
