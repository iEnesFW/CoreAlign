import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { billingApi, type ListOrdersParams } from '../api/billingApi';
import type { CreateSubscriptionOrderInput, MockApproveInput } from '../model/billing.types';

const FIVE_MINUTES = 5 * 60 * 1000;
const SIXTY_SECONDS = 60 * 1000;

export const billingKeys = {
  all: ['billing'] as const,
  catalog: () => [...billingKeys.all, 'catalog'] as const,
  gateways: () => [...billingKeys.all, 'gateways'] as const,
  activeModules: () => [...billingKeys.all, 'active-modules'] as const,
  orders: (params: ListOrdersParams) => [...billingKeys.all, 'orders', params] as const,
  order: (id: string) => [...billingKeys.all, 'order', id] as const,
};

const invalidateAllBilling = (qc: ReturnType<typeof useQueryClient>) => {
  qc.invalidateQueries({ queryKey: billingKeys.all });
};

export const useModulesCatalogQuery = () =>
  useQuery({
    queryKey: billingKeys.catalog(),
    queryFn: () => billingApi.listCatalog(),
    staleTime: FIVE_MINUTES,
  });

export const usePaymentGatewaysQuery = () =>
  useQuery({
    queryKey: billingKeys.gateways(),
    queryFn: () => billingApi.listGateways(),
    staleTime: FIVE_MINUTES,
  });

export const useActiveModulesQuery = (options: { enabled?: boolean } = {}) =>
  useQuery({
    queryKey: billingKeys.activeModules(),
    queryFn: () => billingApi.listActiveModules(),
    staleTime: SIXTY_SECONDS,
    enabled: options.enabled ?? true,
  });

export const useSubscriptionOrdersQuery = (params: ListOrdersParams = {}) =>
  useQuery({
    queryKey: billingKeys.orders(params),
    queryFn: () => billingApi.listOrders(params),
    staleTime: 30 * 1000,
  });

export const useSubscriptionOrderQuery = (id: string | null | undefined) =>
  useQuery({
    queryKey: billingKeys.order(id ?? ''),
    queryFn: () => billingApi.getOrder(id as string),
    enabled: !!id,
    staleTime: 15 * 1000,
  });

export const useCreateSubscriptionOrder = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: CreateSubscriptionOrderInput) => billingApi.createOrder(input),
    onSuccess: () => invalidateAllBilling(qc),
  });
};

export const useCancelSubscriptionOrder = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, reason }: { id: string; reason?: string }) =>
      billingApi.cancelOrder(id, reason),
    onSuccess: () => invalidateAllBilling(qc),
  });
};

export const useMockApprove = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: MockApproveInput) => billingApi.mockApprove(input),
    onSuccess: () => invalidateAllBilling(qc),
  });
};
