import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { notificationsApi, type MyNotificationsParams } from '../api/notificationsApi';

export const useMyNotificationsQuery = (params: MyNotificationsParams = {}) =>
  useQuery({
    queryKey: ['notifications', 'me', params] as const,
    queryFn: () => notificationsApi.listMine(params),
    staleTime: 30 * 1000,
  });

export const useUnreadNotificationCountQuery = () =>
  useQuery({
    queryKey: ['notifications', 'me', 'unread-count'] as const,
    queryFn: () => notificationsApi.unreadCount(),
    refetchInterval: 60 * 1000,
    staleTime: 30 * 1000,
  });

export const useMarkNotificationRead = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => notificationsApi.markRead(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['notifications'] }),
  });
};
