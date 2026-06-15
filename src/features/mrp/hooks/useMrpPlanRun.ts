import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { inventoryApi } from '@/features/inventory/api/inventoryApi';
import { newOperationId } from '@/shared/lib/operationId';
import type { TransferStockInput } from '@/features/inventory/model/inventory.types';
import { mrpPlanningApi } from '../api/mrpPlanningApi';

export const useMrpPeggingQuery = (planRunId: string | null, componentProductId: string | null) =>
  useQuery({
    queryKey: ['mrp-planning', 'pegging', planRunId, componentProductId] as const,
    queryFn: () => mrpPlanningApi.pegging(planRunId as string, componentProductId as string),
    enabled: !!planRunId && !!componentProductId,
    staleTime: 60 * 1000,
  });

export const useMrpChangeImpactQuery = (
  planRunId: string | null,
  sourceOrderLineId: string | null,
) =>
  useQuery({
    queryKey: ['mrp-planning', 'change-impact', planRunId, sourceOrderLineId] as const,
    queryFn: () =>
      mrpPlanningApi.changeImpact({
        planRunId: planRunId as string,
        sourceOrderLineId: sourceOrderLineId as string,
      }),
    enabled: !!planRunId && !!sourceOrderLineId,
    staleTime: 60 * 1000,
  });

export const useMrpTransferSuggestionsQuery = (enabled = true) =>
  useQuery({
    queryKey: ['mrp-planning', 'transfer-suggestions'] as const,
    queryFn: () => mrpPlanningApi.transferSuggestions(),
    enabled,
    staleTime: 30 * 1000,
  });

export const useExecuteTransferSuggestion = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: Omit<TransferStockInput, 'operationId'>) =>
      inventoryApi.transfer({ ...input, operationId: newOperationId() }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['mrp-planning', 'transfer-suggestions'] });
      qc.invalidateQueries({ queryKey: ['inventory'] });
      qc.invalidateQueries({ queryKey: ['products'] });
    },
  });
};
