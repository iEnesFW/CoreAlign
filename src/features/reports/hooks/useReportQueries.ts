import { useQuery } from '@tanstack/react-query';
import { reportsApi } from '../api/reportsApi';
import type { DuplicateEntity, DuplicateKeyKind, SalesBucket } from '../model/reports.types';

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

export const useCashPositionQuery = (params: { asOfUtc?: string } = {}) =>
  useQuery({
    queryKey: ['reports', 'cash-position', params] as const,
    queryFn: () => reportsApi.cashPosition(params),
    staleTime: ONE_MINUTE_MS,
  });

export const useDuplicatesQuery = (params: { entity: DuplicateEntity; key: DuplicateKeyKind }) =>
  useQuery({
    queryKey: ['reports', 'duplicates', params] as const,
    queryFn: () => reportsApi.duplicates(params),
    staleTime: ONE_MINUTE_MS,
  });

export const useDocumentNumberGapsQuery = (params: { year?: number } = {}) =>
  useQuery({
    queryKey: ['reports', 'document-number-gaps', params] as const,
    queryFn: () => reportsApi.documentNumberGaps(params),
    staleTime: ONE_MINUTE_MS,
  });
