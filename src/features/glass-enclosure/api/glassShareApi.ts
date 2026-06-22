import axios from 'axios';
import type { ApiResponse } from '@/shared/types/api';

export interface ShareViewerProjectDto {
  projectId: string;
  code: string;
  projectName: string;
  customerName: string | null;
  status: string;
  currency: string;
  grandTotal: number;
  version: number;
  sceneJson: string;
  validUntilUtc: string;
  alreadyDecided: boolean;
}

export interface ShareViewerActionResultDto {
  accepted: boolean;
  rejected: boolean;
  decidedAtUtc: string;
}

export interface ShareDecisionInput {
  accept: boolean;
  reason: string | null;
  signatureDataUrl: string | null;
}

const publicClient = axios.create({ baseURL: '/api/v1', withCredentials: false });

export const glassShareApi = {
  getSharedProject: (token: string): Promise<ShareViewerProjectDto | null> =>
    publicClient
      .get<ApiResponse<ShareViewerProjectDto>>(`/share/glass/${token}`)
      .then((r) => r.data.data),
  submitDecision: (
    token: string,
    input: ShareDecisionInput,
  ): Promise<ShareViewerActionResultDto | null> =>
    publicClient
      .post<ApiResponse<ShareViewerActionResultDto>>(`/share/glass/${token}/action`, input)
      .then((r) => r.data.data),
};
