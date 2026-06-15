import { apiClient } from '@/shared/api/apiClient';

export interface PortalNotification {
  id: string;
  type: string;
  entityType: string;
  entityId: string;
  title: string;
  body: string;
  actorUserId: string | null;
  actorName: string | null;
  isRead: boolean;
  createdAtUtc: string;
}

export const portalNotificationsApi = {
  list: async (unreadOnly = false, take = 30): Promise<PortalNotification[]> => {
    const { data } = await apiClient.get<PortalNotification[]>('/customer-portal/notifications', {
      params: { unreadOnly, take },
    });
    return data;
  },
  unreadCount: async (): Promise<number> => {
    const { data } = await apiClient.get<number>('/customer-portal/notifications/unread-count');
    return data;
  },
  markRead: async (id: string): Promise<boolean> => {
    const { data } = await apiClient.post<boolean>(`/customer-portal/notifications/${id}/read`);
    return data;
  },
  markAllRead: async (): Promise<number> => {
    const { data } = await apiClient.post<number>('/customer-portal/notifications/read-all');
    return data;
  },
};
