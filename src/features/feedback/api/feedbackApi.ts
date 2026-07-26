import { apiClient } from '@/shared/api/apiClient';
import { cachedGet, invalidateHttpCache } from '@/shared/http/httpCache';
import type { ApiResponse } from '@/shared/types/api';
import type {
  AddFeedbackCommentInput,
  CreateFeedbackInput,
  FeedbackAttachment,
  FeedbackComment,
  FeedbackStatus,
  FeedbackTicket,
  FeedbackType,
  UpdateFeedbackStatusInput,
} from '../model/feedback.types';

const BASE = '/feedback';
const INVALIDATION = [/\/feedback/i] as const;

export interface FeedbackListParams {
  status?: FeedbackStatus;
  type?: FeedbackType;
}

export const feedbackApi = {
  list: (params: FeedbackListParams) =>
    cachedGet<ApiResponse<FeedbackTicket[]>>(apiClient, BASE, { params }),

  getById: (id: string) => cachedGet<ApiResponse<FeedbackTicket>>(apiClient, `${BASE}/${id}`),

  create: (input: CreateFeedbackInput) =>
    apiClient.post<ApiResponse<FeedbackTicket>>(BASE, input).then((r) => {
      invalidateHttpCache(INVALIDATION);
      return r.data;
    }),

  updateStatus: (input: UpdateFeedbackStatusInput) =>
    apiClient.put<ApiResponse<FeedbackTicket>>(`${BASE}/${input.id}/status`, input).then((r) => {
      invalidateHttpCache(INVALIDATION);
      return r.data;
    }),

  uploadAttachment: (id: string, file: File) => {
    const form = new FormData();
    form.append('file', file);
    return apiClient
      .post<ApiResponse<FeedbackTicket>>(`${BASE}/${id}/attachment`, form)
      .then((r) => {
        invalidateHttpCache(INVALIDATION);
        return r.data;
      });
  },
};

export const feedbackThreadApi = {
  listComments: (ticketId: string) =>
    cachedGet<ApiResponse<FeedbackComment[]>>(apiClient, `${BASE}/${ticketId}/comments`),

  addComment: (input: AddFeedbackCommentInput) =>
    apiClient
      .post<ApiResponse<FeedbackComment>>(`${BASE}/${input.ticketId}/comments`, {
        body: input.body,
        isInternal: input.isInternal ?? false,
      })
      .then((r) => {
        invalidateHttpCache(INVALIDATION);
        return r.data;
      }),

  listAttachments: (ticketId: string) =>
    cachedGet<ApiResponse<FeedbackAttachment[]>>(apiClient, `${BASE}/${ticketId}/attachments`),

  uploadAttachments: (ticketId: string, files: File[]) => {
    const form = new FormData();
    for (const file of files) form.append('files', file);
    return apiClient
      .post<ApiResponse<FeedbackAttachment[]>>(`${BASE}/${ticketId}/attachments`, form)
      .then((r) => {
        invalidateHttpCache(INVALIDATION);
        return r.data;
      });
  },

  deleteAttachment: (ticketId: string, attachmentId: string) =>
    apiClient.delete<void>(`${BASE}/${ticketId}/attachments/${attachmentId}`).then((r) => {
      invalidateHttpCache(INVALIDATION);
      return r.data;
    }),

  attachmentUrl: (ticketId: string, attachmentId: string) =>
    `${BASE}/${ticketId}/attachments/${attachmentId}`,
};
