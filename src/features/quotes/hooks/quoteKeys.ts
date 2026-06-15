import type { QuoteListParams } from '../model/quote.types';

export const quoteKeys = {
  all: ['quotes'] as const,
  lists: () => [...quoteKeys.all, 'list'] as const,
  list: (params: QuoteListParams) => [...quoteKeys.lists(), params] as const,
  details: () => [...quoteKeys.all, 'detail'] as const,
  detail: (id: string | null) => [...quoteKeys.details(), id] as const,
};
