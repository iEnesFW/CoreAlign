import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { returnsApi } from '../api/returnsApi';
import { returnKeys } from './returnKeys';
import type {
  CreateReturnRequestPayload,
  ReceiveReturnPayload,
  ReturnRequestListParams,
} from '../model/return.types';

export const useReturnRequestsQuery = (
  params: ReturnRequestListParams,
  options?: { enabled?: boolean },
) =>
  useQuery({
    queryKey: returnKeys.list(params),
    queryFn: () => returnsApi.list(params),
    placeholderData: (previous) => previous,
    enabled: options?.enabled ?? true,
  });

export const useReturnRequestQuery = (id: string | null) =>
  useQuery({
    queryKey: returnKeys.detail(id),
    queryFn: () => returnsApi.getById(id as string),
    enabled: id !== null,
  });

export const useReturnsByOrderQuery = (orderId: string | null) =>
  useQuery({
    queryKey: returnKeys.byOrder(orderId),
    queryFn: () => returnsApi.listByOrder(orderId as string),
    enabled: orderId !== null,
  });

export const useCreateReturnRequest = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: CreateReturnRequestPayload) => returnsApi.create(payload),
    onSuccess: (_, payload) => {
      queryClient.invalidateQueries({ queryKey: returnKeys.lists() });
      queryClient.invalidateQueries({ queryKey: returnKeys.byOrder(payload.orderId) });
    },
  });
};

const invalidateAfterMutation = (qc: ReturnType<typeof useQueryClient>, id: string) => {
  qc.invalidateQueries({ queryKey: returnKeys.lists() });
  qc.invalidateQueries({ queryKey: returnKeys.detail(id) });
};

export const useApproveReturn = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => returnsApi.approve(id),
    onSuccess: (_, id) => invalidateAfterMutation(qc, id),
  });
};

export const useRejectReturn = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, reason }: { id: string; reason?: string | null }) =>
      returnsApi.reject(id, reason),
    onSuccess: (_, vars) => invalidateAfterMutation(qc, vars.id),
  });
};

export const useCancelReturn = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => returnsApi.cancel(id),
    onSuccess: (_, id) => invalidateAfterMutation(qc, id),
  });
};

export const useReceiveReturn = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: ReceiveReturnPayload }) =>
      returnsApi.receive(id, payload),
    onSuccess: (_, vars) => {
      invalidateAfterMutation(qc, vars.id);
      qc.invalidateQueries({ queryKey: ['invoices'] });
    },
  });
};
