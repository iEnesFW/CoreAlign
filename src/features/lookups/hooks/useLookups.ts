import { useQuery } from '@tanstack/react-query';
import { lookupsApi } from '../api/lookupsApi';

// Reference data changes very rarely — keep it fresh for an hour to avoid refetching.
const ONE_HOUR = 60 * 60 * 1000;

export const useCurrenciesQuery = (isActive = true) =>
  useQuery({
    queryKey: ['lookups', 'currencies', { isActive }] as const,
    queryFn: () => lookupsApi.currencies(isActive),
    staleTime: ONE_HOUR,
  });

export const useCountriesQuery = (isActive = true) =>
  useQuery({
    queryKey: ['lookups', 'countries', { isActive }] as const,
    queryFn: () => lookupsApi.countries(isActive),
    staleTime: ONE_HOUR,
  });

export const useProvincesQuery = (countryCode = 'TR') =>
  useQuery({
    queryKey: ['lookups', 'provinces', { countryCode }] as const,
    queryFn: () => lookupsApi.provinces(countryCode),
    staleTime: ONE_HOUR,
  });

export const useDistrictsQuery = (provinceId: number | null) =>
  useQuery({
    queryKey: ['lookups', 'districts', { provinceId }] as const,
    queryFn: () => lookupsApi.districts(provinceId as number),
    enabled: provinceId !== null,
    staleTime: ONE_HOUR,
  });
