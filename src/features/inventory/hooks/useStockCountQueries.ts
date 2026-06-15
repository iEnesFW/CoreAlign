import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { stockCountsApi } from '../api/stockCountsApi';
import type {
  PlanStockCountInput,
  RecordCountInput,
  ReconcileStockCountInput,
  StockCountListParams,
} from '../model/stockCount.types';

export const useStockCountsQuery = (params: StockCountListParams) =>
  useQuery({
    queryKey: ['stock-counts', 'list', params] as const,
    queryFn: () => stockCountsApi.list(params),
    staleTime: 30 * 1000,
  });

export const useStockCountQuery = (id: string | undefined) =>
  useQuery({
    queryKey: ['stock-counts', 'detail', id] as const,
    queryFn: () => stockCountsApi.getById(id!),
    enabled: Boolean(id),
    staleTime: 15 * 1000,
  });

const invalidate = (qc: ReturnType<typeof useQueryClient>) => {
  qc.invalidateQueries({ queryKey: ['stock-counts'] });
  qc.invalidateQueries({ queryKey: ['stock-items'] });
  qc.invalidateQueries({ queryKey: ['stock-movements'] });
};

export const usePlanStockCount = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: PlanStockCountInput) => stockCountsApi.plan(input),
    onSuccess: () => invalidate(qc),
  });
};

export const useStartStockCount = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => stockCountsApi.start(id),
    onSuccess: () => invalidate(qc),
  });
};

export const useRecordStockCount = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: RecordCountInput) => stockCountsApi.record(input),
    onSuccess: () => invalidate(qc),
  });
};

export const useReconcileStockCount = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: ReconcileStockCountInput) => stockCountsApi.reconcile(input),
    onSuccess: () => invalidate(qc),
  });
};

export const usePostStockCount = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => stockCountsApi.post(id),
    onSuccess: () => invalidate(qc),
  });
};

export const useCancelStockCount = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => stockCountsApi.cancel(id),
    onSuccess: () => invalidate(qc),
  });
};
