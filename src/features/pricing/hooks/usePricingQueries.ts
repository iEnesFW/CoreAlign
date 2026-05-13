import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { pricingApi } from '../api/pricingApi';
import type { CreateCustomerProductPriceInput, CustomerProductPrice } from '../model/pricing.types';

const FIVE_MINUTES = 5 * 60 * 1000;

export const useResolvePrice = (
  productId: string | null,
  customerId: string | null,
  quantity = 1,
  enabled = true,
) =>
  useQuery({
    queryKey: ['pricing', 'resolve', productId, customerId, quantity] as const,
    queryFn: () => pricingApi.resolvePrice(productId as string, customerId as string, quantity),
    enabled: enabled && productId !== null && customerId !== null,
    staleTime: 30 * 1000,
  });

export const useCustomerProductPrices = (customerId?: string, productId?: string) =>
  useQuery({
    queryKey: ['pricing', 'customer-product-prices', customerId, productId] as const,
    queryFn: () => pricingApi.listCustomerProductPrices(customerId, productId),
    enabled: !!customerId || !!productId,
    staleTime: FIVE_MINUTES,
  });

export const useAccountingPeriods = (year?: number) =>
  useQuery({
    queryKey: ['accounting', 'periods', year] as const,
    queryFn: () => pricingApi.listAccountingPeriods(year),
    staleTime: FIVE_MINUTES,
  });

const invalidate = (qc: ReturnType<typeof useQueryClient>) => {
  qc.invalidateQueries({ queryKey: ['pricing'] });
  qc.invalidateQueries({ queryKey: ['accounting'] });
};

export const useCreateCustomerProductPrice = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: CreateCustomerProductPriceInput) =>
      pricingApi.createCustomerProductPrice(input),
    onSuccess: () => invalidate(qc),
  });
};

export const useUpdateCustomerProductPrice = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: CustomerProductPrice) => pricingApi.updateCustomerProductPrice(input),
    onSuccess: () => invalidate(qc),
  });
};

export const useDeleteCustomerProductPrice = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => pricingApi.deleteCustomerProductPrice(id),
    onSuccess: () => invalidate(qc),
  });
};

export const useCreateAccountingPeriod = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ year, month }: { year: number; month: number }) =>
      pricingApi.createAccountingPeriod(year, month),
    onSuccess: () => invalidate(qc),
  });
};

export const useClosePeriod = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, notes }: { id: string; notes?: string | null }) =>
      pricingApi.closePeriod(id, notes),
    onSuccess: () => invalidate(qc),
  });
};

export const useReopenPeriod = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => pricingApi.reopenPeriod(id),
    onSuccess: () => invalidate(qc),
  });
};

export const useLockPeriod = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => pricingApi.lockPeriod(id),
    onSuccess: () => invalidate(qc),
  });
};
