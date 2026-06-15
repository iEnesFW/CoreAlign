import { useMutation, useQuery, useQueryClient, type UseQueryOptions } from '@tanstack/react-query';
import { portalApi, type CreateCustomerDirectOrderInput } from './api';
import type {
  CatalogProduct,
  CreditSnapshot,
  CustomerPortalDashboard,
  DealerAccount,
  InvoiceDetail,
  InvoiceSummary,
  OrderDetail,
  OrderSummary,
  PagedResult,
  PortalAddress,
  PortalAddressInput,
} from './types';

export const portalKeys = {
  dashboard: ['portal', 'dashboard'] as const,
  orders: (status?: string, page = 1, pageSize = 20) =>
    ['portal', 'orders', { status: status ?? null, page, pageSize }] as const,
  order: (id: string) => ['portal', 'order', id] as const,
  invoices: (status?: string, page = 1, pageSize = 20) =>
    ['portal', 'invoices', { status: status ?? null, page, pageSize }] as const,
  invoice: (id: string) => ['portal', 'invoice', id] as const,
  dealers: ['portal', 'dealers'] as const,
  catalog: (search?: string, page = 1, pageSize = 20) =>
    ['portal', 'catalog', { search: search ?? '', page, pageSize }] as const,
  credit: ['portal', 'credit'] as const,
  addresses: ['portal', 'addresses'] as const,
};

export const useDashboard = (
  options?: Omit<UseQueryOptions<CustomerPortalDashboard>, 'queryKey' | 'queryFn'>,
) =>
  useQuery({
    queryKey: portalKeys.dashboard,
    queryFn: () => portalApi.getDashboard(),
    staleTime: 30_000,
    ...options,
  });

export const usePortalOrders = (
  params: { status?: string; page?: number; pageSize?: number },
  options?: Omit<UseQueryOptions<PagedResult<OrderSummary>>, 'queryKey' | 'queryFn'>,
) =>
  useQuery({
    queryKey: portalKeys.orders(params.status, params.page ?? 1, params.pageSize ?? 20),
    queryFn: () => portalApi.getOrders(params),
    staleTime: 15_000,
    ...options,
  });

export const usePortalOrder = (
  id: string | undefined,
  options?: Omit<UseQueryOptions<OrderDetail>, 'queryKey' | 'queryFn' | 'enabled'>,
) =>
  useQuery({
    queryKey: portalKeys.order(id ?? ''),
    queryFn: () => portalApi.getOrderById(id!),
    enabled: !!id,
    staleTime: 30_000,
    ...options,
  });

export const usePortalInvoices = (
  params: { status?: string; page?: number; pageSize?: number },
  options?: Omit<UseQueryOptions<PagedResult<InvoiceSummary>>, 'queryKey' | 'queryFn'>,
) =>
  useQuery({
    queryKey: portalKeys.invoices(params.status, params.page ?? 1, params.pageSize ?? 20),
    queryFn: () => portalApi.getInvoices(params),
    staleTime: 15_000,
    ...options,
  });

export const usePortalInvoice = (
  id: string | undefined,
  options?: Omit<UseQueryOptions<InvoiceDetail>, 'queryKey' | 'queryFn' | 'enabled'>,
) =>
  useQuery({
    queryKey: portalKeys.invoice(id ?? ''),
    queryFn: () => portalApi.getInvoiceById(id!),
    enabled: !!id,
    staleTime: 30_000,
    ...options,
  });

export const usePortalDealers = (
  options?: Omit<UseQueryOptions<DealerAccount[]>, 'queryKey' | 'queryFn'>,
) =>
  useQuery({
    queryKey: portalKeys.dealers,
    queryFn: () => portalApi.getDealers(),
    staleTime: 60_000,
    ...options,
  });

export const useCatalogProducts = (
  params: { search?: string; page?: number; pageSize?: number },
  options?: Omit<UseQueryOptions<PagedResult<CatalogProduct>>, 'queryKey' | 'queryFn'>,
) =>
  useQuery({
    queryKey: portalKeys.catalog(params.search, params.page ?? 1, params.pageSize ?? 20),
    queryFn: () => portalApi.getCatalogProducts(params),
    staleTime: 15_000,
    ...options,
  });

export const useCreateDirectOrder = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: CreateCustomerDirectOrderInput) => portalApi.createDirectOrder(input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['portal', 'orders'] });
      queryClient.invalidateQueries({ queryKey: portalKeys.dashboard });
      queryClient.invalidateQueries({ queryKey: portalKeys.credit });
    },
  });
};

export const useCreditSnapshot = (
  options?: Omit<UseQueryOptions<CreditSnapshot>, 'queryKey' | 'queryFn'>,
) =>
  useQuery({
    queryKey: portalKeys.credit,
    queryFn: () => portalApi.getCredit(),
    staleTime: 15_000,
    ...options,
  });

export const usePortalAddresses = (
  options?: Omit<UseQueryOptions<PortalAddress[]>, 'queryKey' | 'queryFn'>,
) =>
  useQuery({
    queryKey: portalKeys.addresses,
    queryFn: () => portalApi.listAddresses(),
    staleTime: 30_000,
    ...options,
  });

export const useCreatePortalAddress = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: PortalAddressInput) => portalApi.createAddress(input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: portalKeys.addresses });
    },
  });
};

export const useUpdatePortalAddress = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, input }: { id: string; input: PortalAddressInput }) =>
      portalApi.updateAddress(id, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: portalKeys.addresses });
    },
  });
};

export const useDeletePortalAddress = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => portalApi.deleteAddress(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: portalKeys.addresses });
    },
  });
};
