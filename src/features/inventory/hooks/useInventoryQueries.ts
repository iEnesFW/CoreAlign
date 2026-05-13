import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  inventoryApi,
  type StockItemsParams,
  type StockMovementsParams,
} from '../api/inventoryApi';
import type {
  AdjustStockInput,
  CreateLotInput,
  IssueStockInput,
  ReceiveStockInput,
  StockReasonCategory,
  UpdateLotInput,
} from '../model/inventory.types';

const THIRTY_SECONDS = 30 * 1000;
const FIVE_MINUTES = 5 * 60 * 1000;

export const useStockItemsQuery = (params: StockItemsParams) =>
  useQuery({
    queryKey: ['inventory', 'items', params] as const,
    queryFn: () => inventoryApi.stockItems(params),
    staleTime: THIRTY_SECONDS,
  });

export const useStockByProductQuery = (productId: string | null) =>
  useQuery({
    queryKey: ['inventory', 'items', 'by-product', productId] as const,
    queryFn: () => inventoryApi.stockByProduct(productId as string),
    enabled: productId !== null,
    staleTime: THIRTY_SECONDS,
  });

export const useStockSummaryQuery = (productId: string | null) =>
  useQuery({
    queryKey: ['inventory', 'summary', productId] as const,
    queryFn: () => inventoryApi.stockSummary(productId as string),
    enabled: productId !== null,
    staleTime: THIRTY_SECONDS,
  });

export const useStockMovementsQuery = (params: StockMovementsParams) =>
  useQuery({
    queryKey: ['inventory', 'movements', params] as const,
    queryFn: () => inventoryApi.movements(params),
    staleTime: THIRTY_SECONDS,
  });

export const useAllocationsByOrderQuery = (orderId: string | null) =>
  useQuery({
    queryKey: ['inventory', 'allocations', 'by-order', orderId] as const,
    queryFn: () => inventoryApi.allocationsByOrder(orderId as string),
    enabled: orderId !== null,
    staleTime: THIRTY_SECONDS,
  });

export const useLotsByProductQuery = (productId: string | null) =>
  useQuery({
    queryKey: ['inventory', 'lots', 'by-product', productId] as const,
    queryFn: () => inventoryApi.lotsByProduct(productId as string),
    enabled: productId !== null,
    staleTime: FIVE_MINUTES,
  });

export const useReasonCodesQuery = (category?: StockReasonCategory, isActive?: boolean) =>
  useQuery({
    queryKey: ['inventory', 'reason-codes', { category, isActive }] as const,
    queryFn: () => inventoryApi.reasonCodes(category, isActive),
    staleTime: FIVE_MINUTES,
  });

const invalidateInventory = (qc: ReturnType<typeof useQueryClient>) => {
  qc.invalidateQueries({ queryKey: ['inventory'] });
  qc.invalidateQueries({ queryKey: ['products'] });
};

export const useAdjustStock = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: AdjustStockInput) => inventoryApi.adjust(input),
    onSuccess: () => invalidateInventory(qc),
  });
};

export const useReceiveStock = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: ReceiveStockInput) => inventoryApi.receive(input),
    onSuccess: () => invalidateInventory(qc),
  });
};

export const useIssueStock = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: IssueStockInput) => inventoryApi.issue(input),
    onSuccess: () => invalidateInventory(qc),
  });
};

export const useCreateLot = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: CreateLotInput) => inventoryApi.createLot(input),
    onSuccess: () => invalidateInventory(qc),
  });
};

export const useUpdateLot = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: UpdateLotInput) => inventoryApi.updateLot(input),
    onSuccess: () => invalidateInventory(qc),
  });
};
