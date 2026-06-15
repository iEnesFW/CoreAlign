import { apiClient } from '@/shared/api/apiClient';
import { cachedGet, invalidateHttpCache } from '@/shared/http/httpCache';
import type { ApiResponse, PagedResult } from '@/shared/types/api';
import type {
  ApplyVendorPaymentInput,
  CreateVendorBillInput,
  CreateVendorPaymentInput,
  ThreeWayMatchRow,
  UpdateVendorBillInput,
  UpdateVendorPaymentInput,
  VendorAgingRow,
  VendorBill,
  VendorBillListParams,
  VendorPayment,
  VendorPaymentApplication,
} from '../model/vendorBilling.types';

const BILLS = '/vendor-bills';
const PAYMENTS = '/vendor-payments';
const INVALIDATION = [/\/vendor-bills/i, /\/vendor-payments/i, /\/vendors/i] as const;

const mutate = <T>(p: Promise<{ data: ApiResponse<T> }>) =>
  p.then((r) => {
    invalidateHttpCache(INVALIDATION);
    return r.data;
  });

export const vendorBillingApi = {
  searchBills: (params: VendorBillListParams) =>
    cachedGet<ApiResponse<PagedResult<VendorBill>>>(apiClient, BILLS, { params }),

  getBill: (id: string) => cachedGet<ApiResponse<VendorBill>>(apiClient, `${BILLS}/${id}`),

  getBillApplications: (id: string) =>
    cachedGet<ApiResponse<VendorPaymentApplication[]>>(apiClient, `${BILLS}/${id}/applications`),

  createBill: (input: CreateVendorBillInput) =>
    mutate(apiClient.post<ApiResponse<VendorBill>>(BILLS, input)),

  updateBill: (input: UpdateVendorBillInput) =>
    mutate(apiClient.put<ApiResponse<VendorBill>>(`${BILLS}/${input.id}`, input)),

  postBill: (id: string) => mutate(apiClient.post<ApiResponse<VendorBill>>(`${BILLS}/${id}/post`)),

  cancelBill: (id: string) =>
    mutate(apiClient.post<ApiResponse<VendorBill>>(`${BILLS}/${id}/cancel`)),

  aging: (asOf?: string) =>
    cachedGet<ApiResponse<VendorAgingRow[]>>(apiClient, `${BILLS}/aging`, {
      params: asOf ? { asOf } : undefined,
    }),

  threeWayMatch: (params: { vendorId?: string; fromUtc?: string; toUtc?: string }) =>
    cachedGet<ApiResponse<ThreeWayMatchRow[]>>(apiClient, `${BILLS}/three-way-match`, { params }),

  searchPayments: (params: { vendorId?: string; page?: number; pageSize?: number }) =>
    cachedGet<ApiResponse<PagedResult<VendorPayment>>>(apiClient, PAYMENTS, { params }),

  getPayment: (id: string) => cachedGet<ApiResponse<VendorPayment>>(apiClient, `${PAYMENTS}/${id}`),

  getPaymentApplications: (id: string) =>
    cachedGet<ApiResponse<VendorPaymentApplication[]>>(apiClient, `${PAYMENTS}/${id}/applications`),

  createPayment: (input: CreateVendorPaymentInput) =>
    mutate(apiClient.post<ApiResponse<VendorPayment>>(PAYMENTS, input)),

  updatePayment: (input: UpdateVendorPaymentInput) =>
    mutate(apiClient.put<ApiResponse<VendorPayment>>(`${PAYMENTS}/${input.id}`, input)),

  voidPayment: (id: string, reason?: string) =>
    mutate(apiClient.post<ApiResponse<VendorPayment>>(`${PAYMENTS}/${id}/void`, { id, reason })),

  applyPayment: (input: ApplyVendorPaymentInput) =>
    mutate(apiClient.post<ApiResponse<VendorPaymentApplication>>(`${PAYMENTS}/apply`, input)),
};
