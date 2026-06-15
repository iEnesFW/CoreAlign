import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { glassWorkOrderRevisionsApi } from '../api/glassWorkOrderRevisionsApi';
import type {
  ApproveWorkOrderRevisionInput,
  RejectWorkOrderRevisionInput,
  WorkOrderRevisionDto,
} from '../model/workOrder.types';
import type { ApiResponse } from '@/shared/types/api';

export const workOrderRevisionKeys = {
  all: ['glass-enclosure', 'work-orders'] as const,
  list: (workOrderId: string) => [...workOrderRevisionKeys.all, workOrderId, 'revisions'] as const,
};

export const useWorkOrderRevisionsQuery = (workOrderId: string | null | undefined) =>
  useQuery<ApiResponse<WorkOrderRevisionDto[]>>({
    queryKey: workOrderRevisionKeys.list(workOrderId ?? ''),
    queryFn: () => glassWorkOrderRevisionsApi.list(workOrderId as string),
    enabled: Boolean(workOrderId),
  });

interface ApproveVariables {
  workOrderId: string;
  revisionId: string;
  input?: ApproveWorkOrderRevisionInput;
}

export const useApproveWorkOrderRevisionMutation = () => {
  const queryClient = useQueryClient();
  return useMutation<void, Error, ApproveVariables>({
    mutationFn: ({ workOrderId, revisionId, input }) =>
      glassWorkOrderRevisionsApi.approve(workOrderId, revisionId, input),
    onSuccess: (_data, variables) => {
      void queryClient.invalidateQueries({
        queryKey: workOrderRevisionKeys.list(variables.workOrderId),
      });
    },
  });
};

interface RejectVariables {
  workOrderId: string;
  revisionId: string;
  input: RejectWorkOrderRevisionInput;
}

export const useRejectWorkOrderRevisionMutation = () => {
  const queryClient = useQueryClient();
  return useMutation<void, Error, RejectVariables>({
    mutationFn: ({ workOrderId, revisionId, input }) =>
      glassWorkOrderRevisionsApi.reject(workOrderId, revisionId, input),
    onSuccess: (_data, variables) => {
      void queryClient.invalidateQueries({
        queryKey: workOrderRevisionKeys.list(variables.workOrderId),
      });
    },
  });
};
