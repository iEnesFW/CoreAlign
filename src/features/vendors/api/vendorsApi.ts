import { apiClient } from '@/shared/api/apiClient';
import type { ApiResponse, PagedResult } from '@/shared/types/api';
import type {
  CreateVendorRequest,
  Vendor,
  VendorAddress,
  VendorBankAccount,
  VendorContact,
  VendorLedgerEntry,
  VendorListParams,
  VendorSummary,
} from '../model/vendor.types';

const BASE = '/vendors';

export const vendorsApi = {
  list: (params: VendorListParams) =>
    apiClient.get<ApiResponse<PagedResult<VendorSummary>>>(BASE, { params }).then((r) => r.data),

  getById: (id: string) => apiClient.get<ApiResponse<Vendor>>(`${BASE}/${id}`).then((r) => r.data),

  create: (request: CreateVendorRequest) =>
    apiClient.post<ApiResponse<Vendor>>(BASE, request).then((r) => r.data),

  update: (
    id: string,
    request: Omit<
      Vendor,
      | 'createdAtUtc'
      | 'updatedAtUtc'
      | 'status'
      | 'currentBalance'
      | 'overdueAmount'
      | 'totalPayable'
      | 'approvedAtUtc'
      | 'blockReason'
      | 'paymentTermsName'
      | 'rating'
    >,
  ) => apiClient.put<ApiResponse<Vendor>>(`${BASE}/${id}`, request).then((r) => r.data),

  approve: (id: string) =>
    apiClient.post<ApiResponse<Vendor>>(`${BASE}/${id}/approve`, {}).then((r) => r.data),

  block: (id: string, reason: string) =>
    apiClient.post<ApiResponse<Vendor>>(`${BASE}/${id}/block`, { id, reason }).then((r) => r.data),

  archive: (id: string) =>
    apiClient.post<ApiResponse<Vendor>>(`${BASE}/${id}/archive`, {}).then((r) => r.data),

  setRating: (id: string, rating: number) =>
    apiClient.post<ApiResponse<Vendor>>(`${BASE}/${id}/rating`, { id, rating }).then((r) => r.data),

  remove: (id: string) =>
    apiClient.delete<ApiResponse<boolean>>(`${BASE}/${id}`).then((r) => r.data),

  addresses: (id: string) =>
    apiClient.get<ApiResponse<VendorAddress[]>>(`${BASE}/${id}/addresses`).then((r) => r.data),

  createAddress: (
    id: string,
    body: Omit<VendorAddress, 'id' | 'vendorId'> & { vendorId: string },
  ) =>
    apiClient.post<ApiResponse<VendorAddress>>(`${BASE}/${id}/addresses`, body).then((r) => r.data),

  deleteAddress: (addressId: string) =>
    apiClient.delete<ApiResponse<boolean>>(`${BASE}/addresses/${addressId}`).then((r) => r.data),

  contacts: (id: string) =>
    apiClient.get<ApiResponse<VendorContact[]>>(`${BASE}/${id}/contacts`).then((r) => r.data),

  createContact: (
    id: string,
    body: Omit<VendorContact, 'id' | 'vendorId'> & { vendorId: string },
  ) =>
    apiClient.post<ApiResponse<VendorContact>>(`${BASE}/${id}/contacts`, body).then((r) => r.data),

  deleteContact: (contactId: string) =>
    apiClient.delete<ApiResponse<boolean>>(`${BASE}/contacts/${contactId}`).then((r) => r.data),

  bankAccounts: (id: string) =>
    apiClient
      .get<ApiResponse<VendorBankAccount[]>>(`${BASE}/${id}/bank-accounts`)
      .then((r) => r.data),

  createBankAccount: (
    id: string,
    body: Omit<VendorBankAccount, 'id' | 'vendorId'> & { vendorId: string },
  ) =>
    apiClient
      .post<ApiResponse<VendorBankAccount>>(`${BASE}/${id}/bank-accounts`, body)
      .then((r) => r.data),

  deleteBankAccount: (accountId: string) =>
    apiClient
      .delete<ApiResponse<boolean>>(`${BASE}/bank-accounts/${accountId}`)
      .then((r) => r.data),

  ledger: (
    id: string,
    params: { fromUtc?: string; toUtc?: string; page?: number; pageSize?: number },
  ) =>
    apiClient
      .get<ApiResponse<PagedResult<VendorLedgerEntry>>>(`${BASE}/${id}/ledger`, { params })
      .then((r) => r.data),
};
