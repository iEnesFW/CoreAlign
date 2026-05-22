import { apiClient } from '@/shared/api/apiClient';
import { parseApiResponse } from '@/shared/api/parseApiResponse';
import {
  authResponseSchema,
  booleanResultSchema,
  loginHistoryListSchema,
  sessionInfoListSchema,
  userProfileSchema,
} from '../model/authResponseSchemas';
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

const validateAuth = (body: unknown, endpoint: string): ApiResponse<AuthResponse> =>
  parseApiResponse(body, authResponseSchema, endpoint);

const validateBool = (body: unknown, endpoint: string): ApiResponse<boolean> =>
  parseApiResponse(body, booleanResultSchema, endpoint);

export const authApi = {
  login: (data: LoginRequest) =>
    apiClient
      .post<ApiResponse<AuthResponse>>(`${AUTH_BASE}/login`, data)
      .then((r) => validateAuth(r.data, 'auth.login')),

  register: (data: RegisterRequest) =>
    apiClient
      .post<ApiResponse<AuthResponse>>(`${AUTH_BASE}/register`, data)
      .then((r) => validateAuth(r.data, 'auth.register')),

  refreshToken: () =>
    apiClient
      .post<ApiResponse<AuthResponse>>(`${AUTH_BASE}/refresh-token`)
      .then((r) => validateAuth(r.data, 'auth.refreshToken')),

  forgotPassword: (data: ForgotPasswordRequest) =>
    apiClient
      .post<ApiResponse<boolean>>(`${AUTH_BASE}/forgot-password`, data)
      .then((r) => validateBool(r.data, 'auth.forgotPassword')),

  resetPassword: (data: ResetPasswordRequest) =>
    apiClient
      .post<ApiResponse<boolean>>(`${AUTH_BASE}/reset-password`, data)
      .then((r) => validateBool(r.data, 'auth.resetPassword')),

  verifyEmail: (data: VerifyEmailRequest) =>
    apiClient
      .post<ApiResponse<boolean>>(`${AUTH_BASE}/verify-email`, data)
      .then((r) => validateBool(r.data, 'auth.verifyEmail')),

  logout: () =>
    apiClient
      .post<ApiResponse<boolean>>(`${AUTH_BASE}/logout`)
      .then((r) => validateBool(r.data, 'auth.logout')),

  getCurrentUser: () =>
    apiClient
      .get<ApiResponse<AuthResponse['user']>>(`${AUTH_BASE}/me`)
      .then((r) => parseApiResponse(r.data, userProfileSchema, 'auth.me')),

  changePassword: (data: ChangePasswordRequest) =>
    apiClient
      .post<ApiResponse<boolean>>(`${AUTH_BASE}/change-password`, data)
      .then((r) => validateBool(r.data, 'auth.changePassword')),

  updateProfile: (data: UpdateProfileRequest) =>
    apiClient
      .put<ApiResponse<AuthResponse>>(`${AUTH_BASE}/profile`, data)
      .then((r) => validateAuth(r.data, 'auth.updateProfile')),

  getSessions: () =>
    apiClient
      .get<ApiResponse<SessionInfo[]>>(`${AUTH_BASE}/sessions`)
      .then((r) => parseApiResponse(r.data, sessionInfoListSchema, 'auth.getSessions')),

  revokeSession: (sessionId: string) =>
    apiClient
      .delete<ApiResponse<boolean>>(`${AUTH_BASE}/sessions/${sessionId}`)
      .then((r) => validateBool(r.data, 'auth.revokeSession')),

  revokeAllSessions: () =>
    apiClient
      .delete<ApiResponse<boolean>>(`${AUTH_BASE}/sessions`)
      .then((r) => validateBool(r.data, 'auth.revokeAllSessions')),

  getLoginHistory: (count = 20) =>
    apiClient
      .get<ApiResponse<LoginHistoryEntry[]>>(`${AUTH_BASE}/login-history`, { params: { count } })
      .then((r) => parseApiResponse(r.data, loginHistoryListSchema, 'auth.getLoginHistory')),
};
