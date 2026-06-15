import { apiClient } from '@/shared/api/apiClient';

export type ConsentPurpose = 'essential' | 'analytics' | 'marketing' | 'terms' | 'kvkk';

export interface ConsentDto {
  id: string;
  userId: string | null;
  purpose: ConsentPurpose;
  version: string;
  capturedAtUtc: string;
  withdrawnAtUtc: string | null;
}

export interface CaptureConsentInput {
  purpose: ConsentPurpose;
  version: string;
  given: boolean;
  fingerprint?: string | null;
}

export const consentApi = {
  capture: async (input: CaptureConsentInput): Promise<ConsentDto> => {
    const { data } = await apiClient.post<ConsentDto>('/consents', input);
    return data;
  },
  listMine: async (): Promise<ConsentDto[]> => {
    const { data } = await apiClient.get<ConsentDto[]>('/consents/me');
    return data;
  },
  withdraw: async (consentId: string): Promise<ConsentDto> => {
    const { data } = await apiClient.post<ConsentDto>(`/consents/${consentId}/withdraw`);
    return data;
  },
};
