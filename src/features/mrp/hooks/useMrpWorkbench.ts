import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { newOperationId } from '@/shared/lib/operationId';
import { mrpPlanningApi } from '../api/mrpPlanningApi';
import type {
  FirmPlannedOrderInput,
  MrpCapacityLoadParams,
  MrpItemPlanParams,
  MrpPreviewParams,
} from '../model/mrp-planning.types';

const invalidatePlanRunChildren = (qc: ReturnType<typeof useQueryClient>) => {
  invalidatePlanning(qc);
  qc.invalidateQueries({ queryKey: ['purchase-orders'] });
  qc.invalidateQueries({ queryKey: ['production-orders'] });
};

const invalidatePlanning = (qc: ReturnType<typeof useQueryClient>) => {
  qc.invalidateQueries({ queryKey: ['mrp-planning'] });
  qc.invalidateQueries({ queryKey: ['mrp'] });
  qc.invalidateQueries({ queryKey: ['purchase-requisitions'] });
};

export const useMrpPreviewQuery = (params: MrpPreviewParams, enabled = true) =>
  useQuery({
    queryKey: ['mrp-planning', 'preview', params] as const,
    queryFn: () => mrpPlanningApi.preview(params),
    enabled,
    staleTime: 30 * 1000,
  });

export const useMrpCapacityLoadQuery = (params: MrpCapacityLoadParams, enabled = true) =>
  useQuery({
    queryKey: ['mrp-planning', 'capacity-load', params] as const,
    queryFn: () => mrpPlanningApi.capacityLoad(params),
    enabled,
    staleTime: 30 * 1000,
  });

export const useMrpItemPlanQuery = (params: MrpItemPlanParams | null) =>
  useQuery({
    queryKey: ['mrp-planning', 'item', params] as const,
    queryFn: () => mrpPlanningApi.itemPlan(params as MrpItemPlanParams),
    enabled: !!params?.productId,
    staleTime: 30 * 1000,
  });

export const useMrpPlanRunsQuery = (page = 1, pageSize = 25) =>
  useQuery({
    queryKey: ['mrp-planning', 'runs', page, pageSize] as const,
    queryFn: () => mrpPlanningApi.listPlanRuns(page, pageSize),
    staleTime: 30 * 1000,
  });

export const useCommitMrpPlan = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (params: MrpPreviewParams) =>
      mrpPlanningApi.commit({ ...params, operationId: newOperationId() }),
    onSuccess: () => invalidatePlanning(qc),
  });
};

export const useReleasePlannedOrders = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      planRunId,
      plannedOrderIds,
    }: {
      planRunId: string;
      plannedOrderIds: string[];
    }) => mrpPlanningApi.release({ planRunId, plannedOrderIds, operationId: newOperationId() }),
    onSuccess: () => invalidatePlanRunChildren(qc),
  });
};

export const useFirmPlannedOrder = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: Omit<FirmPlannedOrderInput, 'operationId'>) =>
      mrpPlanningApi.firmPlannedOrder({ ...input, operationId: newOperationId() }),
    onSuccess: () => invalidatePlanning(qc),
  });
};

export const useFirmProductionOrder = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ productionOrderId }: { productionOrderId: string }) =>
      mrpPlanningApi.firmProductionOrder({ productionOrderId, operationId: newOperationId() }),
    onSuccess: () => invalidatePlanRunChildren(qc),
  });
};

export const useReleaseProductionOrder = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ productionOrderId }: { productionOrderId: string }) =>
      mrpPlanningApi.releaseProductionOrder({ productionOrderId, operationId: newOperationId() }),
    onSuccess: () => invalidatePlanRunChildren(qc),
  });
};

export const useCompleteProductionOrder = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      productionOrderId,
      warehouseId,
    }: {
      productionOrderId: string;
      warehouseId?: string | null;
    }) =>
      mrpPlanningApi.completeProductionOrder({
        productionOrderId,
        warehouseId: warehouseId ?? null,
        operationId: newOperationId(),
      }),
    onSuccess: () => invalidatePlanRunChildren(qc),
  });
};

export const useClassifyAbc = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: () => mrpPlanningApi.classifyAbc(),
    onSuccess: () => invalidatePlanning(qc),
  });
};
