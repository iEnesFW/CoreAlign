import type { ReturnRequestListParams } from '../model/return.types';

export const returnKeys = {
  all: ['returns'] as const,
  lists: () => [...returnKeys.all, 'list'] as const,
  list: (params: ReturnRequestListParams) => [...returnKeys.lists(), params] as const,
  details: () => [...returnKeys.all, 'detail'] as const,
  detail: (id: string | null) => [...returnKeys.details(), id] as const,
  byOrder: (orderId: string | null) => [...returnKeys.all, 'by-order', orderId] as const,
};
