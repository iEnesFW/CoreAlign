import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { purchaseOrdersApi } from '../api/purchaseOrdersApi';
import type {
  CreatePurchaseOrderInput,
  PurchaseOrderListParams,
  ReceivePurchaseOrderInput,
  UpdatePurchaseOrderInput,
} from '../model/purchaseOrder.types';

export const usePurchaseOrdersQuery = (params: PurchaseOrderListParams) =>
  useQuery({
    queryKey: ['purchase-orders', 'list', params] as const,
    queryFn: () => purchaseOrdersApi.search(params),
    staleTime: 30 * 1000,
  });

export const usePurchaseOrderQuery = (id: string | null) =>
  useQuery({
    queryKey: ['purchase-orders', 'detail', id] as const,
    queryFn: () => purchaseOrdersApi.getById(id as string),
    enabled: id !== null,
  });

const invalidate = (qc: ReturnType<typeof useQueryClient>) =>
  qc.invalidateQueries({ queryKey: ['purchase-orders'] });

export const useCreatePurchaseOrder = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: CreatePurchaseOrderInput) => purchaseOrdersApi.create(input),
    onSuccess: () => invalidate(qc),
  });
};

export const useUpdatePurchaseOrder = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: UpdatePurchaseOrderInput) => purchaseOrdersApi.update(input),
    onSuccess: () => invalidate(qc),
  });
};

export const useDeletePurchaseOrder = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => purchaseOrdersApi.remove(id),
    onSuccess: () => invalidate(qc),
  });
};

export const useReceivePurchaseOrder = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: ReceivePurchaseOrderInput) => purchaseOrdersApi.receive(input),
    onSuccess: () => {
      invalidate(qc);
      qc.invalidateQueries({ queryKey: ['inventory'] });
      qc.invalidateQueries({ queryKey: ['products'] });
    },
  });
};

export const usePurchaseOrderAction = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      id,
      action,
      reason,
    }: {
      id: string;
      action: 'submit' | 'approve' | 'cancel' | 'close';
      reason?: string | null;
    }) => {
      if (action === 'submit') return purchaseOrdersApi.submit(id);
      if (action === 'approve') return purchaseOrdersApi.approve(id);
      if (action === 'cancel') return purchaseOrdersApi.cancel(id, reason);
      return purchaseOrdersApi.close(id);
    },
    onSuccess: () => invalidate(qc),
  });
};
