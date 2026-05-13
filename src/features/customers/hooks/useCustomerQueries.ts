import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { customersApi } from '../api/customersApi';
import { customerKeys } from './customerKeys';
import type {
  CreateCustomerInput,
  CustomerAddressInput,
  CustomerContactInput,
  CustomerListParams,
  UpdateCustomerAddressInput,
  UpdateCustomerContactInput,
  UpdateCustomerInput,
} from '../model/customer.types';

export const useCustomersQuery = (params: CustomerListParams) =>
  useQuery({
    queryKey: customerKeys.list(params),
    queryFn: () => customersApi.list(params),
    placeholderData: (previous) => previous,
  });

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
    onSuccess: (_, vars) => {
      queryClient.invalidateQueries({ queryKey: customerKeys.lists() });
      queryClient.invalidateQueries({ queryKey: customerKeys.detail(vars.id) });
      queryClient.invalidateQueries({ queryKey: customerKeys.summary(vars.id) });
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
    onSuccess: (_, vars) =>
      queryClient.invalidateQueries({ queryKey: customerKeys.addresses(vars.customerId) }),
  });
};

export const useUpdateCustomerAddress = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: UpdateCustomerAddressInput) => customersApi.updateAddress(input),
    onSuccess: (_, vars) =>
      queryClient.invalidateQueries({ queryKey: customerKeys.addresses(vars.customerId) }),
  });
};

export const useDeleteCustomerAddress = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ customerId, id }: { customerId: string; id: string }) =>
      customersApi.deleteAddress(customerId, id),
    onSuccess: (_, vars) =>
      queryClient.invalidateQueries({ queryKey: customerKeys.addresses(vars.customerId) }),
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
    onSuccess: (_, vars) =>
      queryClient.invalidateQueries({ queryKey: customerKeys.contacts(vars.customerId) }),
  });
};

export const useUpdateCustomerContact = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: UpdateCustomerContactInput) => customersApi.updateContact(input),
    onSuccess: (_, vars) =>
      queryClient.invalidateQueries({ queryKey: customerKeys.contacts(vars.customerId) }),
  });
};

export const useDeleteCustomerContact = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ customerId, id }: { customerId: string; id: string }) =>
      customersApi.deleteContact(customerId, id),
    onSuccess: (_, vars) =>
      queryClient.invalidateQueries({ queryKey: customerKeys.contacts(vars.customerId) }),
  });
};
