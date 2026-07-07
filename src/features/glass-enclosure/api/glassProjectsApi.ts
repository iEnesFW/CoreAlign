import { apiClient } from '@/shared/api/apiClient';
import { cachedGet, invalidateHttpCache } from '@/shared/http/httpCache';
import type { ApiResponse, PagedResult } from '@/shared/types/api';
import type { WorkOrderRevisionStatus } from '../model/workOrder.types';
import type {
  AddPanelInput,
  AddRunConnectionInput,
  AddRunInput,
  AssignProjectTeamInput,
  BulkRebalancePanelsInput,
  ConfigureEnclosureInput,
  CreateGlassProjectInput,
  GlassProjectDto,
  GlassProjectListItem,
  GlassProjectPanelDto,
  GlassProjectRunDto,
  GlassProjectValidationResultDto,
  ProjectsListParams,
  RunConnectionDto,
  SaveSceneInput,
  SetRunPanelsInput,
  SceneLatestDto,
  SceneVersionDto,
  TransitionProjectStatusInput,
  UpdateGlassProjectHeaderInput,
  UpdatePanelInput,
  UpdateRunConnectionInput,
  UpdateRunInput,
} from '../model/project.types';
import type {
  AddManualBomLineInput,
  BOMSummaryDto,
  CuttingReportDto,
  Glass2DNestingReportDto,
  Optimize2DNestingInput,
  PushBomLinePriceResultDto,
  TechnicalSummaryDto,
} from '../model/engineering.types';

const BASE = '/glass-enclosure/projects';
const INVALIDATION = [/\/glass-enclosure\/projects/i] as const;

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

