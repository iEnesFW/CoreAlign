import { apiClient } from '@/shared/api/apiClient';
import { cachedGet, invalidateHttpCache } from '@/shared/http/httpCache';
import type { ApiResponse } from '@/shared/types/api';
import type {
  CreateFieldSurveyInput,
  FieldSurveyApplyResultDto,
  FieldSurveyDto,
  FieldSurveyUploadResultDto,
  UpdateFieldSurveyInput,
} from '../model/fieldSurvey.types';

const BASE = '/glass-enclosure/field-surveys';
const INVALIDATION = [/\/glass-enclosure\/field-surveys/i] as const;

const post = <T, U = unknown>(path: string, body: U) =>
  apiClient.post<ApiResponse<T>>(`${BASE}${path}`, body).then((r) => {
    invalidateHttpCache(INVALIDATION);
    return r.data;
  });

const put = <T, U = unknown>(path: string, body: U) =>
  apiClient.put<ApiResponse<T>>(`${BASE}${path}`, body).then((r) => {
    invalidateHttpCache(INVALIDATION);
    return r.data;
  });

const del = (path: string) =>
  apiClient.delete(`${BASE}${path}`).then((r) => {
    invalidateHttpCache(INVALIDATION);
    return r.data;
  });

export const glassFieldSurveysApi = {
  listByProject: (projectId: string) =>
    cachedGet<ApiResponse<FieldSurveyDto[]>>(apiClient, `${BASE}/by-project/${projectId}`),

  getById: (id: string) => cachedGet<ApiResponse<FieldSurveyDto>>(apiClient, `${BASE}/${id}`),

  create: (input: CreateFieldSurveyInput) => post<FieldSurveyDto>('', input),
  update: (id: string, input: UpdateFieldSurveyInput) => put<FieldSurveyDto>(`/${id}`, input),
  submit: (id: string) => post<FieldSurveyDto>(`/${id}/submit`, {}),
  approve: (id: string, applyToProject: boolean) =>
    post<FieldSurveyApplyResultDto | null>(`/${id}/approve`, { applyToProject }),
  reject: (id: string, reason: string | null) => post<FieldSurveyDto>(`/${id}/reject`, { reason }),
  apply: (id: string) => post<FieldSurveyApplyResultDto>(`/${id}/apply`, {}),
  remove: (id: string) => del(`/${id}`),
  uploadPhoto: (surveyId: string, file: File) => {
    const formData = new FormData();
    formData.append('file', file);
    return apiClient
      .post<ApiResponse<FieldSurveyUploadResultDto>>(`${BASE}/${surveyId}/photos`, formData, {
        headers: { 'Content-Type': 'multipart/form-data' },
      })
      .then((r) => r.data);
  },
};
