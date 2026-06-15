import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { vendorBillingApi } from '../api/vendorBillingApi';
import type {
  ApplyVendorPaymentInput,
  CreateVendorBillInput,
  CreateVendorPaymentInput,
  UpdateVendorBillInput,
  UpdateVendorPaymentInput,
  VendorBillListParams,
} from '../model/vendorBilling.types';

export const useVendorBillsQuery = (params: VendorBillListParams) =>
  useQuery({
    queryKey: ['vendor-bills', 'list', params] as const,
    queryFn: () => vendorBillingApi.searchBills(params),
    staleTime: 30 * 1000,
  });

export const useVendorAgingQuery = (asOf?: string) =>
  useQuery({
    queryKey: ['vendor-bills', 'aging', asOf ?? 'now'] as const,
    queryFn: () => vendorBillingApi.aging(asOf),
    staleTime: 60 * 1000,
  });

export const useVendorPaymentsQuery = (params: {
  vendorId?: string;
  page?: number;
  pageSize?: number;
}) =>
  useQuery({
    queryKey: ['vendor-payments', 'list', params] as const,
    queryFn: () => vendorBillingApi.searchPayments(params),
    staleTime: 30 * 1000,
  });

const invalidate = (qc: ReturnType<typeof useQueryClient>) => {
  qc.invalidateQueries({ queryKey: ['vendor-bills'] });
  qc.invalidateQueries({ queryKey: ['vendor-payments'] });
  qc.invalidateQueries({ queryKey: ['vendors'] });
};

export const useCreateVendorBill = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: CreateVendorBillInput) => vendorBillingApi.createBill(input),
    onSuccess: () => invalidate(qc),
  });
};

export const useVendorBillAction = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, action }: { id: string; action: 'post' | 'cancel' }) =>
      action === 'post' ? vendorBillingApi.postBill(id) : vendorBillingApi.cancelBill(id),
    onSuccess: () => invalidate(qc),
  });
};

export const useCreateVendorPayment = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: CreateVendorPaymentInput) => vendorBillingApi.createPayment(input),
    onSuccess: () => invalidate(qc),
  });
};

export const useUpdateVendorBill = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: UpdateVendorBillInput) => vendorBillingApi.updateBill(input),
    onSuccess: () => invalidate(qc),
  });
};

export const useUpdateVendorPayment = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: UpdateVendorPaymentInput) => vendorBillingApi.updatePayment(input),
    onSuccess: () => invalidate(qc),
  });
};

export const useVoidVendorPayment = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, reason }: { id: string; reason?: string }) =>
      vendorBillingApi.voidPayment(id, reason),
    onSuccess: () => invalidate(qc),
  });
};

export const useApplyVendorPayment = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: ApplyVendorPaymentInput) => vendorBillingApi.applyPayment(input),
    onSuccess: () => invalidate(qc),
  });
};

export const useVendorBillApplicationsQuery = (billId: string | undefined) =>
  useQuery({
    queryKey: ['vendor-bills', 'applications', billId] as const,
    queryFn: () => vendorBillingApi.getBillApplications(billId!),
    enabled: Boolean(billId),
    staleTime: 30 * 1000,
  });

export const useThreeWayMatchQuery = (params: {
  vendorId?: string;
  fromUtc?: string;
  toUtc?: string;
}) =>
  useQuery({
    queryKey: ['vendor-bills', 'three-way-match', params] as const,
    queryFn: () => vendorBillingApi.threeWayMatch(params),
    staleTime: 60 * 1000,
  });
