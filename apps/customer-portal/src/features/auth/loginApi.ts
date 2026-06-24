import { apiClient } from '@/shared/api/apiClient';
import type { PortalUser } from './authStore';

export interface LoginResponse {
  accessToken: string;
  expiresAt: string;
  user: PortalUser | null;
  requiresTwoFactor?: boolean;
  twoFactorChallengeToken?: string;
}

export const login = async (email: string, password: string): Promise<LoginResponse> => {
  const { data } = await apiClient.post<LoginResponse>('/auth/login', { email, password });
  return data;
};

export const completeTwoFactorChallenge = async (
  challengeToken: string,
  credential: { code?: string; backupCode?: string },
): Promise<LoginResponse> => {
  const { data } = await apiClient.post<LoginResponse>('/auth/2fa/challenge', {
    challengeToken,
    code: credential.code,
    backupCode: credential.backupCode,
  });
  return data;
};
