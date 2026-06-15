import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { collabApi } from '../api/collabApi';
import type { CollabEntityType, CreateCommentInput, EditCommentInput } from '../model/collab.types';

const THIRTY_SECONDS = 30 * 1000;

export const collabKeys = {
  all: ['collab'] as const,
  comments: (entityType: CollabEntityType, entityId: string) =>
    [...collabKeys.all, 'comments', entityType, entityId] as const,
  notifications: (params: { unreadOnly: boolean; take: number }) =>
    [...collabKeys.all, 'notifications', params] as const,
  unreadCount: () => [...collabKeys.all, 'unread-count'] as const,
};

export const useComments = (entityType: CollabEntityType, entityId: string | null | undefined) =>
  useQuery({
    queryKey: collabKeys.comments(entityType, entityId ?? ''),
    queryFn: () => collabApi.listComments(entityType, entityId as string),
    enabled: !!entityId,
    staleTime: 15 * 1000,
  });

export const useCreateComment = (entityType: CollabEntityType, entityId: string) => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: Omit<CreateCommentInput, 'entityType' | 'entityId'>) =>
      collabApi.createComment({ ...input, entityType, entityId }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: collabKeys.comments(entityType, entityId) });
      qc.invalidateQueries({ queryKey: collabKeys.unreadCount() });
      qc.invalidateQueries({ queryKey: [...collabKeys.all, 'notifications'] });
    },
  });
};

export const useEditComment = (entityType: CollabEntityType, entityId: string) => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: EditCommentInput) => collabApi.editComment(input),
    onSuccess: () => qc.invalidateQueries({ queryKey: collabKeys.comments(entityType, entityId) }),
  });
};

export const useDeleteComment = (entityType: CollabEntityType, entityId: string) => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => collabApi.deleteComment(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: collabKeys.comments(entityType, entityId) }),
  });
};

export const useNotifications = (
  params: { unreadOnly?: boolean; take?: number; enabled?: boolean } = {},
) => {
  const unreadOnly = params.unreadOnly ?? false;
  const take = params.take ?? 50;
  return useQuery({
    queryKey: collabKeys.notifications({ unreadOnly, take }),
    queryFn: () => collabApi.listNotifications({ unreadOnly, take }),
    enabled: params.enabled ?? true,
    staleTime: 10 * 1000,
  });
};

export const useUnreadCount = (options: { pollMs?: number; enabled?: boolean } = {}) =>
  useQuery({
    queryKey: collabKeys.unreadCount(),
    queryFn: () => collabApi.unreadCount(),
    refetchInterval: options.pollMs ?? THIRTY_SECONDS,
    enabled: options.enabled ?? true,
    staleTime: 10 * 1000,
  });

export const useMarkNotificationRead = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => collabApi.markRead(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: collabKeys.unreadCount() });
      qc.invalidateQueries({ queryKey: [...collabKeys.all, 'notifications'] });
    },
  });
};

export const useMarkAllNotificationsRead = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: () => collabApi.markAllRead(),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: collabKeys.unreadCount() });
      qc.invalidateQueries({ queryKey: [...collabKeys.all, 'notifications'] });
    },
  });
};
