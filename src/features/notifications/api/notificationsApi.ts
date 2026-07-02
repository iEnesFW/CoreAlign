import { apiClient } from '@/shared/api/apiClient';
import { cachedGet, invalidateHttpCache } from '@/shared/http/httpCache';
import type {
  NotificationMessageView,
  NotificationPreferenceView,
  UpsertNotificationPreferenceInput,
} from '../model/notifications.types';

const MY_BASE = '/notification-messages/me';
const PREF_BASE = '/users/me/notification-preferences';
const INVALIDATION = [/\/notification-messages/i, /\/notification-preferences/i] as const;

export interface MyNotificationsParams {
  unreadOnly?: boolean;
  page?: number;
  pageSize?: number;
}

export const notificationsApi = {
  listMine: (params: MyNotificationsParams) =>
    cachedGet<NotificationMessageView[]>(apiClient, MY_BASE, { params }),

  unreadCount: () => cachedGet<{ unread: number }>(apiClient, `${MY_BASE}/unread-count`),

  markRead: (id: string) =>
    apiClient.post(`/notification-messages/${id}/mark-read`).then(() => {
      invalidateHttpCache(INVALIDATION);
    }),

  acknowledge: (id: string, note?: string) =>
    apiClient.post(`/notification-messages/${id}/acknowledge`, { note: note ?? null }).then(() => {
      invalidateHttpCache(INVALIDATION);
    }),

  listPreferences: () => cachedGet<NotificationPreferenceView[]>(apiClient, PREF_BASE),

  upsertPreference: (input: UpsertNotificationPreferenceInput) =>
    apiClient.patch(PREF_BASE, input).then(() => {
      invalidateHttpCache(INVALIDATION);
    }),
};
