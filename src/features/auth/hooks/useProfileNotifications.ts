import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  profileNotificationsApi,
  type ProfileNotificationPreference,
  type UpdateProfileNotificationPreferencesPayload,
} from '../api/profileNotificationsApi';

const QUERY_KEY = ['profile', 'notification-preferences'];

export const useProfileNotificationPreferencesQuery = () =>
  useQuery({
    queryKey: QUERY_KEY,
    queryFn: async () => {
      const response = await profileNotificationsApi.list();
      return response.data ?? ([] as ProfileNotificationPreference[]);
    },
  });

export const useUpdateProfileNotificationPreferences = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: UpdateProfileNotificationPreferencesPayload) =>
      profileNotificationsApi.update(payload),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: QUERY_KEY });
    },
  });
};
