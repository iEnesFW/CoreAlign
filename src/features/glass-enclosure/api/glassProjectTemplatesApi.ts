import { apiClient } from '@/shared/api/apiClient';
import { cachedGet, invalidateHttpCache } from '@/shared/http/httpCache';
import type { ApiResponse } from '@/shared/types/api';

const BASE = '/glass-enclosure/project-templates';
const INVALIDATION = [/\/glass-enclosure\/project-templates/i] as const;

export interface GlassProjectTemplateSummaryDto {
  id: string;
  name: string;
  wallCount: number;
  slabCount: number;
  runCount: number;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface GlassProjectTemplateDto {
  id: string;
  name: string;
  payloadJson: string;
  wallCount: number;
  slabCount: number;
  runCount: number;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface SaveGlassProjectTemplateInput {
  name: string;
  payloadJson: string;
}

export const glassProjectTemplatesApi = {
  list: () => cachedGet<ApiResponse<GlassProjectTemplateSummaryDto[]>>(apiClient, BASE),

  getById: (id: string) =>
    cachedGet<ApiResponse<GlassProjectTemplateDto>>(apiClient, `${BASE}/${id}`),

  save: (input: SaveGlassProjectTemplateInput) =>
    apiClient.post<ApiResponse<GlassProjectTemplateDto>>(BASE, input).then((r) => {
      invalidateHttpCache(INVALIDATION);
      return r.data;
    }),

  remove: (id: string) =>
    apiClient.delete(`${BASE}/${id}`).then((r) => {
      invalidateHttpCache(INVALIDATION);
      return r.data;
    }),
};
