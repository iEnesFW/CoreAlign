import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { notificationsApi } from '../api/notificationsApi';
import type { UpsertNotificationPreferenceInput } from '../model/notifications.types';

export const useNotificationPreferencesQuery = () =>
  useQuery({
    queryKey: ['notifications', 'preferences'] as const,
    queryFn: () => notificationsApi.listPreferences(),
    staleTime: 5 * 60 * 1000,
  });

export const useUpsertNotificationPreference = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: UpsertNotificationPreferenceInput) =>
      notificationsApi.upsertPreference(input),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['notifications', 'preferences'] }),
  });
};
