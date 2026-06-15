import { useQuery, type UseQueryOptions } from '@tanstack/react-query';
import { dealerApi } from './api';
import type {
  CommissionEntry,
  CommissionStatus,
  CommissionSummary,
  CreditSnapshot,
  DealerAllowedCustomer,
  DealerPortalDashboard,
  DealerProfile,
  InvoiceDetail,
  InvoiceSummary,
  OrderDetail,
  OrderSummary,
  PagedResult,
  ProductSummary,
} from './types';

export const dealerKeys = {
  dashboard: ['dealer', 'dashboard'] as const,
  customers: ['dealer', 'customers'] as const,
  orders: (status?: string, approvalStatus?: string, page = 1, pageSize = 20) =>
    [
      'dealer',
      'orders',
      { status: status ?? null, approvalStatus: approvalStatus ?? null, page, pageSize },
    ] as const,
  order: (id: string) => ['dealer', 'order', id] as const,
  catalog: (search?: string, customerId?: string, page = 1, pageSize = 20) =>
    [
      'dealer',
      'catalog',
      { search: search ?? '', customerId: customerId ?? null, page, pageSize },
    ] as const,
  invoices: (
    customerId?: string,
    status?: string,
    fromUtc?: string,
    toUtc?: string,
    page = 1,
    pageSize = 20,
  ) =>
    [
      'dealer',
      'invoices',
      {
        customerId: customerId ?? null,
        status: status ?? null,
        fromUtc: fromUtc ?? null,
        toUtc: toUtc ?? null,
        page,
        pageSize,
      },
    ] as const,
  invoice: (id: string) => ['dealer', 'invoice', id] as const,
  commissions: (
    status?: CommissionStatus,
    fromUtc?: string,
    toUtc?: string,
    page = 1,
    pageSize = 20,
  ) =>
    [
      'dealer',
      'commissions',
      { status: status ?? null, fromUtc: fromUtc ?? null, toUtc: toUtc ?? null, page, pageSize },
    ] as const,
  commissionSummary: ['dealer', 'commissions', 'summary'] as const,
  profile: ['dealer', 'profile'] as const,
  customerCredit: (customerId: string) => ['dealer', 'customers', customerId, 'credit'] as const,
};

export const useDealerDashboard = (
  options?: Omit<UseQueryOptions<DealerPortalDashboard>, 'queryKey' | 'queryFn'>,
) =>
  useQuery({
    queryKey: dealerKeys.dashboard,
    queryFn: () => dealerApi.getDashboard(),
    staleTime: 30_000,
    ...options,
  });

export const useDealerCustomers = (
  options?: Omit<UseQueryOptions<DealerAllowedCustomer[]>, 'queryKey' | 'queryFn'>,
) =>
  useQuery({
    queryKey: dealerKeys.customers,
    queryFn: () => dealerApi.getAllowedCustomers(),
    staleTime: 60_000,
    ...options,
  });

export const useDealerOrders = (
  params: { status?: string; approvalStatus?: string; page?: number; pageSize?: number },
  options?: Omit<UseQueryOptions<PagedResult<OrderSummary>>, 'queryKey' | 'queryFn'>,
) =>
  useQuery({
    queryKey: dealerKeys.orders(
      params.status,
      params.approvalStatus,
      params.page ?? 1,
      params.pageSize ?? 20,
    ),
    queryFn: () => dealerApi.getOrders(params),
    staleTime: 15_000,
    ...options,
  });

export const useDealerOrder = (
  id: string | undefined,
  options?: Omit<UseQueryOptions<OrderDetail>, 'queryKey' | 'queryFn' | 'enabled'>,
) =>
  useQuery({
    queryKey: dealerKeys.order(id ?? ''),
    queryFn: () => dealerApi.getOrderById(id!),
    enabled: !!id,
    staleTime: 30_000,
    ...options,
  });

export const useCatalogProducts = (
  params: { search?: string; customerId?: string; page?: number; pageSize?: number },
  options?: Omit<UseQueryOptions<PagedResult<ProductSummary>>, 'queryKey' | 'queryFn'>,
) =>
  useQuery({
    queryKey: dealerKeys.catalog(
      params.search,
      params.customerId,
      params.page ?? 1,
      params.pageSize ?? 20,
    ),
    queryFn: () => dealerApi.getCatalogProducts(params),
    staleTime: 15_000,
    ...options,
  });

export const useDealerInvoices = (
  params: {
    customerId?: string;
    status?: string;
    fromUtc?: string;
    toUtc?: string;
    page?: number;
    pageSize?: number;
  },
  options?: Omit<UseQueryOptions<PagedResult<InvoiceSummary>>, 'queryKey' | 'queryFn'>,
) =>
  useQuery({
    queryKey: dealerKeys.invoices(
      params.customerId,
      params.status,
      params.fromUtc,
      params.toUtc,
      params.page ?? 1,
      params.pageSize ?? 20,
    ),
    queryFn: () => dealerApi.getInvoices(params),
    staleTime: 15_000,
    ...options,
  });

export const useDealerInvoice = (
  id: string | undefined,
  options?: Omit<UseQueryOptions<InvoiceDetail>, 'queryKey' | 'queryFn' | 'enabled'>,
) =>
  useQuery({
    queryKey: dealerKeys.invoice(id ?? ''),
    queryFn: () => dealerApi.getInvoiceById(id!),
    enabled: !!id,
    staleTime: 30_000,
    ...options,
  });

export const useDealerCommissions = (
  params: {
    status?: CommissionStatus;
    fromUtc?: string;
    toUtc?: string;
    page?: number;
    pageSize?: number;
  },
  options?: Omit<UseQueryOptions<PagedResult<CommissionEntry>>, 'queryKey' | 'queryFn'>,
) =>
  useQuery({
    queryKey: dealerKeys.commissions(
      params.status,
      params.fromUtc,
      params.toUtc,
      params.page ?? 1,
      params.pageSize ?? 20,
    ),
    queryFn: () => dealerApi.getCommissions(params),
    staleTime: 15_000,
    ...options,
  });

export const useDealerCommissionSummary = (
  options?: Omit<UseQueryOptions<CommissionSummary>, 'queryKey' | 'queryFn'>,
) =>
  useQuery({
    queryKey: dealerKeys.commissionSummary,
    queryFn: () => dealerApi.getCommissionSummary(),
    staleTime: 60_000,
    ...options,
  });

export const useDealerProfile = (
  options?: Omit<UseQueryOptions<DealerProfile>, 'queryKey' | 'queryFn'>,
) =>
  useQuery({
    queryKey: dealerKeys.profile,
    queryFn: () => dealerApi.getProfile(),
    staleTime: 60_000,
    ...options,
  });

export const useDealerCustomerCredit = (
  customerId: string | null,
  options?: Omit<UseQueryOptions<CreditSnapshot>, 'queryKey' | 'queryFn' | 'enabled'>,
) =>
  useQuery({
    queryKey: dealerKeys.customerCredit(customerId ?? ''),
    queryFn: () => dealerApi.getCustomerCredit(customerId!),
    enabled: !!customerId,
    staleTime: 15_000,
    ...options,
  });
