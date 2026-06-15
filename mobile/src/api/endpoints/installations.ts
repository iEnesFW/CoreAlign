import { apiClient } from '@/api/apiClient';

export interface InstallationListItem {
  id: string;
  projectId: string;
  customerName: string;
  siteAddress: string;
  scheduledAt: string;
  status: 'Pending' | 'InProgress' | 'AwaitingCustomer' | 'Accepted' | 'Rejected';
  totalGlassCount: number;
}

export interface InstallationDetail extends InstallationListItem {
  notes: string | null;
  technicianId: string;
  photos: InstallationPhoto[];
  signature: InstallationSignature | null;
}

export interface InstallationPhoto {
  id: string;
  url: string;
  capturedAt: string;
  caption: string | null;
}

export interface InstallationSignature {
  id: string;
  signerName: string;
  capturedAt: string;
  imageUrl: string;
}

export interface AcceptInstallationRequest {
  signerName: string;
  signatureBase64: string;
  notes?: string;
  photoIds: string[];
}

export const installationsApi = {
  async listPending(): Promise<InstallationListItem[]> {
    const { data } = await apiClient.get<InstallationListItem[]>('/api/v1/installations/pending');
    return data;
  },
  async getById(id: string): Promise<InstallationDetail> {
    const { data } = await apiClient.get<InstallationDetail>(`/api/v1/installations/${id}`);
    return data;
  },
  async accept(id: string, body: AcceptInstallationRequest): Promise<InstallationDetail> {
    const { data } = await apiClient.post<InstallationDetail>(
      `/api/v1/installations/${id}/accept`,
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
};
