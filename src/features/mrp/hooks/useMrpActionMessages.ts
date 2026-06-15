import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { mrpPlanningApi } from '../api/mrpPlanningApi';
import type { MrpActionMessageParams } from '../model/mrp-planning.types';

export const useMrpActionMessagesQuery = (params: MrpActionMessageParams) =>
  useQuery({
    queryKey: ['mrp-planning', 'action-messages', params] as const,
    queryFn: () => mrpPlanningApi.listActionMessages(params),
    staleTime: 30 * 1000,
  });

export const useDismissActionMessage = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => mrpPlanningApi.dismissActionMessage(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['mrp-planning'] });
    },
  });
};
