import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { discountRulesApi, priceListItemsApi, taxRulesApi } from '../api/pricingRulesApi';
import type {
  DiscountRuleInput,
  DiscountRuleUpdateInput,
  PriceListItemInput,
  PriceListItemUpdateInput,
  TaxRuleInput,
  TaxRuleUpdateInput,
} from '../model/pricingRules.types';

const FIVE_MINUTES = 5 * 60 * 1000;

export const usePriceListItemsQuery = (priceListId: string | undefined) =>
  useQuery({
    queryKey: ['pricing', 'price-list-items', priceListId] as const,
    queryFn: () => priceListItemsApi.list(priceListId!),
    enabled: Boolean(priceListId),
    staleTime: FIVE_MINUTES,
  });

export const useAddPriceListItem = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: PriceListItemInput) => priceListItemsApi.add(input),
    onSuccess: (_data, vars) =>
      qc.invalidateQueries({ queryKey: ['pricing', 'price-list-items', vars.priceListId] }),
  });
};

export const useUpdatePriceListItem = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: PriceListItemUpdateInput) => priceListItemsApi.update(input),
    onSuccess: (_data, vars) =>
      qc.invalidateQueries({ queryKey: ['pricing', 'price-list-items', vars.priceListId] }),
  });
};

export const useRemovePriceListItem = (priceListId: string | undefined) => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => priceListItemsApi.remove(priceListId!, id),
    onSuccess: () =>
      qc.invalidateQueries({ queryKey: ['pricing', 'price-list-items', priceListId] }),
  });
};

export const useDiscountRulesQuery = (isActive?: boolean) =>
  useQuery({
    queryKey: ['pricing', 'discount-rules', { isActive }] as const,
    queryFn: () => discountRulesApi.list(isActive),
    staleTime: FIVE_MINUTES,
  });

export const useCreateDiscountRule = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: DiscountRuleInput) => discountRulesApi.create(input),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['pricing', 'discount-rules'] }),
  });
};

export const useUpdateDiscountRule = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: DiscountRuleUpdateInput) => discountRulesApi.update(input),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['pricing', 'discount-rules'] }),
  });
};

export const useDeleteDiscountRule = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => discountRulesApi.remove(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['pricing', 'discount-rules'] }),
  });
};

export const useTaxRulesQuery = (isActive?: boolean) =>
  useQuery({
    queryKey: ['pricing', 'tax-rules', { isActive }] as const,
    queryFn: () => taxRulesApi.list(isActive),
    staleTime: FIVE_MINUTES,
  });

export const useCreateTaxRule = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: TaxRuleInput) => taxRulesApi.create(input),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['pricing', 'tax-rules'] }),
  });
};

export const useUpdateTaxRule = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: TaxRuleUpdateInput) => taxRulesApi.update(input),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['pricing', 'tax-rules'] }),
  });
};

export const useDeleteTaxRule = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => taxRulesApi.remove(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['pricing', 'tax-rules'] }),
  });
};
