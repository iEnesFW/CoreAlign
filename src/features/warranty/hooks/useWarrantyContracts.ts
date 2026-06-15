import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { warrantyApi, type WarrantyListParams } from '../api/warrantyApi';
import type {
  CancelWarrantyContractInput,
  CreateWarrantyContractInput,
  ExtendWarrantyContractInput,
} from '../model/warranty.types';

export const useWarrantyContractsQuery = (params: WarrantyListParams) =>
  useQuery({
    queryKey: ['warranty-contracts', 'list', params] as const,
    queryFn: () => warrantyApi.list(params),
    staleTime: 60 * 1000,
  });

export const useWarrantyContractQuery = (id: string | undefined) =>
  useQuery({
    queryKey: ['warranty-contracts', 'detail', id] as const,
    queryFn: () => warrantyApi.getById(id!),
    enabled: Boolean(id),
    staleTime: 60 * 1000,
  });

export const useExpiringWarrantiesQuery = (withinDays = 30) =>
  useQuery({
    queryKey: ['warranty-contracts', 'expiring', withinDays] as const,
    queryFn: () => warrantyApi.listExpiring(withinDays),
    staleTime: 5 * 60 * 1000,
  });

export const useCreateWarrantyContract = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: CreateWarrantyContractInput) => warrantyApi.create(input),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['warranty-contracts'] }),
  });
};

export const useExtendWarrantyContract = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: ExtendWarrantyContractInput) => warrantyApi.extend(input),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['warranty-contracts'] }),
  });
};

export const useCancelWarrantyContract = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: CancelWarrantyContractInput) => warrantyApi.cancel(input),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['warranty-contracts'] }),
  });
};
