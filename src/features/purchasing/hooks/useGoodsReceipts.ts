import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { goodsReceiptsApi } from '../api/goodsReceiptsApi';
import type { GoodsReceiptListParams, ReverseGoodsReceiptInput } from '../model/goodsReceipt.types';

export const useGoodsReceiptsQuery = (params: GoodsReceiptListParams, enabled = true) =>
  useQuery({
    queryKey: ['goods-receipts', 'list', params] as const,
    queryFn: () => goodsReceiptsApi.search(params),
    staleTime: 30 * 1000,
    enabled,
  });

export const useGoodsReceiptQuery = (id: string | null) =>
  useQuery({
    queryKey: ['goods-receipts', 'detail', id] as const,
    queryFn: () => goodsReceiptsApi.getById(id as string),
    enabled: id !== null,
  });

const invalidate = (qc: ReturnType<typeof useQueryClient>) => {
  qc.invalidateQueries({ queryKey: ['goods-receipts'] });
  qc.invalidateQueries({ queryKey: ['purchase-orders'] });
  qc.invalidateQueries({ queryKey: ['inventory'] });
  qc.invalidateQueries({ queryKey: ['products'] });
};

export const useReverseGoodsReceipt = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, reason }: ReverseGoodsReceiptInput) => goodsReceiptsApi.reverse(id, reason),
    onSuccess: () => invalidate(qc),
  });
};

export const useApproveGoodsReceiptQc = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => goodsReceiptsApi.approveQc(id),
    onSuccess: () => invalidate(qc),
  });
};

export const useRejectGoodsReceiptQc = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) =>
      goodsReceiptsApi.rejectQc(id, reason),
    onSuccess: () => invalidate(qc),
  });
};
