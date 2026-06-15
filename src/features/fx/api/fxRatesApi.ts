import { apiClient } from '@/shared/api/apiClient';
import { safeRequest, type SafeResult } from '@/shared/lib/safeRequest';

export interface FxRateSnapshot {
  currencyCode: string;
  buyingRate: number;
  sellingRate: number;
  effectiveDate: string;
  source: string;
}

export interface FxConvertRequest {
  amount: number;
  fromCurrency: string;
  toCurrency: string;
  asOfDate?: string;
}

export interface FxConvertResponse {
  fromCurrency: string;
  toCurrency: string;
  originalAmount: number;
  convertedAmount: number;
  asOfDate: string;
}

const BASE = '/fx-rates';

const unwrap = <T>(response: { data: T }): T => response.data;

export const fxRatesApi = {
  getLatest: (): Promise<SafeResult<FxRateSnapshot[]>> =>
    safeRequest(apiClient.get<FxRateSnapshot[]>(`${BASE}/latest`).then(unwrap)),

  getRate: (currencyCode: string, asOfDate?: string): Promise<SafeResult<FxRateSnapshot>> =>
    safeRequest(
      apiClient
        .get<FxRateSnapshot>(`${BASE}/${encodeURIComponent(currencyCode)}`, {
          params: asOfDate ? { asOfDate } : undefined,
        })
        .then(unwrap),
    ),

  convert: (payload: FxConvertRequest): Promise<SafeResult<FxConvertResponse>> =>
    safeRequest(apiClient.post<FxConvertResponse>(`${BASE}/convert`, payload).then(unwrap)),

  triggerSync: (targetDate?: string): Promise<SafeResult<{ inserted: number }>> =>
    safeRequest(
      apiClient
        .post<{ inserted: number }>(`${BASE}/sync`, null, {
          params: targetDate ? { targetDate } : undefined,
        })
        .then(unwrap),
    ),

  getPreferences: (): Promise<SafeResult<FxPreferenceDto>> =>
    safeRequest(apiClient.get<FxPreferenceDto>(`${BASE}/preferences`).then(unwrap)),

  updatePreferences: (payload: FxPreferenceDto): Promise<SafeResult<FxPreferenceDto>> =>
    safeRequest(apiClient.put<FxPreferenceDto>(`${BASE}/preferences`, payload).then(unwrap)),

  resolve: (currencyCode: string, asOfDate?: string): Promise<SafeResult<FxResolutionDto>> =>
    safeRequest(
      apiClient
        .get<FxResolutionDto>(`${BASE}/resolve/${encodeURIComponent(currencyCode)}`, {
          params: asOfDate ? { asOfDate } : undefined,
        })
        .then(unwrap),
    ),
};

export type FxSourceCode = 'TCMB' | 'ECB' | 'MANUAL' | 'TENANT_OVERRIDE';

export interface FxPreferenceDto {
  defaultSource: FxSourceCode | string;
  perCurrencyOverrides: Record<string, FxSourceCode | string>;
}

export interface FxResolutionDto {
  currencyCode: string;
  buyingRate: number;
  sellingRate: number;
  effectiveDate: string;
  source: string;
  usedTenantOverride: boolean;
}
