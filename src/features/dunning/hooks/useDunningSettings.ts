import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { dunningSettingsApi } from '../api/dunningSettingsApi';
import type { UpsertDunningSettingInput } from '../model/dunning.types';

export const useDunningSettingsQuery = () =>
  useQuery({
    queryKey: ['dunning-settings', 'list'] as const,
    queryFn: () => dunningSettingsApi.list(),
    staleTime: 30 * 1000,
  });

export const useUpsertDunningSetting = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: UpsertDunningSettingInput) => dunningSettingsApi.upsert(input),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['dunning-settings'] }),
  });
};
