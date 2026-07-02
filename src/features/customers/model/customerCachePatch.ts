import type { ApiResponse, PagedResult } from '@/shared/types/api';
import type { Customer, UpdateCustomerInput } from './customer.types';

export type PagedCustomersResponse = ApiResponse<PagedResult<Customer>>;

const mergeDefined = (row: Customer, input: UpdateCustomerInput): Customer => {
  const next: Record<string, unknown> = { ...row };
  for (const [key, value] of Object.entries(input)) {
    if (value !== undefined) next[key] = value;
  }
  return next as unknown as Customer;
};

export const patchCustomerInPaged = (
  old: PagedCustomersResponse | undefined,
  input: UpdateCustomerInput,
): PagedCustomersResponse | undefined => {
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
