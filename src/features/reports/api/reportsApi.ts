import { apiClient } from '@/shared/api/apiClient';
import { cachedGet } from '@/shared/http/httpCache';
import type { ApiResponse } from '@/shared/types/api';
import type {
  AgingSummaryReport,
  CashPositionReport,
  DocumentNumberGapReport,
  DuplicateEntity,
  DuplicateKeyKind,
  DuplicateReport,
  SalesBucket,
  SalesByPeriodReport,
  TopCustomerReportRow,
  TopProductReportRow,
} from '../model/reports.types';

const BASE = '/reports';

export const reportsApi = {
  salesByPeriod: (params: { fromUtc: string; toUtc: string; bucket?: SalesBucket }) =>
    cachedGet<ApiResponse<SalesByPeriodReport>>(apiClient, `${BASE}/sales-by-period`, {
      params,
    }),

  topCustomers: (params: { limit?: number; fromUtc?: string; toUtc?: string }) =>
    cachedGet<ApiResponse<TopCustomerReportRow[]>>(apiClient, `${BASE}/top-customers`, {
      params,
    }),

  topProducts: (params: { limit?: number; fromUtc?: string; toUtc?: string }) =>
    cachedGet<ApiResponse<TopProductReportRow[]>>(apiClient, `${BASE}/top-products`, {
      params,
    }),

  agingSummary: (params: { asOfUtc?: string } = {}) =>
    cachedGet<ApiResponse<AgingSummaryReport>>(apiClient, `${BASE}/aging-summary`, {
      params,
    }),

  cashPosition: (params: { asOfUtc?: string } = {}) =>
    cachedGet<ApiResponse<CashPositionReport>>(apiClient, `${BASE}/cash-position`, {
      params,
    }),

  duplicates: (params: { entity: DuplicateEntity; key: DuplicateKeyKind }) =>
    cachedGet<ApiResponse<DuplicateReport>>(apiClient, `${BASE}/duplicates`, {
      params,
    }),

  documentNumberGaps: (params: { year?: number } = {}) =>
    cachedGet<ApiResponse<DocumentNumberGapReport>>(apiClient, `${BASE}/document-number-gaps`, {
      params,
    }),
};
