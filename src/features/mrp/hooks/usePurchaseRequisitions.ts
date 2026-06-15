import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { mrpApi } from '../api/mrpApi';
import type {
  ConvertRequisitionInput,
  CreatePurchaseRequisitionInput,
  RequisitionListParams,
} from '../model/mrp.types';

export const usePurchaseRequisitionsQuery = (params: RequisitionListParams) =>
  useQuery({
    queryKey: ['purchase-requisitions', 'list', params] as const,
    queryFn: () => mrpApi.listRequisitions(params),
    staleTime: 30 * 1000,
  });

const invalidate = (qc: ReturnType<typeof useQueryClient>) => {
  qc.invalidateQueries({ queryKey: ['purchase-requisitions'] });
  qc.invalidateQueries({ queryKey: ['mrp'] });
};

export const useCreatePurchaseRequisition = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: CreatePurchaseRequisitionInput) => mrpApi.createRequisition(input),
    onSuccess: () => invalidate(qc),
  });
};

export const useSubmitPurchaseRequisition = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => mrpApi.submitRequisition(id),
    onSuccess: () => invalidate(qc),
  });
};

export const useApprovePurchaseRequisition = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => mrpApi.approveRequisition(id),
    onSuccess: () => invalidate(qc),
  });
};

export const useRejectPurchaseRequisition = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, reason }: { id: string; reason?: string | null }) =>
      mrpApi.rejectRequisition(id, reason),
    onSuccess: () => invalidate(qc),
  });
};

export const useCancelPurchaseRequisition = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, reason }: { id: string; reason?: string | null }) =>
      mrpApi.cancelRequisition(id, reason),
    onSuccess: () => invalidate(qc),
  });
};

export const useConvertRequisition = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: ConvertRequisitionInput) => mrpApi.convertRequisition(input),
    onSuccess: () => {
      invalidate(qc);
      qc.invalidateQueries({ queryKey: ['purchase-orders'] });
    },
  });
};
