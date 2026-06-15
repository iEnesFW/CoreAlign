import { apiClient } from '@/shared/api/apiClient';
import type { PortalUser } from './authStore';

export interface LoginResponse {
  accessToken: string;
  expiresAt: string;
  user: PortalUser;
}

export const login = async (email: string, password: string): Promise<LoginResponse> => {
  const { data } = await apiClient.post<LoginResponse>('/auth/login', { email, password });
  return data;
};
