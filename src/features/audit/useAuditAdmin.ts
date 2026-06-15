import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  auditAdminApi,
  type AuditLogExportFormat,
  type AuditLogSearchFilter,
  type UpsertScheduledAuditExportBody,
} from './auditAdminApi';

const STALE_TIME_15S = 15 * 1000;
const STALE_TIME_5MIN = 5 * 60 * 1000;

export const auditAdminKeys = {
  all: ['audit', 'admin'] as const,
  search: (filter: AuditLogSearchFilter) => [...auditAdminKeys.all, 'search', filter] as const,
  schedule: () => [...auditAdminKeys.all, 'schedule'] as const,
};

export const useAuditSearchQuery = (filter: AuditLogSearchFilter, enabled = true) =>
  useQuery({
    queryKey: auditAdminKeys.search(filter),
    queryFn: () => auditAdminApi.search(filter),
    enabled,
    staleTime: STALE_TIME_15S,
  });

export const useAuditScheduleQuery = () =>
  useQuery({
    queryKey: auditAdminKeys.schedule(),
    queryFn: () => auditAdminApi.getSchedule(),
    staleTime: STALE_TIME_5MIN,
  });

export const useUpsertAuditSchedule = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: UpsertScheduledAuditExportBody) => auditAdminApi.upsertSchedule(body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: auditAdminKeys.schedule() });
    },
  });
};

export const useDownloadAuditExport = () =>
  useMutation({
    mutationFn: async ({
      format,
      filter,
    }: {
      format: AuditLogExportFormat;
      filter: Omit<AuditLogSearchFilter, 'page' | 'pageSize'>;
    }) => {
      const { blob, fileName } = await auditAdminApi.exportFile(format, filter);
      triggerDownload(blob, fileName);
      return { fileName };
    },
  });

const triggerDownload = (blob: Blob, fileName: string) => {
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = fileName;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  setTimeout(() => URL.revokeObjectURL(url), 0);
};
