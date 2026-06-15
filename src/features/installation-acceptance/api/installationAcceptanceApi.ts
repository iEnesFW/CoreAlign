import { apiClient } from '@/shared/api/apiClient';
import { cachedGet, invalidateHttpCache } from '@/shared/http/httpCache';
import type { ApiResponse } from '@/shared/types/api';
import type {
  AcceptInstallationInput,
  AcceptanceFullDetails,
  AddPunchListItemInput,
  CaptureSignatureInput,
  InstallationAcceptance,
  InstallationAcceptanceStatus,
  PunchListItem,
  PunchListItemStatus,
  RejectInstallationInput,
  ResolvePunchListItemInput,
  StartAcceptanceInput,
  UpdateChecklistItemInput,
  UploadPhotoInput,
} from '../model/installationAcceptance.types';

const BASE = '/installation-acceptances';
const INVALIDATION = [/\/installation-acceptances/i] as const;

export const installationAcceptanceApi = {
  listByWorkOrder: (workOrderId: string) =>
    cachedGet<ApiResponse<InstallationAcceptance[]>>(apiClient, BASE, {
      params: { workOrderId },
    }),

  listForInspector: (inspectorUserId: string, status?: InstallationAcceptanceStatus) =>
    cachedGet<ApiResponse<InstallationAcceptance[]>>(
      apiClient,
      `${BASE}/inspector/${inspectorUserId}`,
      { params: status ? { status } : undefined },
    ),

  getById: (id: string) =>
    cachedGet<ApiResponse<AcceptanceFullDetails>>(apiClient, `${BASE}/${id}`),

  listPunchByStatus: (status: PunchListItemStatus) =>
    cachedGet<ApiResponse<PunchListItem[]>>(apiClient, `${BASE}/punch-list`, {
      params: { status },
    }),

  start: (input: StartAcceptanceInput) =>
    apiClient.post<ApiResponse<InstallationAcceptance>>(`${BASE}/start`, input).then((r) => {
      invalidateHttpCache(INVALIDATION);
      return r.data;
    }),

  updateChecklist: (input: UpdateChecklistItemInput) =>
    apiClient
      .patch<ApiResponse<InstallationAcceptance>>(`${BASE}/${input.acceptanceId}/checklist`, input)
      .then((r) => {
        invalidateHttpCache(INVALIDATION);
        return r.data;
      }),

  addPhoto: (input: UploadPhotoInput) =>
    apiClient
      .post<ApiResponse<InstallationAcceptance>>(`${BASE}/${input.acceptanceId}/photos`, input)
      .then((r) => {
        invalidateHttpCache(INVALIDATION);
        return r.data;
      }),

  captureSignature: (input: CaptureSignatureInput) =>
    apiClient
      .post<ApiResponse<InstallationAcceptance>>(`${BASE}/${input.acceptanceId}/signature`, input)
      .then((r) => {
        invalidateHttpCache(INVALIDATION);
        return r.data;
      }),

  accept: (input: AcceptInstallationInput) =>
    apiClient
      .post<ApiResponse<InstallationAcceptance>>(`${BASE}/${input.acceptanceId}/accept`, input)
      .then((r) => {
        invalidateHttpCache(INVALIDATION);
        return r.data;
      }),

  reject: (input: RejectInstallationInput) =>
    apiClient
      .post<ApiResponse<InstallationAcceptance>>(`${BASE}/${input.acceptanceId}/reject`, input)
      .then((r) => {
        invalidateHttpCache(INVALIDATION);
        return r.data;
      }),

  addPunchListItem: (input: AddPunchListItemInput) =>
    apiClient
      .post<ApiResponse<PunchListItem>>(`${BASE}/${input.acceptanceId}/punch-list`, input)
      .then((r) => {
        invalidateHttpCache(INVALIDATION);
        return r.data;
      }),

  resolvePunchListItem: (input: ResolvePunchListItemInput) =>
    apiClient
      .post<ApiResponse<PunchListItem>>(`${BASE}/punch-list/${input.punchItemId}/resolve`, input)
      .then((r) => {
        invalidateHttpCache(INVALIDATION);
        return r.data;
      }),
};
