import { apiClient } from '@/shared/api/apiClient';
import type {
  ApiResponse,
  AuthResponse,
  ChangePasswordRequest,
  ForgotPasswordRequest,
  LoginHistoryEntry,
  LoginRequest,
  RegisterRequest,
  ResetPasswordRequest,
  SessionInfo,
  UpdateProfileRequest,
  VerifyEmailRequest,
} from '../model/auth.types';

const AUTH_BASE = '/auth';

export const authApi = {
  login: (data: LoginRequest) =>
    apiClient.post<ApiResponse<AuthResponse>>(`${AUTH_BASE}/login`, data).then((r) => r.data),

  register: (data: RegisterRequest) =>
    apiClient.post<ApiResponse<AuthResponse>>(`${AUTH_BASE}/register`, data).then((r) => r.data),

  refreshToken: () =>
    apiClient.post<ApiResponse<AuthResponse>>(`${AUTH_BASE}/refresh-token`).then((r) => r.data),

  forgotPassword: (data: ForgotPasswordRequest) =>
    apiClient.post<ApiResponse<boolean>>(`${AUTH_BASE}/forgot-password`, data).then((r) => r.data),

  resetPassword: (data: ResetPasswordRequest) =>
    apiClient.post<ApiResponse<boolean>>(`${AUTH_BASE}/reset-password`, data).then((r) => r.data),

  verifyEmail: (data: VerifyEmailRequest) =>
    apiClient.post<ApiResponse<boolean>>(`${AUTH_BASE}/verify-email`, data).then((r) => r.data),

  logout: () => apiClient.post<ApiResponse<boolean>>(`${AUTH_BASE}/logout`).then((r) => r.data),

  getCurrentUser: () =>
    apiClient.get<ApiResponse<AuthResponse['user']>>(`${AUTH_BASE}/me`).then((r) => r.data),

  changePassword: (data: ChangePasswordRequest) =>
    apiClient.post<ApiResponse<boolean>>(`${AUTH_BASE}/change-password`, data).then((r) => r.data),

  updateProfile: (data: UpdateProfileRequest) =>
    apiClient.put<ApiResponse<AuthResponse>>(`${AUTH_BASE}/profile`, data).then((r) => r.data),

  getSessions: () =>
    apiClient.get<ApiResponse<SessionInfo[]>>(`${AUTH_BASE}/sessions`).then((r) => r.data),

  revokeSession: (sessionId: string) =>
    apiClient
      .delete<ApiResponse<boolean>>(`${AUTH_BASE}/sessions/${sessionId}`)
      .then((r) => r.data),

  revokeAllSessions: () =>
    apiClient.delete<ApiResponse<boolean>>(`${AUTH_BASE}/sessions`).then((r) => r.data),

  getLoginHistory: (count = 20) =>
    apiClient
      .get<ApiResponse<LoginHistoryEntry[]>>(`${AUTH_BASE}/login-history`, { params: { count } })
      .then((r) => r.data),
};
