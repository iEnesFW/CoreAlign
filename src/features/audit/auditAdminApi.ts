import { apiClient } from '@/shared/api/apiClient';
import type { ApiResponse } from '@/shared/types/api';
import type { EntityAuditLogDto } from './auditApi';

export type AuditLogExportFormat = 'Csv' | 'Json' | 'Excel';
export type AuditExportFrequency = 'Daily' | 'Weekly' | 'Monthly';

export interface AuditLogSearchFilter {
  fromUtc?: string;
  toUtc?: string;
  entityType?: string;
  action?: string;
  userId?: string;
  entityId?: string;
  page?: number;
  pageSize?: number;
}

export interface AuditLogPagedResult {
  items: EntityAuditLogDto[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface ScheduledAuditExportConfig {
  enabled: boolean;
  frequency: AuditExportFrequency;
  format: AuditLogExportFormat;
  recipients: string[];
  lookbackDays: number;
  entityTypes: string[] | null;
  lastRunAtUtc: string | null;
  lastRunStatus: string | null;
  lastRunError: string | null;
}

export interface UpsertScheduledAuditExportBody {
  enabled: boolean;
  frequency: AuditExportFrequency;
  format: AuditLogExportFormat;
  lookbackDays: number;
  recipients: string[];
  entityTypes: string[] | null;
}

export const auditAdminApi = {
  search: (filter: AuditLogSearchFilter) =>
    apiClient
      .get<ApiResponse<AuditLogPagedResult>>('/audit/search', { params: filter })
      .then((r) => r.data),

  exportFile: (
    format: AuditLogExportFormat,
    filter: Omit<AuditLogSearchFilter, 'page' | 'pageSize'>,
  ) =>
    apiClient
      .get<Blob>('/audit/export', {
        params: { ...filter, format },
        responseType: 'blob',
      })
      .then((r) => ({ blob: r.data, fileName: extractFileName(r.headers['content-disposition']) })),

  getSchedule: () =>
    apiClient
      .get<ApiResponse<ScheduledAuditExportConfig | null>>('/audit/schedule')
      .then((r) => r.data),

  upsertSchedule: (body: UpsertScheduledAuditExportBody) =>
    apiClient
      .put<ApiResponse<ScheduledAuditExportConfig>>('/audit/schedule', body)
      .then((r) => r.data),
};

const extractFileName = (header: string | undefined): string => {
  if (!header) return 'audit-log';
  const match = /filename\*?=(?:UTF-8'')?"?([^";]+)"?/i.exec(header);
  return match?.[1] ? decodeURIComponent(match[1]) : 'audit-log';
};
