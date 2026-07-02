import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  fxRatesApi,
  type FxPreferenceDto,
  type FxRateSnapshot,
  type FxResolutionDto,
} from '../api/fxRatesApi';

const FOUR_HOURS = 4 * 60 * 60 * 1000;

const unwrapOrNull = <T>([data, error]: [T, null] | [null, Error]): T => {
  if (error) {
    throw error;
  }
  return data as T;
};

export const useLatestFxRatesQuery = () =>
  useQuery({
    queryKey: ['fx-rates', 'latest'] as const,
    queryFn: async () => unwrapOrNull(await fxRatesApi.getLatest()),
    staleTime: FOUR_HOURS,
  });

export const useFxRateQuery = (currencyCode: string | undefined, asOfDate?: string) =>
  useQuery({
    queryKey: ['fx-rates', currencyCode ?? '', asOfDate ?? ''] as const,
    queryFn: async () => unwrapOrNull(await fxRatesApi.getRate(currencyCode!, asOfDate)),
    enabled: Boolean(currencyCode),
    staleTime: FOUR_HOURS,
  });

export const useFxPreferencesQuery = () =>
  useQuery({
    queryKey: ['fx-rates', 'preferences'] as const,
    queryFn: async () => unwrapOrNull(await fxRatesApi.getPreferences()),
    staleTime: FOUR_HOURS,
  });

export const useUpdateFxPreferencesMutation = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (payload: FxPreferenceDto) =>
      unwrapOrNull(await fxRatesApi.updatePreferences(payload)),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['fx-rates', 'preferences'] });
    },
  });
};

export const useResolveFxRateQuery = (
  currencyCode: string | undefined,
  asOfDate?: string,
  enabled: boolean = true,
) =>
  useQuery({
    queryKey: ['fx-rates', 'resolve', currencyCode ?? '', asOfDate ?? ''] as const,
    queryFn: async () => unwrapOrNull(await fxRatesApi.resolve(currencyCode!, asOfDate)),
    enabled: enabled && Boolean(currencyCode),
    staleTime: FOUR_HOURS,
  });

export type { FxPreferenceDto, FxRateSnapshot, FxResolutionDto };
