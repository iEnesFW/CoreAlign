import { apiClient } from '@/shared/api/apiClient';
import { invalidateHttpCache } from '@/shared/http/httpCache';
import type { ApiResponse } from '@/shared/types/api';
import type {
  CollabEntityType,
  Comment,
  CreateCommentInput,
  EditCommentInput,
  Notification,
} from '../model/collab.types';

const COMMENTS_BASE = '/comments';
const NOTIFICATIONS_BASE = '/notifications';

const NOTIFICATION_INVALIDATION = [/\/notifications/i] as const;

export const collabApi = {
  listComments: (entityType: CollabEntityType, entityId: string) =>
    apiClient
      .get<ApiResponse<Comment[]>>(COMMENTS_BASE, {
        params: { entityType, entityId },
      })
      .then((r) => r.data),

  createComment: (input: CreateCommentInput) =>
    apiClient.post<ApiResponse<Comment>>(COMMENTS_BASE, input).then((r) => {
      invalidateHttpCache(NOTIFICATION_INVALIDATION);
      return r.data;
    }),

  editComment: (input: EditCommentInput) =>
    apiClient
      .put<ApiResponse<Comment>>(`${COMMENTS_BASE}/${input.id}`, { id: input.id, body: input.body })
      .then((r) => r.data),

  deleteComment: (id: string) =>
    apiClient.delete<ApiResponse<boolean>>(`${COMMENTS_BASE}/${id}`).then((r) => r.data),

  listNotifications: (params: { unreadOnly?: boolean; take?: number } = {}) =>
    apiClient
      .get<ApiResponse<Notification[]>>(NOTIFICATIONS_BASE, {
        params: {
          unreadOnly: params.unreadOnly ?? false,
          take: params.take ?? 50,
        },
      })
      .then((r) => r.data),

  unreadCount: () =>
    apiClient.get<ApiResponse<number>>(`${NOTIFICATIONS_BASE}/unread-count`).then((r) => r.data),

  markRead: (id: string) =>
    apiClient.post<ApiResponse<boolean>>(`${NOTIFICATIONS_BASE}/${id}/read`).then((r) => {
      invalidateHttpCache(NOTIFICATION_INVALIDATION);
      return r.data;
    }),

  markAllRead: () =>
    apiClient.post<ApiResponse<number>>(`${NOTIFICATIONS_BASE}/read-all`).then((r) => {
      invalidateHttpCache(NOTIFICATION_INVALIDATION);
      return r.data;
    }),
};