export const glassProjectsApi = {
  list: (params: ProjectsListParams) =>
    cachedGet<ApiResponse<PagedResult<GlassProjectListItem>>>(apiClient, BASE, { params }),

  getById: (id: string) => cachedGet<ApiResponse<GlassProjectDto>>(apiClient, `${BASE}/${id}`),

  create: (input: CreateGlassProjectInput) => post<GlassProjectDto>('', input),

  updateHeader: (id: string, input: UpdateGlassProjectHeaderInput) =>
    put<GlassProjectDto>(`/${id}/header`, input),

  assignTeam: (id: string, input: AssignProjectTeamInput) =>
    put<GlassProjectDto>(`/${id}/team`, input),

  configureEnclosure: (id: string, input: ConfigureEnclosureInput) =>
    put<GlassProjectDto>(`/${id}/enclosure`, input),

  transitionStatus: (id: string, input: TransitionProjectStatusInput) =>
    post<GlassProjectDto>(`/${id}/status`, input),

  remove: (id: string) => del(`/${id}`),

  addRun: (id: string, input: AddRunInput) => post<GlassProjectRunDto>(`/${id}/runs`, input),

  updateRun: (id: string, runId: string, input: UpdateRunInput) =>
    put<GlassProjectRunDto>(`/${id}/runs/${runId}`, input),

  removeRun: (id: string, runId: string) => del(`/${id}/runs/${runId}`),

  rebalancePanels: (id: string, runId: string, input: BulkRebalancePanelsInput) =>
    post<GlassProjectRunDto>(`/${id}/runs/${runId}/rebalance-panels`, input),
  setRunPanels: (id: string, runId: string, input: SetRunPanelsInput) =>
    post<GlassProjectRunDto>(`/${id}/runs/${runId}/set-panels`, input),

  addPanel: (id: string, runId: string, input: AddPanelInput) =>
    post<GlassProjectPanelDto>(`/${id}/runs/${runId}/panels`, input),

  updatePanel: (id: string, runId: string, panelId: string, input: UpdatePanelInput) =>
    put<GlassProjectPanelDto>(`/${id}/runs/${runId}/panels/${panelId}`, input),

  removePanel: (id: string, runId: string, panelId: string) =>
    del(`/${id}/runs/${runId}/panels/${panelId}`),

  addConnection: (id: string, input: AddRunConnectionInput) =>
    post<RunConnectionDto>(`/${id}/connections`, input),

  updateConnection: (id: string, connectionId: string, input: UpdateRunConnectionInput) =>
    put<RunConnectionDto>(`/${id}/connections/${connectionId}`, input),

  removeConnection: (id: string, connectionId: string) => del(`/${id}/connections/${connectionId}`),

  getSceneLatest: (id: string) =>
    apiClient
      .get<ApiResponse<SceneLatestDto | null>>(`${BASE}/${id}/scene/latest`)
      .then((r) => r.data),

  getSceneVersions: (id: string, limit = 50) =>
    cachedGet<ApiResponse<SceneVersionDto[]>>(apiClient, `${BASE}/${id}/scene/versions`, {
      params: { limit },
    }),

  getSceneByVersion: (id: string, version: number) =>
    apiClient
      .get<ApiResponse<SceneLatestDto | null>>(`${BASE}/${id}/scene/version/${version}`)
      .then((r) => r.data),

  saveScene: (id: string, input: SaveSceneInput) => post<SceneVersionDto>(`/${id}/scene`, input),

  validate: (id: string) => post<GlassProjectValidationResultDto>(`/${id}/validate`, {}),

  recomputeBOM: (id: string) => post<BOMSummaryDto>(`/${id}/bom/recompute`, {}),

  getBOM: (id: string) => cachedGet<ApiResponse<BOMSummaryDto>>(apiClient, `${BASE}/${id}/bom`),

  // Not HTTP-cached: a live cost preview must re-compose on every settled edit.
  getBomPreview: (id: string) =>
    apiClient.get<ApiResponse<BOMSummaryDto>>(`${BASE}/${id}/bom/preview`).then((r) => r.data.data),

  overrideBomLinePrice: (id: string, lineId: string, unitPriceOverride: number | null) =>
    put<BOMSummaryDto>(`/${id}/bom/lines/${lineId}/price-override`, { unitPriceOverride }),

  addManualBomLine: (id: string, input: AddManualBomLineInput) =>
    post<BOMSummaryDto>(`/${id}/bom/lines/manual`, input),

  deleteManualBomLine: (id: string, lineId: string) => del(`/${id}/bom/lines/${lineId}`),

  pushBomLinePriceToCatalog: (id: string, lineId: string) =>
    post<PushBomLinePriceResultDto>(`/${id}/bom/lines/${lineId}/push-price-to-catalog`, {}),

  generateCuttingPlan: (id: string) => post<CuttingReportDto>(`/${id}/cutting-plan/generate`, {}),

  optimize2DNesting: (id: string, input: Optimize2DNestingInput) =>
    post<Glass2DNestingReportDto>(`/${id}/optimize-2d-nesting`, input),

  getCuttingReport: (id: string) =>
    apiClient
      .get<ApiResponse<CuttingReportDto | null>>(`${BASE}/${id}/cutting-plan`)
      .then((r) => r.data),

  getTechnicalSummary: (id: string) =>
    cachedGet<ApiResponse<TechnicalSummaryDto>>(apiClient, `${BASE}/${id}/technical-summary`),

  generateShareToken: (id: string, overrideTtlDays: number | null) =>
    post<ShareTokenInfoDto>(`/${id}/share-tokens`, { overrideTtlDays }),

  listShareTokens: (id: string) =>
    cachedGet<ApiResponse<ShareTokenInfoDto[]>>(apiClient, `${BASE}/${id}/share-tokens`),

  convertToOrder: (id: string) =>
    post<ConvertProjectToOrderResultDto>(`/${id}/convert-to-order`, {}),

  releaseToProduction: (id: string, body: ReleaseToProductionInput) =>
    post<GlassWorkOrderDto>(`/${id}/release-to-production`, body),

  listWorkOrders: (id: string) =>
    cachedGet<ApiResponse<GlassWorkOrderDto[]>>(apiClient, `${BASE}/${id}/work-orders`),

  updateWorkOrderStatus: (workOrderId: string, status: string) =>
    put<GlassWorkOrderDto>(`/work-orders/${workOrderId}/status`, { status }),

  recordWorkOrderDefect: (workOrderId: string, defectNotes: string) =>
    post<GlassWorkOrderDto>(`/work-orders/${workOrderId}/defect`, { defectNotes }),

  listNotificationHistory: (id: string) =>
    cachedGet<ApiResponse<NotificationLogDto[]>>(apiClient, `${BASE}/${id}/notifications`),
};

export interface NotificationLogDto {
  id: string;
  projectId: string;
  eventCode: string;
  channel: string;
  recipientKind: string;
  recipientAddress: string;
  status: string;
  providerMessageId: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
  deliveredAtUtc: string | null;
  readAtUtc: string | null;
  errorMessage: string | null;
  retryCount: number;
}

export interface ShareTokenInfoDto {
  id: string;
  token: string;
  publicUrl: string;
  sceneVersion: number;
  expiresAtUtc: string;
  viewCount: number;
  lastViewedAtUtc: string | null;
  acceptedAtUtc: string | null;
  rejectedAtUtc: string | null;
  rejectionReason: string | null;
}

export interface ConvertProjectToOrderResultDto {
  projectId: string;
  orderId: string;
  orderNumber: string;
  linkedAtUtc: string;
}

export interface ReleaseToProductionInput {
  requestedStartDateUtc: string | null;
  assignedTeamId: string | null;
}

export interface GlassWorkOrderDto {
  id: string;
  projectId: string;
  scheduledStartDate: string;
  scheduledEndDate: string;
  assignedTeamId: string | null;
  assignedInstallerUserId: string | null;
  workloadM2: number;
  status: string;
  recutCount: number;
  defectNotes: string | null;
  bomSnapshotJson: string | null;
  bomSnapshotTotal: number | null;
  revisionCount: number;
  hasOutstandingBlockingRevision: boolean;
  latestRevisionStatus: WorkOrderRevisionStatus | null;
  latestRevisionNumber: number | null;
  latestRevisionDeltaPercent: number | null;
}
