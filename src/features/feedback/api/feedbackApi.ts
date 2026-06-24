import { apiClient } from '@/shared/api/apiClient';
import { cachedGet, invalidateHttpCache } from '@/shared/http/httpCache';
import type { ApiResponse } from '@/shared/types/api';
import type {
  CreateFeedbackInput,
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
