import { useQuery } from '@tanstack/react-query';
import { reportsApi } from '../api/reportsApi';
import type { SalesBucket } from '../model/reports.types';

const ONE_MINUTE_MS = 60 * 1000;

export const useSalesByPeriodQuery = (params: {
  fromUtc: string;
  toUtc: string;
  bucket?: SalesBucket;
}) =>
  useQuery({
    queryKey: ['reports', 'sales-by-period', params] as const,
    queryFn: () => reportsApi.salesByPeriod(params),
    staleTime: ONE_MINUTE_MS,
  });

export const useTopCustomersQuery = (params: {
  limit?: number;
  fromUtc?: string;
  toUtc?: string;
}) =>
  useQuery({
    queryKey: ['reports', 'top-customers', params] as const,
    queryFn: () => reportsApi.topCustomers(params),
    staleTime: ONE_MINUTE_MS,
  });

export const useTopProductsQuery = (params: { limit?: number; fromUtc?: string; toUtc?: string }) =>
  useQuery({
    queryKey: ['reports', 'top-products', params] as const,
    queryFn: () => reportsApi.topProducts(params),
    staleTime: ONE_MINUTE_MS,
  });

export const useAgingSummaryQuery = (params: { asOfUtc?: string } = {}) =>
  useQuery({
    queryKey: ['reports', 'aging-summary', params] as const,
    queryFn: () => reportsApi.agingSummary(params),
    staleTime: ONE_MINUTE_MS,
  });
