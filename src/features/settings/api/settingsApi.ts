import { apiClient } from '@/shared/api/apiClient';
import type { ApiResponse } from '@/shared/types/api';
import type {
  CompanyProfile,
  ConfigureDocumentSequenceRequest,
  CreateEmailTemplateRequest,
  DocumentSequenceConfig,
  EmailTemplate,
  SettingUpsertItem,
  TenantSetting,
  UpdateCompanyProfileRequest,
  UpdateEmailTemplateRequest,
} from '../model/settings.types';

const BASE = '/settings';

export const settingsApi = {
  getCompany: () =>
    apiClient.get<ApiResponse<CompanyProfile>>(`${BASE}/company`).then((r) => r.data),

  updateCompany: (request: UpdateCompanyProfileRequest) =>
    apiClient.put<ApiResponse<CompanyProfile>>(`${BASE}/company`, request).then((r) => r.data),

  getParameters: (category?: string) =>
    apiClient
      .get<ApiResponse<TenantSetting[]>>(`${BASE}/parameters`, { params: { category } })
      .then((r) => r.data),

  upsertParameters: (items: SettingUpsertItem[]) =>
    apiClient
      .put<ApiResponse<TenantSetting[]>>(`${BASE}/parameters`, { items })
      .then((r) => r.data),

  deleteParameter: (category: string, key: string) =>
    apiClient
      .delete<ApiResponse<boolean>>(`${BASE}/parameters/${category}/${key}`)
      .then((r) => r.data),

  getEmailTemplates: () =>
    apiClient.get<ApiResponse<EmailTemplate[]>>(`${BASE}/email-templates`).then((r) => r.data),

  getEmailTemplate: (id: string) =>
    apiClient.get<ApiResponse<EmailTemplate>>(`${BASE}/email-templates/${id}`).then((r) => r.data),

  createEmailTemplate: (request: CreateEmailTemplateRequest) =>
    apiClient
      .post<ApiResponse<EmailTemplate>>(`${BASE}/email-templates`, request)
      .then((r) => r.data),

  updateEmailTemplate: (request: UpdateEmailTemplateRequest) =>
    apiClient
      .put<ApiResponse<EmailTemplate>>(`${BASE}/email-templates/${request.id}`, request)
      .then((r) => r.data),

  deleteEmailTemplate: (id: string) =>
    apiClient.delete<ApiResponse<boolean>>(`${BASE}/email-templates/${id}`).then((r) => r.data),

  getDocumentSequences: () =>
    apiClient
      .get<ApiResponse<DocumentSequenceConfig[]>>(`${BASE}/document-sequences`)
      .then((r) => r.data),

  configureDocumentSequence: (request: ConfigureDocumentSequenceRequest) =>
    apiClient
      .post<ApiResponse<DocumentSequenceConfig>>(`${BASE}/document-sequences`, request)
      .then((r) => r.data),
};
