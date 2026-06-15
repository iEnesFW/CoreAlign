import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { orderRevisionsApi, type RevisionLineInput } from '../api/orderRevisionsApi';
import { orderKeys } from './orderKeys';

const revisionKeys = {
  all: (orderId: string) => [...orderKeys.detail(orderId), 'revisions'] as const,
};

export const useOrderRevisionsQuery = (orderId: string | null) =>
  useQuery({
    queryKey: orderId ? revisionKeys.all(orderId) : ['order-revisions', 'idle'],
    queryFn: () => orderRevisionsApi.list(orderId as string).then((r) => r.data),
    enabled: !!orderId,
  });

export const useRequestOrderRevision = (orderId: string) => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: { proposedLines: RevisionLineInput[]; requestNotes?: string | null }) =>
      orderRevisionsApi.request(orderId, input.proposedLines, input.requestNotes),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: revisionKeys.all(orderId) });
      qc.invalidateQueries({ queryKey: orderKeys.detail(orderId) });
    },
  });
};

export const useApproveOrderRevision = (orderId: string) => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (revisionId: string) => orderRevisionsApi.approve(orderId, revisionId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: revisionKeys.all(orderId) });
      qc.invalidateQueries({ queryKey: orderKeys.detail(orderId) });
    },
  });
};

export const useRejectOrderRevision = (orderId: string) => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: { revisionId: string; reason: string }) =>
      orderRevisionsApi.reject(orderId, input.revisionId, input.reason),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: revisionKeys.all(orderId) });
      qc.invalidateQueries({ queryKey: orderKeys.detail(orderId) });
    },
  });
};

export const useCancelOrderRevision = (orderId: string) => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (revisionId: string) => orderRevisionsApi.cancel(orderId, revisionId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: revisionKeys.all(orderId) });
      qc.invalidateQueries({ queryKey: orderKeys.detail(orderId) });
    },
  });
};
