import { apiClient } from '@/shared/api/apiClient';
import { cachedGet, invalidateHttpCache } from '@/shared/http/httpCache';
import type { ApiResponse } from '@/shared/types/api';
import type { CreateTagInput, Tag, UpdateTagInput } from '../model/tag.types';

const BASE = '/tags';

const TAGS_INVALIDATION = [/\/tags/i, /\/customers/i] as const;

export const tagsApi = {
  list: (isActive?: boolean) =>
    cachedGet<ApiResponse<Tag[]>>(apiClient, BASE, {
      params: isActive === undefined ? {} : { isActive },
    }),

  create: (input: CreateTagInput) =>
    apiClient.post<ApiResponse<Tag>>(BASE, input).then((r) => {
      invalidateHttpCache(TAGS_INVALIDATION);
      return r.data;
    }),

  update: (input: UpdateTagInput) =>
    apiClient.put<ApiResponse<Tag>>(`${BASE}/${input.id}`, input).then((r) => {
      invalidateHttpCache(TAGS_INVALIDATION);
      return r.data;
    }),

  remove: (id: string) =>
    apiClient.delete<ApiResponse<boolean>>(`${BASE}/${id}`).then((r) => {
      invalidateHttpCache(TAGS_INVALIDATION);
      return r.data;
    }),
};
