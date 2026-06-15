import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  portalProfileApi,
  type UpdatePortalProfileInput,
  type PortalNotificationPreference,
  type ChangePasswordInput,
} from './profileApi';
import { portalNotificationsApi } from './notificationsApi';

export const profileKeys = {
  profile: ['portal', 'profile'] as const,
  sessions: ['portal', 'profile', 'sessions'] as const,
  notificationPreferences: ['portal', 'notificationPreferences'] as const,
  notifications: ['portal', 'notifications'] as const,
  unreadCount: ['portal', 'notifications', 'unreadCount'] as const,
};

export const usePortalProfile = () =>
  useQuery({
    queryKey: profileKeys.profile,
    queryFn: () => portalProfileApi.getProfile(),
    staleTime: 30_000,
  });

export const useUpdatePortalProfile = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: UpdatePortalProfileInput) => portalProfileApi.updateProfile(input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: profileKeys.profile });
    },
  });
};

export const useChangePassword = () =>
  useMutation({
    mutationFn: (input: ChangePasswordInput) => portalProfileApi.changePassword(input),
  });

export const usePortalSessions = () =>
  useQuery({
    queryKey: profileKeys.sessions,
    queryFn: () => portalProfileApi.listSessions(),
    staleTime: 10_000,
  });

export const useRevokeAllSessions = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => portalProfileApi.revokeAllSessions(),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: profileKeys.sessions });
    },
  });
};

export const usePortalNotificationPreferences = () =>
  useQuery({
    queryKey: profileKeys.notificationPreferences,
    queryFn: () => portalProfileApi.listNotificationPreferences(),
    staleTime: 60_000,
  });

export const useUpdateNotificationPreference = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: PortalNotificationPreference) =>
      portalProfileApi.updateNotificationPreference(input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: profileKeys.notificationPreferences });
    },
  });
};

export const useEnrollTwoFactor = () =>
  useMutation({
    mutationFn: () => portalProfileApi.enrollTwoFactor(),
  });

export const useVerifyTwoFactor = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (code: string) => portalProfileApi.verifyTwoFactor(code),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: profileKeys.profile });
    },
  });
};

export const useDisableTwoFactor = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (password: string) => portalProfileApi.disableTwoFactor(password),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: profileKeys.profile });
    },
  });
};

export const useRegenerateBackupCodes = () =>
  useMutation({
    mutationFn: (password: string) => portalProfileApi.regenerateBackupCodes(password),
  });

export const usePayInvoice = () =>
  useMutation({
    mutationFn: (invoiceId: string) => portalProfileApi.payInvoice(invoiceId),
  });

export const usePortalNotifications = (params: { take?: number; enabled?: boolean }) =>
  useQuery({
    queryKey: [...profileKeys.notifications, params.take ?? 30] as const,
    queryFn: () => portalNotificationsApi.list(false, params.take ?? 30),
    enabled: params.enabled ?? true,
    staleTime: 15_000,
  });

export const usePortalUnreadCount = (params: { pollMs?: number; enabled?: boolean }) =>
  useQuery({
    queryKey: profileKeys.unreadCount,
    queryFn: () => portalNotificationsApi.unreadCount(),
    refetchInterval: params.pollMs ?? false,
    enabled: params.enabled ?? true,
    staleTime: 5_000,
  });

export const useMarkPortalNotificationRead = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => portalNotificationsApi.markRead(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: profileKeys.notifications });
      queryClient.invalidateQueries({ queryKey: profileKeys.unreadCount });
    },
  });
};

export const useMarkAllPortalNotificationsRead = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => portalNotificationsApi.markAllRead(),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: profileKeys.notifications });
      queryClient.invalidateQueries({ queryKey: profileKeys.unreadCount });
    },
  });
};
