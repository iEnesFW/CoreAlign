import { apiClient } from '@/shared/api/apiClient';
import type { ApiResponse } from '@/shared/types/api';

export interface ProfileNotificationPreference {
  notificationKind: string;
  emailEnabled: boolean;
  inAppEnabled: boolean;
}

export interface UpdateProfileNotificationPreferencesPayload {
  items: ProfileNotificationPreference[];
}

const BASE = '/profile/notification-preferences';

export const profileNotificationsApi = {
  list: () => apiClient.get<ApiResponse<ProfileNotificationPreference[]>>(BASE).then((r) => r.data),

  update: (payload: UpdateProfileNotificationPreferencesPayload) =>
    apiClient.put<ApiResponse<ProfileNotificationPreference[]>>(BASE, payload).then((r) => r.data),
};
