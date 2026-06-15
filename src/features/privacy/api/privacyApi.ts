import { apiClient } from '@/shared/api/apiClient';
import type { ApiResponse } from '@/shared/types/api';
import type {
  PersonalDataExportDto,
  ErasureResultDto,
  DataSubjectRequestDto,
  DataSubjectRequestStatus,
  PagedRequestList,
  ProcessDataSubjectRequestBody,
  RetentionPolicyDto,
  SubmitDataSubjectRequestBody,
  UpsertRetentionPolicyBody,
} from '../model/privacy.types';

const BASE = '/privacy';
const REQUESTS_BASE = '/privacy/requests';
const ADMIN_REQUESTS_BASE = '/admin/privacy/requests';
const ADMIN_POLICIES_BASE = '/admin/privacy/retention-policies';

export const privacyApi = {
  exportMyData: () =>
    apiClient.get<ApiResponse<PersonalDataExportDto>>(`${BASE}/me/export`).then((r) => r.data),

  eraseMyAccount: (confirmationUsername: string) =>
    apiClient
      .post<ApiResponse<ErasureResultDto>>(`${BASE}/me/erase`, { confirmationUsername })
      .then((r) => r.data),

  submitRequest: (body: SubmitDataSubjectRequestBody) =>
    apiClient.post<ApiResponse<DataSubjectRequestDto>>(REQUESTS_BASE, body).then((r) => r.data),

  getRequest: (id: string) =>
    apiClient.get<ApiResponse<DataSubjectRequestDto>>(`${REQUESTS_BASE}/${id}`).then((r) => r.data),

  listAdminRequests: (status?: DataSubjectRequestStatus, page = 1, pageSize = 25) =>
    apiClient
      .get<ApiResponse<PagedRequestList>>(ADMIN_REQUESTS_BASE, {
        params: { status, page, pageSize },
      })
      .then((r) => r.data),

  processRequest: (id: string, body: ProcessDataSubjectRequestBody) =>
    apiClient
      .post<ApiResponse<DataSubjectRequestDto>>(`${ADMIN_REQUESTS_BASE}/${id}/process`, body)
      .then((r) => r.data),

  listRetentionPolicies: () =>
    apiClient.get<ApiResponse<RetentionPolicyDto[]>>(ADMIN_POLICIES_BASE).then((r) => r.data),

  createRetentionPolicy: (body: UpsertRetentionPolicyBody) =>
    apiClient.post<ApiResponse<RetentionPolicyDto>>(ADMIN_POLICIES_BASE, body).then((r) => r.data),

  updateRetentionPolicy: (id: string, body: UpsertRetentionPolicyBody) =>
    apiClient
      .put<ApiResponse<RetentionPolicyDto>>(`${ADMIN_POLICIES_BASE}/${id}`, body)
      .then((r) => r.data),
};
