import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { ordersApi, type BulkOrderActionKind } from '../api/ordersApi';
import { orderKeys } from './orderKeys';
import type {
  CreateOrderInput,
  CreateShipmentInput,
  DeliverShipmentInput,
  DispatchShipmentInput,
  OrderListParams,
  RecordOrderScrapInput,
  UpdateOrderInput,
} from '../model/order.types';

export const useOrdersQuery = (params: OrderListParams, options?: { enabled?: boolean }) =>
  useQuery({
    queryKey: orderKeys.list(params),
    queryFn: () => ordersApi.list(params),
    placeholderData: (previous) => previous,
    enabled: options?.enabled ?? true,
  });

export const useOrderQuery = (id: string | null) =>
  useQuery({
    queryKey: orderKeys.detail(id),
    queryFn: () => ordersApi.getById(id as string),
    enabled: id !== null,
  });

export const useShipmentsByOrderQuery = (orderId: string | null) =>
  useQuery({
    queryKey: ['shipments', 'by-order', orderId] as const,
    queryFn: () => ordersApi.getShipmentsByOrder(orderId as string),
    enabled: orderId !== null,
  });

const invalidateOrder = (queryClient: ReturnType<typeof useQueryClient>, id?: string) => {
  queryClient.invalidateQueries({ queryKey: orderKeys.lists() });
  if (id) {
    queryClient.invalidateQueries({ queryKey: orderKeys.detail(id) });
    queryClient.invalidateQueries({ queryKey: ['shipments', 'by-order', id] });
  } else {
    queryClient.invalidateQueries({ queryKey: orderKeys.details() });
    queryClient.invalidateQueries({ queryKey: ['shipments'] });
  }
  queryClient.invalidateQueries({ queryKey: ['inventory'] });
  queryClient.invalidateQueries({ queryKey: ['customers'] });
};

export const useCreateOrder = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: CreateOrderInput) => ordersApi.create(input),
    onSuccess: () => invalidateOrder(queryClient),
  });
};

export const useUpdateOrder = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: UpdateOrderInput) => ordersApi.update(input),
    onSuccess: (_, vars) => invalidateOrder(queryClient, vars.id),
  });
};

export const useDeleteOrder = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => ordersApi.remove(id),
    onSuccess: (_, id) => {
      invalidateOrder(queryClient);
      queryClient.removeQueries({ queryKey: orderKeys.detail(id) });
    },
  });
};

export const useSubmitOrder = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => ordersApi.submit(id),
    onSuccess: (_, id) => invalidateOrder(queryClient, id),
  });
};

export const useReorderOrder = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => ordersApi.reorder(id),
    onSuccess: () => invalidateOrder(queryClient),
  });
};

export const useBulkOrderAction = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (params: {
      orderIds: string[];
      action: BulkOrderActionKind;
      reason?: string | null;
    }) => ordersApi.bulkAction(params.orderIds, params.action, params.reason),
    onSuccess: () => invalidateOrder(queryClient),
  });
};

export const useApproveOrder = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (params: { id: string; approvedByUserId?: string | null }) =>
      ordersApi.approve(params.id, params.approvedByUserId),
    onSuccess: (_, params) => invalidateOrder(queryClient, params.id),
  });
};

export const useAllocateOrder = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (params: { id: string; preferredWarehouseId?: string | null }) =>
      ordersApi.allocate(params.id, params.preferredWarehouseId),
    onSuccess: (_, params) => invalidateOrder(queryClient, params.id),
  });
};

export const useCancelOrder = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (params: { id: string; reason?: string | null }) =>
      ordersApi.cancel(params.id, params.reason),
    onSuccess: (_, params) => invalidateOrder(queryClient, params.id),
  });
};

export const useDeliverOrder = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (params: { id: string; deliveredAtUtc?: string | null }) =>
      ordersApi.deliver(params.id, params.deliveredAtUtc),
    onSuccess: (_, params) => invalidateOrder(queryClient, params.id),
  });
};

export const useCloseOrder = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => ordersApi.close(id),
    onSuccess: (_, id) => invalidateOrder(queryClient, id),
  });
};

export const useCreateShipment = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: CreateShipmentInput) => ordersApi.createShipment(input),
    onSuccess: (_, input) => invalidateOrder(queryClient, input.orderId),
  });
};

export const usePickShipment = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => ordersApi.pickShipment(id),
    onSuccess: () => invalidateOrder(queryClient),
  });
};

export const usePackShipment = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => ordersApi.packShipment(id),
    onSuccess: () => invalidateOrder(queryClient),
  });
};

export const useDispatchShipment = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: DispatchShipmentInput) => ordersApi.dispatchShipment(input),
    onSuccess: () => invalidateOrder(queryClient),
  });
};

export const useDeliverShipment = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: DeliverShipmentInput) => ordersApi.deliverShipment(input),
    onSuccess: () => invalidateOrder(queryClient),
  });
};

export const useCancelShipment = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (params: { id: string; reason?: string | null }) =>
      ordersApi.cancelShipment(params.id, params.reason),
    onSuccess: () => invalidateOrder(queryClient),
  });
};

export const useRecordOrderScrap = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: RecordOrderScrapInput) => ordersApi.recordScrap(input),
    onSuccess: (_, input) => invalidateOrder(queryClient, input.id),
  });
};
