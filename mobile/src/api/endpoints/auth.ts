import { apiClient } from '@/api/apiClient';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface AuthSession {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
}

export interface CurrentUser {
  id: string;
  email: string;
  fullName: string;
  role: string;
  tenantId: string;
  locale: string;
  avatarUrl?: string | null;
}

export const authApi = {
  async login(body: LoginRequest): Promise<AuthSession> {
    const { data } = await apiClient.post<AuthSession>('/api/v1/auth/login', body);
    return data;
  },
  async refresh(refreshToken: string): Promise<AuthSession> {
    const { data } = await apiClient.post<AuthSession>('/api/v1/auth/refresh', { refreshToken });
    return data;
  },
  async logout(): Promise<void> {
    await apiClient.post('/api/v1/auth/logout');
  },
  async me(): Promise<CurrentUser> {
    const { data } = await apiClient.get<CurrentUser>('/api/v1/auth/me');
    return data;
  },
};
