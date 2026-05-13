import type { CustomerListParams } from '../model/customer.types';

export const customerKeys = {
  all: ['customers'] as const,
  lists: () => [...customerKeys.all, 'list'] as const,
  list: (params: CustomerListParams) => [...customerKeys.lists(), params] as const,
  details: () => [...customerKeys.all, 'detail'] as const,
  detail: (id: string | null) => [...customerKeys.details(), id] as const,
  summaries: () => [...customerKeys.all, 'summary'] as const,
  summary: (id: string | null) => [...customerKeys.summaries(), id] as const,
  transactions: (id: string | null) => [...customerKeys.all, 'transactions', id] as const,
  addresses: (customerId: string | null) => [...customerKeys.all, 'addresses', customerId] as const,
  contacts: (customerId: string | null) => [...customerKeys.all, 'contacts', customerId] as const,
};
