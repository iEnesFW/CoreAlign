import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { biApi } from '../api/biApi';
import type { BIExportFormat, BIResult, SavedReport, SavedReportUpsert } from '../model/bi.types';

const REPORTS_KEY = ['bi', 'reports'] as const;

export const useReportsQuery = (enabled = true) =>
  useQuery({
    queryKey: REPORTS_KEY,
    queryFn: async () => {
      const [data, error] = await biApi.listReports();
      if (error) {
        throw error;
      }
      return (data ?? []) as SavedReport[];
    },
    enabled,
    staleTime: 60 * 1000,
  });

export const useCreateReport = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (dto: SavedReportUpsert) => {
      const [data, error] = await biApi.createReport(dto);
      if (error) {
        throw error;
      }
      return data as SavedReport;
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: REPORTS_KEY }),
  });
};

export const useUpdateReport = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, dto }: { id: string; dto: SavedReportUpsert }) => {
      const [data, error] = await biApi.updateReport(id, dto);
      if (error) {
        throw error;
      }
      return data as SavedReport;
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: REPORTS_KEY }),
  });
};

export const useDeleteReport = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      const [, error] = await biApi.deleteReport(id);
      if (error) {
        throw error;
      }
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: REPORTS_KEY }),
  });
};

export const useRunReport = () =>
  useMutation({
    mutationFn: async (id: string) => {
      const [data, error] = await biApi.runReport(id);
      if (error) {
        throw error;
      }
      return data as BIResult;
    },
  });

export const useExportReport = () =>
  useMutation({
    mutationFn: async ({ id, format }: { id: string; format: BIExportFormat }) => {
      const [blob, error] = await biApi.exportReport(id, format);
      if (error) {
        throw error;
      }
      return blob as Blob;
    },
  });
