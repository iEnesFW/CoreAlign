import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { quotesApi } from '../api/quotesApi';
import { quoteKeys } from './quoteKeys';
import type { CreateQuotePayload, QuoteListParams } from '../model/quote.types';

export const useQuotesQuery = (params: QuoteListParams, options?: { enabled?: boolean }) =>
  useQuery({
    queryKey: quoteKeys.list(params),
    queryFn: () => quotesApi.list(params),
    placeholderData: (previous) => previous,
    enabled: options?.enabled ?? true,
  });

export const useQuoteQuery = (id: string | null) =>
  useQuery({
    queryKey: quoteKeys.detail(id),
    queryFn: () => quotesApi.getById(id as string),
    enabled: id !== null,
  });

export const useCreateQuote = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: CreateQuotePayload) => quotesApi.create(payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: quoteKeys.lists() });
    },
  });
};

export const useDeleteQuote = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => quotesApi.remove(id),
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: quoteKeys.lists() });
      queryClient.invalidateQueries({ queryKey: quoteKeys.detail(id) });
    },
  });
};

export const useSendQuote = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => quotesApi.send(id),
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: quoteKeys.lists() });
      queryClient.invalidateQueries({ queryKey: quoteKeys.detail(id) });
    },
  });
};

export const useAcceptQuote = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => quotesApi.accept(id),
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: quoteKeys.lists() });
      queryClient.invalidateQueries({ queryKey: quoteKeys.detail(id) });
    },
  });
};

export const useRejectQuote = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, reason }: { id: string; reason?: string | null }) =>
      quotesApi.reject(id, reason),
    onSuccess: (_, vars) => {
      queryClient.invalidateQueries({ queryKey: quoteKeys.lists() });
      queryClient.invalidateQueries({ queryKey: quoteKeys.detail(vars.id) });
    },
  });
};

export const useConvertQuoteToOrder = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => quotesApi.convertToOrder(id),
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: quoteKeys.lists() });
      queryClient.invalidateQueries({ queryKey: quoteKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: ['orders'] });
    },
  });
};
