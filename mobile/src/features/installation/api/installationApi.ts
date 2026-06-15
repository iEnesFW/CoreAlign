import { apiClient } from '@/api/apiClient';

export type InstallationStatus =
  | 'Pending'
  | 'InProgress'
  | 'AwaitingCustomer'
  | 'Accepted'
  | 'Rejected';

export type ChecklistCategoryCode = 'Glass' | 'Frame' | 'Hardware' | 'Sealing' | 'Cleanup';

export type ChecklistItemStatus = 'NotChecked' | 'Pass' | 'Fail' | 'NotApplicable';

export interface InstallationListItem {
  id: string;
  projectId: string;
  projectCode: string;
  customerName: string;
  siteAddress: string;
  scheduledAt: string;
  status: InstallationStatus;
  totalGlassCount: number;
  checklistCompletion: number;
}

export interface InstallationChecklistItem {
  id: string;
  code: string;
  label: string;
  status: ChecklistItemStatus;
  notes: string | null;
  requiresPhoto: boolean;
}

export interface InstallationChecklistCategory {
  code: ChecklistCategoryCode;
  label: string;
  items: InstallationChecklistItem[];
}

export interface InstallationPhoto {
  id: string;
  url: string;
  capturedAt: string;
  caption: string | null;
  checklistItemId: string | null;
}

export interface InstallationSignature {
  id: string;
  signerName: string;
  signerRole: string | null;
  capturedAt: string;
  imageUrl: string;
}

export interface PunchListItem {
  id: string;
  description: string;
  severity: 'Minor' | 'Major' | 'Critical';
  resolved: boolean;
  createdAt: string;
}

export interface InstallationDetail extends InstallationListItem {
  notes: string | null;
  technicianId: string;
  categories: InstallationChecklistCategory[];
  photos: InstallationPhoto[];
  signature: InstallationSignature | null;
  punchList: PunchListItem[];
}

export interface StartInstallationResponse {
  installationId: string;
  startedAt: string;
}

export interface UpdateChecklistItemRequest {
  itemId: string;
  status: ChecklistItemStatus;
  notes?: string | null;
}

export interface SubmitSignatureRequest {
  signerName: string;
  signerRole?: string | null;
  signatureBase64: string;
}

export interface AcceptInstallationRequest {
  signerName: string;
  signatureBase64: string;
  notes?: string | null;
  photoIds: string[];
  idempotencyKey: string;
}

export interface RejectInstallationRequest {
  reason: string;
  punchList: Omit<PunchListItem, 'id' | 'resolved' | 'createdAt'>[];
  idempotencyKey: string;
}

export interface CreatePunchItemRequest {
  description: string;
  severity: PunchListItem['severity'];
}

const buildIdempotencyHeader = (key: string): Record<string, string> => ({
  'Idempotency-Key': key,
});

export const installationApi = {
  async listPending(): Promise<InstallationListItem[]> {
    const { data } = await apiClient.get<InstallationListItem[]>('/api/v1/installations/pending');
    return data;
  },

  async getById(id: string): Promise<InstallationDetail> {
    const { data } = await apiClient.get<InstallationDetail>(`/api/v1/installations/${id}`);
    return data;
  },

  async start(id: string): Promise<StartInstallationResponse> {
    const { data } = await apiClient.post<StartInstallationResponse>(
      `/api/v1/installations/${id}/start`,
    );
    return data;
  },

  async updateChecklistItem(
    id: string,
    body: UpdateChecklistItemRequest,
  ): Promise<InstallationChecklistItem> {
    const { data } = await apiClient.patch<InstallationChecklistItem>(
      `/api/v1/installations/${id}/checklist`,
      body,
    );
    return data;
  },

  async uploadPhoto(id: string, form: FormData): Promise<InstallationPhoto> {
    const { data } = await apiClient.post<InstallationPhoto>(
      `/api/v1/installations/${id}/photos`,
      form,
      { headers: { 'Content-Type': 'multipart/form-data' } },
    );
    return data;
  },

  async deletePhoto(id: string, photoId: string): Promise<void> {
    await apiClient.delete(`/api/v1/installations/${id}/photos/${photoId}`);
  },

  async submitSignature(id: string, body: SubmitSignatureRequest): Promise<InstallationSignature> {
    const { data } = await apiClient.post<InstallationSignature>(
      `/api/v1/installations/${id}/signature`,
      body,
    );
    return data;
  },

  async accept(id: string, body: AcceptInstallationRequest): Promise<InstallationDetail> {
    const { data } = await apiClient.post<InstallationDetail>(
      `/api/v1/installations/${id}/accept`,
      body,
      { headers: buildIdempotencyHeader(body.idempotencyKey) },
    );
    return data;
  },

  async reject(id: string, body: RejectInstallationRequest): Promise<InstallationDetail> {
    const { data } = await apiClient.post<InstallationDetail>(
      `/api/v1/installations/${id}/reject`,
      body,
      { headers: buildIdempotencyHeader(body.idempotencyKey) },
    );
    return data;
  },

  async addPunchItem(id: string, body: CreatePunchItemRequest): Promise<PunchListItem> {
    const { data } = await apiClient.post<PunchListItem>(
      `/api/v1/installations/${id}/punch-list`,
      body,
    );
    return data;
  },

  async resolvePunchItem(id: string, punchItemId: string): Promise<PunchListItem> {
    const { data } = await apiClient.patch<PunchListItem>(
      `/api/v1/installations/${id}/punch-list/${punchItemId}/resolve`,
    );
    return data;
  },
};
