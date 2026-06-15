import { useQuery } from '@tanstack/react-query';
import { getTypedClient } from './typedClient';

export const useTypedSettings = (enabled = false) =>
  useQuery({
    queryKey: ['typed', 'admin', 'settings'] as const,
    queryFn: () => getTypedClient().settings(),
    enabled,
    staleTime: 5 * 60_000,
  });

export const useTypedNotifications = (
  params: { unreadOnly?: boolean; take?: number; enabled?: boolean } = {},
) =>
  useQuery({
    queryKey: [
      'typed',
      'admin',
      'notifications',
      params.unreadOnly ?? false,
      params.take ?? 30,
    ] as const,
    queryFn: () => getTypedClient().notifications3(params.unreadOnly, params.take ?? 30),
    enabled: params.enabled ?? false,
    staleTime: 30_000,
  });

export const useTypedOutbox = (status?: string, enabled = false) =>
  useQuery({
    queryKey: ['typed', 'admin', 'outbox', status ?? null] as const,
    queryFn: () => getTypedClient().outbox(status as never),
    enabled,
    staleTime: 30_000,
  });
