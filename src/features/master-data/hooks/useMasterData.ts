import { useQuery } from '@tanstack/react-query';
import { masterDataApi } from '../api/masterDataApi';

const FIVE_MINUTES = 5 * 60 * 1000;

export const useBrandsQuery = (isActive?: boolean) =>
  useQuery({
    queryKey: ['master-data', 'brands', { isActive }] as const,
    queryFn: () => masterDataApi.brands.list(isActive),
    staleTime: FIVE_MINUTES,
  });

export const useCategoriesQuery = (isActive?: boolean) =>
  useQuery({
    queryKey: ['master-data', 'categories', { isActive }] as const,
    queryFn: () => masterDataApi.categories.list(isActive),
    staleTime: FIVE_MINUTES,
  });

export const useCustomerGroupsQuery = (isActive?: boolean) =>
  useQuery({
    queryKey: ['master-data', 'customer-groups', { isActive }] as const,
    queryFn: () => masterDataApi.customerGroups.list(isActive),
    staleTime: FIVE_MINUTES,
  });

export const useUomsQuery = (isActive?: boolean) =>
  useQuery({
    queryKey: ['master-data', 'units-of-measure', { isActive }] as const,
    queryFn: () => masterDataApi.uoms.list(isActive),
    staleTime: FIVE_MINUTES,
  });

export const useTaxRatesQuery = (isActive?: boolean) =>
  useQuery({
    queryKey: ['master-data', 'tax-rates', { isActive }] as const,
    queryFn: () => masterDataApi.taxRates.list(isActive),
    staleTime: FIVE_MINUTES,
  });

export const usePaymentTermsQuery = (isActive?: boolean) =>
  useQuery({
    queryKey: ['master-data', 'payment-terms', { isActive }] as const,
    queryFn: () => masterDataApi.paymentTerms.list(isActive),
    staleTime: FIVE_MINUTES,
  });

export const usePriceListsQuery = (isActive?: boolean) =>
  useQuery({
    queryKey: ['master-data', 'price-lists', { isActive }] as const,
    queryFn: () => masterDataApi.priceLists.list(isActive),
    staleTime: FIVE_MINUTES,
  });

export const useWarehousesQuery = (isActive?: boolean) =>
  useQuery({
    queryKey: ['master-data', 'warehouses', { isActive }] as const,
    queryFn: () => masterDataApi.warehouses.list(isActive),
    staleTime: FIVE_MINUTES,
  });
