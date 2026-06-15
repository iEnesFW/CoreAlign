import { apiClient } from '@/shared/api/apiClient';
import { safeRequest, type SafeResult } from '@/shared/lib/safeRequest';
import type {
  BIDataSource,
  BIExportFormat,
  BIQueryConfig,
  BIResult,
  DashboardWidget,
  DashboardWidgetUpsert,
  SavedReport,
  SavedReportUpsert,
} from '../model/bi.types';

const DASHBOARD_BASE = '/bi/dashboard';
const REPORTS_BASE = '/bi/reports';

const unwrap = <T>(response: { data: T }): T => response.data;

export const biApi = {
  getDashboard: (): Promise<SafeResult<DashboardWidget[]>> =>
    safeRequest(apiClient.get<DashboardWidget[]>(DASHBOARD_BASE).then(unwrap)),

  saveLayout: (widgets: DashboardWidgetUpsert[]): Promise<SafeResult<void>> =>
    safeRequest(apiClient.put<void>(DASHBOARD_BASE, widgets).then(unwrap)),

  addWidget: (widget: DashboardWidgetUpsert): Promise<SafeResult<DashboardWidget>> =>
    safeRequest(apiClient.post<DashboardWidget>(`${DASHBOARD_BASE}/widgets`, widget).then(unwrap)),

  removeWidget: (id: string): Promise<SafeResult<void>> =>
    safeRequest(apiClient.delete<void>(`${DASHBOARD_BASE}/widgets/${id}`).then(unwrap)),

  listReports: (): Promise<SafeResult<SavedReport[]>> =>
    safeRequest(apiClient.get<SavedReport[]>(REPORTS_BASE).then(unwrap)),

  createReport: (dto: SavedReportUpsert): Promise<SafeResult<SavedReport>> =>
    safeRequest(apiClient.post<SavedReport>(REPORTS_BASE, dto).then(unwrap)),

  updateReport: (id: string, dto: SavedReportUpsert): Promise<SafeResult<SavedReport>> =>
    safeRequest(apiClient.put<SavedReport>(`${REPORTS_BASE}/${id}`, dto).then(unwrap)),

  deleteReport: (id: string): Promise<SafeResult<void>> =>
    safeRequest(apiClient.delete<void>(`${REPORTS_BASE}/${id}`).then(unwrap)),

  runReport: (id: string): Promise<SafeResult<BIResult>> =>
    safeRequest(apiClient.post<BIResult>(`${REPORTS_BASE}/${id}/run`).then(unwrap)),

  exportReport: (id: string, format: BIExportFormat): Promise<SafeResult<Blob>> =>
    safeRequest(
      apiClient
        .post<Blob>(`${REPORTS_BASE}/${id}/export`, null, {
          params: { format },
          responseType: 'blob',
        })
        .then(unwrap),
    ),

  executeAdHoc: (dataSource: BIDataSource, config: BIQueryConfig): Promise<SafeResult<BIResult>> =>
    safeRequest(
      apiClient
        .post<BIResult>(`${REPORTS_BASE}/execute`, config, { params: { dataSource } })
        .then(unwrap),
    ),
};
