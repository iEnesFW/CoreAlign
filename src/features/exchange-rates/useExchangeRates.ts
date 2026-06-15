import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { exchangeRatesApi } from './exchangeRatesApi';

const LIST_KEY = (from?: string, to?: string, currency?: string) =>
  ['exchange-rates', from ?? '', to ?? '', currency ?? ''] as const;

export const useExchangeRatesQuery = (from?: string, to?: string, currency?: string) =>
  useQuery({
    queryKey: LIST_KEY(from, to, currency),
    queryFn: () => exchangeRatesApi.list(from, to, currency),
    staleTime: 5 * 60 * 1000,
  });

export const useRefreshExchangeRates = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: () => exchangeRatesApi.refresh(),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['exchange-rates'] }),
  });
};
