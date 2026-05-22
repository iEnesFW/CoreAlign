import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { vendorsApi } from '../api/vendorsApi';
import type {
  CreateVendorRequest,
  Vendor,
  VendorAddress,
  VendorBankAccount,
  VendorContact,
  VendorListParams,
} from '../model/vendor.types';

type VendorUpdateBody = Omit<
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
>;

const listKey = (params: VendorListParams) => ['vendors', 'list', params] as const;
const detailKey = (id: string) => ['vendors', 'detail', id] as const;
const childrenKey = (id: string, kind: string) => ['vendors', id, kind] as const;
const ledgerKey = (
  id: string,
  params: { fromUtc?: string; toUtc?: string; page?: number; pageSize?: number },
) => ['vendors', id, 'ledger', params] as const;

const invalidate = (qc: ReturnType<typeof useQueryClient>) => {
  qc.invalidateQueries({ queryKey: ['vendors'] });
};

export const useVendorsQuery = (params: VendorListParams) =>
  useQuery({
    queryKey: listKey(params),
    queryFn: () => vendorsApi.list(params),
    staleTime: 60 * 1000,
  });

export const useVendorQuery = (id: string | undefined) =>
  useQuery({
    queryKey: detailKey(id ?? ''),
    queryFn: () => vendorsApi.getById(id as string),
    enabled: !!id,
    staleTime: 60 * 1000,
  });

export const useCreateVendor = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (request: CreateVendorRequest) => vendorsApi.create(request),
    onSuccess: () => invalidate(qc),
  });
};

export const useUpdateVendor = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, body }: { id: string; body: VendorUpdateBody }) =>
      vendorsApi.update(id, body),
    onSuccess: () => invalidate(qc),
  });
};

export const useApproveVendor = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => vendorsApi.approve(id),
    onSuccess: () => invalidate(qc),
  });
};

export const useCreateVendorAddress = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      vendorId,
      body,
    }: {
      vendorId: string;
      body: Omit<VendorAddress, 'id' | 'vendorId'>;
    }) => vendorsApi.createAddress(vendorId, { ...body, vendorId }),
    onSuccess: () => invalidate(qc),
  });
};

export const useDeleteVendorAddress = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (addressId: string) => vendorsApi.deleteAddress(addressId),
    onSuccess: () => invalidate(qc),
  });
};

export const useCreateVendorContact = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      vendorId,
      body,
    }: {
      vendorId: string;
      body: Omit<VendorContact, 'id' | 'vendorId'>;
    }) => vendorsApi.createContact(vendorId, { ...body, vendorId }),
    onSuccess: () => invalidate(qc),
  });
};

export const useDeleteVendorContact = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (contactId: string) => vendorsApi.deleteContact(contactId),
    onSuccess: () => invalidate(qc),
  });
};

export const useCreateVendorBankAccount = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      vendorId,
      body,
    }: {
      vendorId: string;
      body: Omit<VendorBankAccount, 'id' | 'vendorId'>;
    }) => vendorsApi.createBankAccount(vendorId, { ...body, vendorId }),
    onSuccess: () => invalidate(qc),
  });
};

export const useDeleteVendorBankAccount = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (accountId: string) => vendorsApi.deleteBankAccount(accountId),
    onSuccess: () => invalidate(qc),
  });
};

export const useBlockVendor = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) => vendorsApi.block(id, reason),
    onSuccess: () => invalidate(qc),
  });
};

export const useArchiveVendor = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => vendorsApi.archive(id),
    onSuccess: () => invalidate(qc),
  });
};

export const useSetVendorRating = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, rating }: { id: string; rating: number }) =>
      vendorsApi.setRating(id, rating),
    onSuccess: () => invalidate(qc),
  });
};

export const useDeleteVendor = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => vendorsApi.remove(id),
    onSuccess: () => invalidate(qc),
  });
};

export const useVendorAddressesQuery = (id: string | undefined) =>
  useQuery({
    queryKey: childrenKey(id ?? '', 'addresses'),
    queryFn: () => vendorsApi.addresses(id as string),
    enabled: !!id,
  });

export const useVendorContactsQuery = (id: string | undefined) =>
  useQuery({
    queryKey: childrenKey(id ?? '', 'contacts'),
    queryFn: () => vendorsApi.contacts(id as string),
    enabled: !!id,
  });

export const useVendorBankAccountsQuery = (id: string | undefined) =>
  useQuery({
    queryKey: childrenKey(id ?? '', 'bank-accounts'),
    queryFn: () => vendorsApi.bankAccounts(id as string),
    enabled: !!id,
  });

export const useVendorLedgerQuery = (
  id: string | undefined,
  params: { fromUtc?: string; toUtc?: string; page?: number; pageSize?: number },
) =>
  useQuery({
    queryKey: ledgerKey(id ?? '', params),
    queryFn: () => vendorsApi.ledger(id as string, params),
    enabled: !!id,
    staleTime: 60 * 1000,
  });
