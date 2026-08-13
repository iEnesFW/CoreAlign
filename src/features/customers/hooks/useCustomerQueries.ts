import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { customersApi } from '../api/customersApi';
import { customerKeys } from './customerKeys';
import { patchCustomerInPaged, type PagedCustomersResponse } from '../model/customerCachePatch';
import type {
  CreateCustomerInput,
  CustomerAddressInput,
  CustomerContactInput,
  CustomerDuplicateCheckParams,
  CustomerListParams,
  UpdateCustomerAddressInput,
  UpdateCustomerContactInput,
  UpdateCustomerInput,
} from '../model/customer.types';

export const useCustomersQuery = (params: CustomerListParams, options?: { enabled?: boolean }) =>
  useQuery({
    queryKey: customerKeys.list(params),
    queryFn: () => customersApi.list(params),
    placeholderData: (previous) => previous,
    enabled: options?.enabled ?? true,
  });

/**
 * Advisory: warns that another record already carries this identity. Never blocks a save, so it is
 * deliberately kept out of the zod schema and out of the submit path.
 */
export const useCustomerDuplicateCheck = (params: CustomerDuplicateCheckParams) => {
  const hasIdentity = Boolean(params.taxNumber || params.nationalId || params.email);
  return useQuery({
    queryKey: customerKeys.duplicateCheck(params),
    queryFn: () => customersApi.duplicateCheck(params),
    enabled: hasIdentity,
    staleTime: 30 * 1000,
  });
};

export const useCustomerQuery = (id: string | null) =>
  useQuery({
    queryKey: customerKeys.detail(id),
    queryFn: () => customersApi.getById(id as string),
    enabled: id !== null,
  });

export const useCustomerSummaryQuery = (id: string | null) =>
  useQuery({
    queryKey: customerKeys.summary(id),
    queryFn: () => customersApi.getSummary(id as string),
    enabled: id !== null,
  });

export const useCustomerOverviewQuery = (id: string | null) =>
  useQuery({
    queryKey: customerKeys.overview(id),
    queryFn: () => customersApi.getOverview(id as string),
    enabled: id !== null,
  });

export const useCustomerNotesQuery = (id: string | null) =>
  useQuery({
    queryKey: customerKeys.notes(id),
    queryFn: () => customersApi.getNotes(id as string),
    enabled: id !== null,
  });

export const useAddCustomerNote = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (params: { customerId: string; body: string }) =>
      customersApi.addNote(params.customerId, params.body),
    onSuccess: (_, params) => {
      queryClient.invalidateQueries({ queryKey: customerKeys.notes(params.customerId) });
    },
  });
};

export const useCustomerAnalyticsQuery = (id: string | null, monthsBack = 12) =>
  useQuery({
    queryKey: customerKeys.analytics(id, monthsBack),
    queryFn: () => customersApi.getAnalytics(id as string, monthsBack),
    enabled: id !== null,
  });

export const useCustomerTransactionsQuery = (id: string | null) =>
  useQuery({
    queryKey: customerKeys.transactions(id),
    queryFn: () => customersApi.getTransactions(id as string),
    enabled: id !== null,
  });

export const useCreateCustomer = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: CreateCustomerInput) => customersApi.create(input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: customerKeys.lists() });
    },
  });
};

export const useUpdateCustomer = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: UpdateCustomerInput) => customersApi.update(input),
    onMutate: async (input) => {
      await queryClient.cancelQueries({ queryKey: customerKeys.lists() });
      const snapshots = queryClient.getQueriesData<PagedCustomersResponse>({
        queryKey: customerKeys.lists(),
      });
      queryClient.setQueriesData<PagedCustomersResponse>(
        { queryKey: customerKeys.lists() },
        (old) => patchCustomerInPaged(old, input),
      );
      return { snapshots };
    },
    onError: (_error, _input, context) => {
      context?.snapshots.forEach(([key, data]) => queryClient.setQueryData(key, data));
    },
    onSettled: (_data, _error, input) => {
      queryClient.invalidateQueries({ queryKey: customerKeys.lists() });
      queryClient.invalidateQueries({ queryKey: customerKeys.detail(input.id) });
      queryClient.invalidateQueries({ queryKey: customerKeys.summary(input.id) });
      queryClient.invalidateQueries({ queryKey: customerKeys.overview(input.id) });
    },
  });
};

export const useDeleteCustomer = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => customersApi.remove(id),
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: customerKeys.lists() });
      queryClient.removeQueries({ queryKey: customerKeys.detail(id) });
    },
  });
};

export const useCustomerAddressesQuery = (customerId: string | null) =>
  useQuery({
    queryKey: customerKeys.addresses(customerId),
    queryFn: () => customersApi.getAddresses(customerId as string),
    enabled: customerId !== null,
  });

export const useCreateCustomerAddress = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: CustomerAddressInput) => customersApi.createAddress(input),
    onSuccess: (_, vars) => {
      queryClient.invalidateQueries({ queryKey: customerKeys.addresses(vars.customerId) });
      queryClient.invalidateQueries({ queryKey: customerKeys.overview(vars.customerId) });
    },
  });
};

export const useUpdateCustomerAddress = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: UpdateCustomerAddressInput) => customersApi.updateAddress(input),
    onSuccess: (_, vars) => {
      queryClient.invalidateQueries({ queryKey: customerKeys.addresses(vars.customerId) });
      queryClient.invalidateQueries({ queryKey: customerKeys.overview(vars.customerId) });
    },
  });
};

export const useDeleteCustomerAddress = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ customerId, id }: { customerId: string; id: string }) =>
      customersApi.deleteAddress(customerId, id),
    onSuccess: (_, vars) => {
      queryClient.invalidateQueries({ queryKey: customerKeys.addresses(vars.customerId) });
      queryClient.invalidateQueries({ queryKey: customerKeys.overview(vars.customerId) });
    },
  });
};

export const useCustomerContactsQuery = (customerId: string | null) =>
  useQuery({
    queryKey: customerKeys.contacts(customerId),
    queryFn: () => customersApi.getContacts(customerId as string),
    enabled: customerId !== null,
  });

export const useCreateCustomerContact = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: CustomerContactInput) => customersApi.createContact(input),
    onSuccess: (_, vars) => {
      queryClient.invalidateQueries({ queryKey: customerKeys.contacts(vars.customerId) });
      queryClient.invalidateQueries({ queryKey: customerKeys.overview(vars.customerId) });
    },
  });
};

export const useUpdateCustomerContact = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: UpdateCustomerContactInput) => customersApi.updateContact(input),
    onSuccess: (_, vars) => {
      queryClient.invalidateQueries({ queryKey: customerKeys.contacts(vars.customerId) });
      queryClient.invalidateQueries({ queryKey: customerKeys.overview(vars.customerId) });
    },
  });
};

export const useDeleteCustomerContact = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ customerId, id }: { customerId: string; id: string }) =>
      customersApi.deleteContact(customerId, id),
    onSuccess: (_, vars) => {
      queryClient.invalidateQueries({ queryKey: customerKeys.contacts(vars.customerId) });
      queryClient.invalidateQueries({ queryKey: customerKeys.overview(vars.customerId) });
    },
  });
};
